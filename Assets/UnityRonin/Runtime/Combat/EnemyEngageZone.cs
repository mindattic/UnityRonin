using UnityEngine;
using UnityRonin.Core.Movement;

namespace UnityRonin.Runtime.Combat
{
    /// <summary>Tracks how close the player is to this one enemy, through the Core hysteresis gate.</summary>
    public class EnemyEngageZone : MonoBehaviour
    {
        [SerializeField] private float _enterRadius = 4f;
        [SerializeField] private float _exitRadius = 6f;

        private EngageGate _gate;
        public EngageState State => _gate.State;
        public float ExitRadius => _exitRadius;

        private void Awake()
        {
            _gate = new EngageGate(_enterRadius, _exitRadius);
        }

        public EngageState UpdateDistance(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(playerPosition, transform.position);
            return _gate.Update(distance);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _enterRadius);
            Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _exitRadius);
        }
    }
}
