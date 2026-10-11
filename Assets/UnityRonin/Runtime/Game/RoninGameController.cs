using UnityEngine;
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
            EnemyEngageZone best = null;
            float bestDistance = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                var state = enemy.UpdateDistance(transform.position);
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
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
            _duelPresenter.Begin(_duelSeed++);
        }

        private void OnDuelEnded(bool playerWon)
        {
            _duelPresenter.DuelEnded -= OnDuelEnded;
            _camera.SetDuelFocus(null);
            _swipeZone.SetActive(false);
            _joystickZone.SetActive(true);
            if (_activeEnemy != null && playerWon) _activeEnemy.gameObject.SetActive(false);
            _activeEnemy = null;
        }
    }
}
