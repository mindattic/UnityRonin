using UnityEngine;
using UnityRonin.Core.Combat;
using UnityRonin.Core.Movement;
using UnityRonin.Runtime.Camera;
using UnityRonin.Runtime.Combat;
using UnityRonin.Runtime.Input;
using UnityRonin.Runtime.Player;

namespace UnityRonin.Runtime.Game
{
    /// <summary>
    /// The single state switch of the game: Explore (joystick drives free third-person movement) vs
    /// Duel (movement is automatic, the joystick is released, and swipes attack/parry a telegraphed
    /// enemy). Picks the nearest in-range enemy, hands off camera framing and input ownership, and
    /// returns to Explore when the duel ends.
    /// </summary>
    public class RoninGameController : MonoBehaviour
    {
        [SerializeField] private SamuraiLocomotion _locomotion;
        [SerializeField] private SamuraiWeapon _weapon;
        [SerializeField] private SmartFollowCamera _camera;
        [SerializeField] private VirtualJoystick _joystick;
        [SerializeField] private DuelPresenter _duelPresenter;
        [SerializeField] private GameObject _joystickZone;
        [SerializeField] private GameObject _swipeZone;
        [SerializeField] private EnemyEngageZone[] _enemies;
        [SerializeField] private uint _duelSeed = 1;

        private EnemyEngageZone _activeEnemy;

        private void Awake()
        {
            // Mobile battery/thermal hygiene, and it matters functionally too: left uncapped (as happens
            // in a -nographics batch run with nothing to render), the frame rate goes high enough that
            // CharacterController.Move's per-frame displacement drops below its own collision-resolution
            // precision while touching the ground, and horizontal movement effectively stops. A real phone
            // never runs anywhere near this fast, so this only ever bit the headless autoplay harness —
            // but capping it is correct regardless of that.
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            if (_duelPresenter.IsActive)
            {
                _locomotion.StickInput = StickVector.Zero;
                return;
            }

            _locomotion.AcceptsStickInput = true;
            _locomotion.StickInput = _joystick.Current;

            var nearest = FindNearestEngaged();
            if (nearest != null)
                StartDuel(nearest);
        }

        private EnemyEngageZone FindNearestEngaged()
        {
            // This controller lives on its own logic-only GameObject, not on the player, so distance
            // must be measured from the Samurai's transform, never this component's own transform.
            Vector3 playerPosition = _locomotion.transform.position;

            EnemyEngageZone best = null;
            float bestDistance = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                var state = enemy.UpdateDistance(playerPosition);
                float distance = Vector3.Distance(playerPosition, enemy.transform.position);
                if (state == EngageState.Engaged && distance < bestDistance)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void StartDuel(EnemyEngageZone enemy)
        {
            _activeEnemy = enemy;
            _joystick.ForceRelease();
            _locomotion.AcceptsStickInput = false;
            _locomotion.AutoMoveTarget = null;
            _camera.SetDuelFocus(enemy.transform);
            _joystickZone.SetActive(false);
            _swipeZone.SetActive(true);

            _duelPresenter.DuelEnded += OnDuelEnded;
            _duelPresenter.BeatResolved += OnBeatResolved;
            _duelPresenter.Begin(_duelSeed++);
        }

        /// <summary>Just the two riposte outcomes that are about the enemy's own reaction, not the player's
        /// (see <see cref="EnemyCombatFeedback"/>) — everything else about scoring stays Core's business.</summary>
        private void OnBeatResolved(DuelOutcome outcome)
        {
            if (_activeEnemy == null) return;
            var feedback = _activeEnemy.GetComponent<EnemyCombatFeedback>();
            if (feedback == null) return;

            if (outcome == DuelOutcome.PlayerRiposteRetreated) feedback.HopBack(_locomotion.transform.position);
            else if (outcome == DuelOutcome.PlayerRiposteCountered) feedback.FlashCounter();
        }

        private void OnDuelEnded(bool playerWon)
        {
            _duelPresenter.DuelEnded -= OnDuelEnded;
            _duelPresenter.BeatResolved -= OnBeatResolved;
            _camera.SetDuelFocus(null);
            _swipeZone.SetActive(false);
            _joystickZone.SetActive(true);

            if (_activeEnemy != null)
            {
                if (playerWon) _activeEnemy.gameObject.SetActive(false);
                else Retreat(_activeEnemy);
            }
            _activeEnemy = null;
        }

        /// <summary>
        /// Losing knocks the Samurai back out of the enemy's exit radius, so the engage gate actually
        /// releases to Exploring instead of re-triggering the same duel the very next frame.
        /// </summary>
        private void Retreat(EnemyEngageZone enemy)
        {
            Vector3 away = _locomotion.transform.position - enemy.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -enemy.transform.forward;

            Vector3 destination = enemy.transform.position + away.normalized * (enemy.ExitRadius + 1f);
            _locomotion.Teleport(destination);
        }
    }
}
