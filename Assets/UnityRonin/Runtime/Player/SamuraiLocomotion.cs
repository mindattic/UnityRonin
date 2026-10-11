using UnityEngine;
using UnityRonin.Core.Movement;

namespace UnityRonin.Runtime.Player
{
    /// <summary>
    /// Third-person movement for the Samurai. While exploring, the stick drives it camera-relative.
    /// While dueling, the game itself drives <see cref="AutoMoveTarget"/> and the stick is ignored, since
    /// combat positioning is automatic and the player's thumb is busy swiping.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SamuraiLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform _cameraYawSource;
        [SerializeField] private float _moveSpeed = 4.5f;
        [SerializeField] private float _turnDegreesPerSecond = 540f;
        [SerializeField] private float _gravity = -20f;

        public StickVector StickInput { get; set; } = StickVector.Zero;
        public bool AcceptsStickInput { get; set; } = true;

        /// <summary>When dueling, drive movement toward this world position instead of the stick.</summary>
        public Vector3? AutoMoveTarget { get; set; }

        private CharacterController _controller;
        private float _verticalVelocity;

        public bool IsMoving { get; private set; }
        public Vector3 DebugLastHorizontal { get; private set; }
        public Vector3 DebugControllerVelocity => _controller.velocity;
        public bool DebugIsGrounded => _controller.isGrounded;
        public CollisionFlags DebugLastCollisionFlags { get; private set; }
        public float DebugLastDeltaTime { get; private set; }
        public int DebugFrameCount { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector3 horizontal = AcceptsStickInput ? ComputeStickMotion() : ComputeAutoMotion();
            DebugLastHorizontal = horizontal;

            if (horizontal.sqrMagnitude > 0.0001f)
            {
                IsMoving = true;
                var targetRotation = Quaternion.LookRotation(horizontal, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    _turnDegreesPerSecond * Time.deltaTime);
            }
            else
            {
                IsMoving = false;
            }

            _verticalVelocity = _controller.isGrounded ? -0.5f : _verticalVelocity + _gravity * Time.deltaTime;

            Vector3 motion = horizontal * _moveSpeed + Vector3.up * _verticalVelocity;
            DebugLastDeltaTime = Time.deltaTime;
            DebugFrameCount++;
            DebugLastCollisionFlags = _controller.Move(motion * Time.deltaTime);
        }

        private Vector3 ComputeStickMotion()
        {
            float yaw = _cameraYawSource != null ? _cameraYawSource.eulerAngles.y : transform.eulerAngles.y;
            var (x, z) = CameraRelativeMotion.ToWorldDirection(StickInput, yaw);
            return new Vector3(x, 0f, z);
        }

        /// <summary>Repositions the Samurai outright (e.g. a defeat knockback), bypassing normal movement.</summary>
        public void Teleport(Vector3 worldPosition)
        {
            _controller.enabled = false;
            transform.position = worldPosition;
            _controller.enabled = true;
        }

        private Vector3 ComputeAutoMotion()
        {
            if (AutoMoveTarget is not Vector3 target) return Vector3.zero;
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude < 0.05f) return Vector3.zero;
            return toTarget.normalized;
        }
    }
}
