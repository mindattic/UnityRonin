using UnityEngine;
using UnityRonin.Core.Movement;

namespace UnityRonin.Runtime.Player
{
    /// <summary>
    /// Third-person movement for the Samurai. While exploring, the stick drives it camera-relative.
    /// While dueling, the game itself drives <see cref="AutoMoveTarget"/> and the stick is ignored, since
    /// combat positioning is automatic and the player's thumb is busy swiping.
    ///
    /// The explore camera trails the Samurai's facing, so reading the camera's live yaw every frame would
    /// feed the turn back into the steering: holding the stick sideways would turn the Samurai, swing the
    /// camera behind him, re-aim "sideways" and run him in circles. So the camera yaw is latched when the
    /// thumb lands (<see cref="SteeringHeld"/>) and held for the whole drag: the Samurai runs where the
    /// thumb points relative to the view as it was when the drag began, and the camera is free to swing
    /// in behind him meanwhile. A new drag picks up the camera's new yaw (U.RON-T21).
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

        /// <summary>When dueling, keep facing this world position (squared up to the enemy) rather than
        /// turning to the direction of the automatic movement.</summary>
        public Vector3? AutoFaceTarget { get; set; }

        /// <summary>Whether the thumb is down on the stick. The steering yaw is latched while it is.</summary>
        public bool SteeringHeld
        {
            get => _steeringHeld;
            set
            {
                if (value && !_steeringHeld) _latchedYaw = LiveCameraYaw;
                _steeringHeld = value;
            }
        }

        /// <summary>The camera yaw (degrees) the stick is read against right now: the one latched when the
        /// current drag began, or the camera's live yaw when no drag is held.</summary>
        public float SteeringYaw => _steeringHeld ? _latchedYaw : LiveCameraYaw;

        private float LiveCameraYaw => _cameraYawSource != null ? _cameraYawSource.eulerAngles.y : transform.eulerAngles.y;

        private bool _steeringHeld;
        private float _latchedYaw;

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

            IsMoving = horizontal.sqrMagnitude > 0.0001f;
            Vector3 facing = horizontal;
            if (!AcceptsStickInput && AutoFaceTarget is Vector3 faceTarget)
            {
                facing = faceTarget - transform.position;
                facing.y = 0f;
            }
            if (facing.sqrMagnitude > 0.0001f)
            {
                var targetRotation = Quaternion.LookRotation(facing, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    _turnDegreesPerSecond * Time.deltaTime);
            }

            _verticalVelocity = _controller.isGrounded ? -0.5f : _verticalVelocity + _gravity * Time.deltaTime;

            Vector3 motion = horizontal * _moveSpeed + Vector3.up * _verticalVelocity;
            DebugLastDeltaTime = Time.deltaTime;
            DebugFrameCount++;
            DebugLastCollisionFlags = _controller.Move(motion * Time.deltaTime);
        }

        private Vector3 ComputeStickMotion()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(StickInput, SteeringYaw);
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
