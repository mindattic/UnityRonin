using UnityEngine;

namespace UnityRonin.Runtime.Camera
{
    /// <summary>
    /// Third-person, slightly-above-and-behind follow camera. While exploring it simply trails the
    /// Samurai; while dueling it instead frames the midpoint between the Samurai and the enemy, so a
    /// one-thumb portrait layout never needs the player to aim the camera themselves.
    /// </summary>
    public class SmartFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private Vector3 _exploreOffset = new Vector3(0f, 2.3f, -3.1f);
        [SerializeField] private Vector3 _duelOffset = new Vector3(0f, 1.8f, -2.3f);
        [SerializeField] private float _positionSmoothTime = 0.18f;
        [SerializeField] private float _lookSmoothSpeed = 2.5f;

        private Transform _duelFocus;
        private Vector3 _velocity;

        public void SetPlayer(Transform player) => _player = player;

        /// <summary>Set a second combatant to frame alongside the player; null returns to free-follow.</summary>
        public void SetDuelFocus(Transform enemy) => _duelFocus = enemy;

        private void LateUpdate()
        {
            if (_player == null) return;

            Vector3 lookTarget;
            Vector3 desiredPosition;

            if (_duelFocus != null)
            {
                Vector3 midpoint = (_player.position + _duelFocus.position) * 0.5f;
                Vector3 toEnemy = _duelFocus.position - _player.position;
                toEnemy.y = 0f;
                Quaternion facing = toEnemy.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(toEnemy, Vector3.up)
                    : _player.rotation;
                desiredPosition = midpoint + facing * _duelOffset;
                lookTarget = midpoint + Vector3.up * 0.8f;
            }
            else
            {
                desiredPosition = _player.position + _player.rotation * _exploreOffset;
                lookTarget = _player.position + Vector3.up * 1.2f;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);
            Quaternion desiredRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, _lookSmoothSpeed * Time.deltaTime);
        }
    }
}
