using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Bridge between EntityMotor state and the Animator on the model child.
    /// Reads motor values in LateUpdate and writes cached parameter hashes.
    /// </summary>
    [RequireComponent(typeof(EntityMotor))]
    [RequireComponent(typeof(EntityIdentity))]
    public class EntityAnimator : MonoBehaviour
    {
        [FoldoutGroup("Damping")]
        [Tooltip("Smoothing time for the Speed parameter.")]
        [PropertyRange(0f, 0.5f)]
        public float speedDampTime = 0.1f;

        [FoldoutGroup("Damping")]
        [Tooltip("Smoothing time for the VerticalSpeed parameter.")]
        [PropertyRange(0f, 0.5f)]
        public float verticalSpeedDampTime = 0.05f;

        // Cached parameter hashes
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashVerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");
        private static readonly int HashLocomotionMode = Animator.StringToHash("LocomotionMode");
        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashLand = Animator.StringToHash("Land");

        private Animator _animator;
        private EntityMotor _motor;
        private EntityIdentity _identity;
        private EntityEvents _events;

        private void Start()
        {
            _motor = GetComponent<EntityMotor>();
            _identity = GetComponent<EntityIdentity>();
            _events = GetComponent<EntityEvents>();

            _animator = _identity.GetModelAnimator();

            if (_events != null)
            {
                _events.OnJumped += OnJumped;
                _events.OnLanded += OnLanded;
            }
        }

        private void OnDestroy()
        {
            if (_events != null)
            {
                _events.OnJumped -= OnJumped;
                _events.OnLanded -= OnLanded;
            }
        }

        private void LateUpdate()
        {
            if (_animator == null || !_animator.isActiveAndEnabled) return;

            _animator.SetFloat(HashSpeed, _motor.currentSpeed, speedDampTime, Time.deltaTime);
            _animator.SetFloat(HashVerticalSpeed, _motor.verticalVelocity, verticalSpeedDampTime, Time.deltaTime);
            _animator.SetBool(HashIsGrounded, _motor.isGrounded);
            _animator.SetBool(HashIsSprinting, _motor.currentSpeed > _motor.walkSpeed + 0.1f);
            _animator.SetInteger(HashLocomotionMode, (int)_motor.currentMode);
        }

        /// <summary>
        /// Call after swapping the model at runtime to re-acquire the Animator reference.
        /// </summary>
        public void RefreshAnimator()
        {
            _animator = _identity.GetModelAnimator();
        }

        private void OnJumped()
        {
            if (_animator != null && _animator.isActiveAndEnabled)
                _animator.SetTrigger(HashJump);
        }

        private void OnLanded()
        {
            if (_animator != null && _animator.isActiveAndEnabled)
                _animator.SetTrigger(HashLand);
        }
    }
}
