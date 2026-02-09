using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Shared movement motor for Player and NPC entities.
    /// Supports three locomotion modes: Ground (gravity + jumping),
    /// Swimming (buoyancy + 3D water movement), and Flying (free 3D movement).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EntityMotor : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // MOVEMENT SETTINGS
        // -------------------------------------------------------------------

        [FoldoutGroup("Movement")]
        [Tooltip("Walking speed in units per second.")]
        [PropertyRange(0f, 20f)]
        [SuffixLabel("u/s")]
        public float walkSpeed = 4f;

        [FoldoutGroup("Movement")]
        [Tooltip("Sprinting speed in units per second (when holding Sprint).")]
        [PropertyRange(0f, 30f)]
        [SuffixLabel("u/s")]
        public float sprintSpeed = 7f;

        [FoldoutGroup("Movement")]
        [Tooltip("How quickly the character rotates to face movement direction. " +
                 "Lower = snappier, higher = smoother.")]
        [PropertyRange(0.01f, 0.5f)]
        [SuffixLabel("sec")]
        public float rotationSmoothTime = 0.12f;

        [FoldoutGroup("Movement")]
        [Tooltip("How quickly the character accelerates/decelerates to the target speed.")]
        [PropertyRange(1f, 30f)]
        public float speedChangeRate = 10f;

        // -------------------------------------------------------------------
        // JUMP & GRAVITY SETTINGS
        // -------------------------------------------------------------------

        [FoldoutGroup("Jump & Gravity")]
        [Tooltip("How high the character jumps (in units).")]
        [PropertyRange(0f, 5f)]
        [SuffixLabel("units")]
        public float jumpHeight = 1.2f;

        [FoldoutGroup("Jump & Gravity")]
        [Tooltip("Gravity strength. Unity default is -9.81, but -15 feels snappier.")]
        public float gravity = -15f;

        [FoldoutGroup("Jump & Gravity")]
        [Tooltip("Offset below the character's origin for the ground-check sphere. " +
                 "For capsule primitives at y=1.08, use -1.0 so the sphere reaches the ground.")]
        [SuffixLabel("units")]
        public float groundedOffset = -1.0f;

        [FoldoutGroup("Jump & Gravity")]
        [Tooltip("Radius of the ground-check sphere.")]
        [SuffixLabel("units")]
        public float groundedRadius = 0.5f;

        [FoldoutGroup("Jump & Gravity")]
        [Tooltip("Which layers count as 'ground' for the grounded check.")]
        public LayerMask groundLayers = ~0;

        // -------------------------------------------------------------------
        // LOCOMOTION MODE
        // -------------------------------------------------------------------

        [FoldoutGroup("Locomotion")]
        [EnumToggleButtons, ReadOnly]
        [Tooltip("Current locomotion mode. Set via SetLocomotionMode().")]
        public LocomotionMode currentMode = LocomotionMode.Ground;

        // -------------------------------------------------------------------
        // SWIMMING SETTINGS
        // -------------------------------------------------------------------

        [FoldoutGroup("Swimming")]
        [Tooltip("Horizontal swim speed.")]
        [PropertyRange(0f, 15f)]
        [SuffixLabel("u/s")]
        public float swimSpeed = 3f;

        [FoldoutGroup("Swimming")]
        [Tooltip("Horizontal swim sprint speed.")]
        [PropertyRange(0f, 20f)]
        [SuffixLabel("u/s")]
        public float swimSprintSpeed = 5f;

        [FoldoutGroup("Swimming")]
        [Tooltip("Vertical swim speed (ascend/descend).")]
        [PropertyRange(0f, 10f)]
        [SuffixLabel("u/s")]
        public float swimVerticalSpeed = 2.5f;

        [FoldoutGroup("Swimming")]
        [Tooltip("Velocity damping factor per frame while swimming. Lower = more drag.")]
        [PropertyRange(0.5f, 1f)]
        public float waterDrag = 0.85f;

        [FoldoutGroup("Swimming")]
        [Tooltip("Upward force toward water surface when idle (no vertical input).")]
        [PropertyRange(0f, 10f)]
        public float buoyancy = 3f;

        [FoldoutGroup("Swimming")]
        [Tooltip("Y position of the water surface. Used for buoyancy calculations.")]
        public float waterSurfaceY = 0f;

        // -------------------------------------------------------------------
        // FLYING SETTINGS
        // -------------------------------------------------------------------

        [FoldoutGroup("Flying")]
        [Tooltip("Horizontal fly speed.")]
        [PropertyRange(0f, 20f)]
        [SuffixLabel("u/s")]
        public float flySpeed = 6f;

        [FoldoutGroup("Flying")]
        [Tooltip("Horizontal fly sprint speed.")]
        [PropertyRange(0f, 30f)]
        [SuffixLabel("u/s")]
        public float flySprintSpeed = 12f;

        [FoldoutGroup("Flying")]
        [Tooltip("Vertical fly speed (ascend/descend).")]
        [PropertyRange(0f, 15f)]
        [SuffixLabel("u/s")]
        public float flyVerticalSpeed = 5f;

        // -------------------------------------------------------------------
        // DEBUG
        // -------------------------------------------------------------------

        [FoldoutGroup("Debug"), ReadOnly]
        [ProgressBar(0f, 10f, ColorGetter = "GetSpeedBarColor")]
        public float currentSpeed;

        [FoldoutGroup("Debug"), ReadOnly]
        public bool isGrounded;

        [FoldoutGroup("Debug"), ReadOnly]
        public float verticalVelocity;

        // -------------------------------------------------------------------
        // PRIVATE STATE
        // -------------------------------------------------------------------

        private float _speed;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private bool _grounded;

        private const float TERMINAL_VELOCITY = -53f;

        private static readonly Collider[] _groundCheckBuffer = new Collider[8];

        private CharacterController _controller;
        private Camera _mainCamera;
        private IMovementInput _input;
        private CameraPerspectiveManager _perspectiveManager;
        private EntityEvents _events;
        private bool _wasGrounded;
        private bool _wasSprinting;

        // Swim/fly velocity
        private Vector3 _swimFlyVelocity;

        // -------------------------------------------------------------------
        // UNITY LIFECYCLE
        // -------------------------------------------------------------------

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _mainCamera = Camera.main;
            _input = GetComponent<IMovementInput>();
            _perspectiveManager = GetComponent<CameraPerspectiveManager>();
            _events = GetComponent<EntityEvents>();

            if (_input == null)
                Debug.LogWarning($"EntityMotor on '{name}': No IMovementInput component found.");
        }

        private void Update()
        {
            if (_input == null) return;

            switch (currentMode)
            {
                case LocomotionMode.Ground:
                    UpdateGround();
                    break;
                case LocomotionMode.Swimming:
                    UpdateSwimming();
                    break;
                case LocomotionMode.Flying:
                    UpdateFlying();
                    break;
            }

            // Update debug values
            currentSpeed = _speed;
            isGrounded = _grounded;
            verticalVelocity = _verticalVelocity;

            // Fire events (ground mode only for grounded/sprint transitions)
            if (_events != null && currentMode == LocomotionMode.Ground)
            {
                if (_grounded && !_wasGrounded) _events.RaiseLanded();
                bool sprinting = _input.SprintInput && _input.MoveInput != Vector2.zero;
                if (sprinting != _wasSprinting) _events.RaiseSprintChanged(sprinting);
                _wasSprinting = sprinting;
            }
            _wasGrounded = _grounded;
        }

        // -------------------------------------------------------------------
        // PUBLIC API
        // -------------------------------------------------------------------

        /// <summary>
        /// Switch locomotion mode. Resets velocity and fires events.
        /// </summary>
        public void SetLocomotionMode(LocomotionMode mode)
        {
            if (mode == currentMode) return;

            var previous = currentMode;
            currentMode = mode;

            // Reset state for the new mode
            _verticalVelocity = 0f;
            _speed = 0f;
            _swimFlyVelocity = Vector3.zero;
            _grounded = false;

            _events?.RaiseLocomotionModeChanged(previous, mode);
        }

        // -------------------------------------------------------------------
        // GROUND MODE
        // -------------------------------------------------------------------

        private void UpdateGround()
        {
            GroundedCheck();
            ApplyGravity();
            MoveHorizontal(walkSpeed, sprintSpeed);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = transform.position + Vector3.up * groundedOffset;
            int count = Physics.OverlapSphereNonAlloc(
                spherePosition, groundedRadius, _groundCheckBuffer,
                groundLayers, QueryTriggerInteraction.Ignore);
            _grounded = false;
            for (int i = 0; i < count; i++)
            {
                if (!_groundCheckBuffer[i].transform.IsChildOf(transform) &&
                    _groundCheckBuffer[i].gameObject != gameObject)
                {
                    _grounded = true;
                    break;
                }
            }
        }

        private void ApplyGravity()
        {
            if (_grounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                if (_input.JumpInput)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    _input.ConsumeJump();
                    _events?.RaiseJumped();
                }
            }
            else
            {
                if (_verticalVelocity > TERMINAL_VELOCITY)
                    _verticalVelocity += gravity * Time.deltaTime;

                // Consume jump input while airborne so it doesn't buffer
                if (_input.JumpInput)
                    _input.ConsumeJump();
            }
        }

        // -------------------------------------------------------------------
        // SWIMMING MODE
        // -------------------------------------------------------------------

        private void UpdateSwimming()
        {
            _grounded = false;

            float targetHSpeed = _input.SprintInput ? swimSprintSpeed : swimSpeed;
            Vector2 moveInput = _input.MoveInput;
            if (moveInput == Vector2.zero) targetHSpeed = 0f;

            // Horizontal direction (camera-relative)
            Vector3 horizontalDir = GetCameraRelativeDirection(moveInput);

            // Smooth horizontal speed
            float currentH = new Vector3(_swimFlyVelocity.x, 0f, _swimFlyVelocity.z).magnitude;
            float speedOffset = 0.1f;
            if (currentH < targetHSpeed - speedOffset || currentH > targetHSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentH, targetHSpeed, Time.deltaTime * speedChangeRate);
            }
            else
            {
                _speed = targetHSpeed;
            }

            Vector3 targetHorizontalVelocity = horizontalDir * _speed;

            // Vertical movement
            float targetVertical = 0f;
            if (_input.AscendInput)
                targetVertical = swimVerticalSpeed;
            else if (_input.DescendInput)
                targetVertical = -swimVerticalSpeed;
            else
            {
                // Buoyancy: push toward water surface when idle
                float distToSurface = waterSurfaceY - transform.position.y;
                targetVertical = Mathf.Clamp(distToSurface * buoyancy, -swimVerticalSpeed, swimVerticalSpeed);
            }

            // Apply drag and smooth
            float dragFactor = 1f - Mathf.Pow(1f - waterDrag, Time.deltaTime * 60f);
            _swimFlyVelocity.x = Mathf.Lerp(_swimFlyVelocity.x, targetHorizontalVelocity.x, dragFactor);
            _swimFlyVelocity.z = Mathf.Lerp(_swimFlyVelocity.z, targetHorizontalVelocity.z, dragFactor);
            _swimFlyVelocity.y = Mathf.Lerp(_swimFlyVelocity.y, targetVertical, Time.deltaTime * speedChangeRate);

            _verticalVelocity = _swimFlyVelocity.y;

            // Rotation
            if (moveInput != Vector2.zero)
                RotateToward(horizontalDir);

            // Consume jump (no ground jumping while swimming)
            if (_input.JumpInput)
                _input.ConsumeJump();

            _controller.Move(_swimFlyVelocity * Time.deltaTime);
        }

        // -------------------------------------------------------------------
        // FLYING MODE
        // -------------------------------------------------------------------

        private void UpdateFlying()
        {
            _grounded = false;

            float targetHSpeed = _input.SprintInput ? flySprintSpeed : flySpeed;
            Vector2 moveInput = _input.MoveInput;
            if (moveInput == Vector2.zero) targetHSpeed = 0f;

            // Horizontal direction (camera-relative)
            Vector3 horizontalDir = GetCameraRelativeDirection(moveInput);

            // Smooth horizontal speed
            float currentH = new Vector3(_swimFlyVelocity.x, 0f, _swimFlyVelocity.z).magnitude;
            float speedOffset = 0.1f;
            if (currentH < targetHSpeed - speedOffset || currentH > targetHSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentH, targetHSpeed, Time.deltaTime * speedChangeRate);
            }
            else
            {
                _speed = targetHSpeed;
            }

            Vector3 targetHorizontalVelocity = horizontalDir * _speed;

            // Vertical movement
            float targetVertical = 0f;
            if (_input.AscendInput)
                targetVertical = flyVerticalSpeed;
            else if (_input.DescendInput)
                targetVertical = -flyVerticalSpeed;
            // else hold altitude (targetVertical stays 0)

            // Smooth transitions
            _swimFlyVelocity.x = Mathf.Lerp(_swimFlyVelocity.x, targetHorizontalVelocity.x, Time.deltaTime * speedChangeRate);
            _swimFlyVelocity.z = Mathf.Lerp(_swimFlyVelocity.z, targetHorizontalVelocity.z, Time.deltaTime * speedChangeRate);
            _swimFlyVelocity.y = Mathf.Lerp(_swimFlyVelocity.y, targetVertical, Time.deltaTime * speedChangeRate);

            _verticalVelocity = _swimFlyVelocity.y;

            // Rotation
            if (moveInput != Vector2.zero)
                RotateToward(horizontalDir);

            // Consume jump (no ground jumping while flying)
            if (_input.JumpInput)
                _input.ConsumeJump();

            _controller.Move(_swimFlyVelocity * Time.deltaTime);
        }

        // -------------------------------------------------------------------
        // SHARED HORIZONTAL MOVEMENT (Ground mode)
        // -------------------------------------------------------------------

        private void MoveHorizontal(float baseSpeed, float sprintSpd)
        {
            Vector2 moveInput = _input.MoveInput;

            float targetSpeed = _input.SprintInput ? sprintSpd : baseSpeed;
            if (moveInput == Vector2.zero) targetSpeed = 0f;

            // Smooth acceleration/deceleration
            float currentHorizontalSpeed =
                new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed,
                    Time.deltaTime * speedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // Direction calculation
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            // Check perspective mode (player only)
            bool isFirstPerson = _perspectiveManager != null && _perspectiveManager.IsFirstPerson;

            // Determine the reference yaw for camera-relative movement
            float referenceYaw;
            if (_mainCamera != null)
            {
                referenceYaw = _mainCamera.transform.eulerAngles.y;
            }
            else
            {
                // NPC fallback: use entity's own forward direction
                referenceYaw = transform.eulerAngles.y;
            }

            if (moveInput != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                                  + referenceYaw;

                if (!isFirstPerson)
                {
                    // Third-person (or NPC): rotate to face movement direction
                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y, _targetRotation,
                        ref _rotationVelocity, rotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);
                }
            }
            else if (isFirstPerson)
            {
                _targetRotation = referenceYaw;
            }

            // Apply movement
            Vector3 moveDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
            Vector3 verticalMove = new Vector3(0f, _verticalVelocity, 0f);

            _controller.Move(
                moveDirection.normalized * (_speed * Time.deltaTime) +
                verticalMove * Time.deltaTime);
        }

        // -------------------------------------------------------------------
        // HELPERS
        // -------------------------------------------------------------------

        private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (moveInput == Vector2.zero) return Vector3.zero;

            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            float referenceYaw;
            if (_mainCamera != null)
                referenceYaw = _mainCamera.transform.eulerAngles.y;
            else
                referenceYaw = transform.eulerAngles.y;

            return (Quaternion.Euler(0f, referenceYaw, 0f) * inputDirection).normalized;
        }

        private void RotateToward(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return;

            bool isFirstPerson = _perspectiveManager != null && _perspectiveManager.IsFirstPerson;
            if (isFirstPerson) return;

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle,
                ref _rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        // -------------------------------------------------------------------
        // ODIN HELPERS
        // -------------------------------------------------------------------

        private Color GetSpeedBarColor(float value)
        {
            return Color.Lerp(Color.green, Color.red, value / sprintSpeed);
        }
    }
}
