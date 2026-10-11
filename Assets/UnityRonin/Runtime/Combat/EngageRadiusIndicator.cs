using UnityEngine;

namespace UnityRonin.Runtime.Combat
{
    /// <summary>
    /// A transparent disc on the ground, centered on the player and sized to the enemies' engage-enter
    /// radius — a visible version of <see cref="EnemyEngageZone"/>'s own editor-only gizmo, shown only
    /// while an enemy is near (inside its exit radius) and no duel is already running, so stepping inside
    /// it is a visible warning that the duel is about to start.
    /// </summary>
    public class EngageRadiusIndicator : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private DuelPresenter _duelPresenter;
        [SerializeField] private EnemyEngageZone[] _enemies;

        private Renderer _renderer;
        private bool _sized;

        private void Awake() => _renderer = GetComponent<Renderer>();

        private void LateUpdate()
        {
            transform.position = new Vector3(_player.position.x, transform.position.y, _player.position.z);

            if (!_sized)
                TrySizeToEnterRadius();

            _renderer.enabled = !_duelPresenter.IsActive && IsAnyEnemyNear();
        }

        private void TrySizeToEnterRadius()
        {
            foreach (var enemy in _enemies)
            {
                if (enemy == null) continue;
                float diameter = enemy.EnterRadius * 2f;
                transform.localScale = new Vector3(diameter, transform.localScale.y, diameter);
                _sized = true;
                return;
            }
        }

        private bool IsAnyEnemyNear()
        {
            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(_player.position, enemy.transform.position) <= enemy.ExitRadius) return true;
            }
            return false;
        }
    }
}
