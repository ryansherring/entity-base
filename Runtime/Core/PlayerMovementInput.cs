using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Implements IMovementInput via Unity Input System.
    /// Reads from the "Player" action map for Move, Jump, Sprint, and Descend actions.
    /// AscendInput is true while Jump/Space is held (not just pressed).
    /// </summary>
    public class PlayerMovementInput : MonoBehaviour, IMovementInput
    {
        [Header("Input")]
        [Required]
        [Tooltip("Reference to the InputActionAsset containing the 'Player' action map. " +
                 "Must have 'Move', 'Jump', and 'Sprint' actions defined. " +
                 "'Descend' action is optional (falls back to no descend input).")]
        public InputActionAsset inputActions;

        // Cached input values
        private Vector2 _moveInput;
        private bool _jumpInput;
        private bool _sprintInput;
        private bool _jumpHeld;

        // Resolved InputAction references
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _descendAction;

        public Vector2 MoveInput => _moveInput;
        public bool JumpInput => _jumpInput;
        public bool SprintInput => _sprintInput;
        public bool AscendInput => _jumpHeld;
        public bool DescendInput => _descendAction != null && _descendAction.IsPressed();

        public void ConsumeJump()
        {
            _jumpInput = false;
        }

        private void Awake()
        {
            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    _moveAction = playerMap.FindAction("Move");
                    _jumpAction = playerMap.FindAction("Jump");
                    _sprintAction = playerMap.FindAction("Sprint");
                    _descendAction = playerMap.FindAction("Descend") ?? playerMap.FindAction("Crouch");
                }
                else
                {
                    Debug.LogWarning("PlayerMovementInput: 'Player' action map not found.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerMovementInput: No InputActionAsset assigned.");
            }
        }

        private void OnEnable()
        {
            if (_moveAction != null)
            {
                _moveAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
                _moveAction.canceled += ctx => _moveInput = Vector2.zero;
                _moveAction.Enable();
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += ctx =>
                {
                    _jumpInput = true;
                    _jumpHeld = true;
                };
                _jumpAction.canceled += ctx => _jumpHeld = false;
                _jumpAction.Enable();
            }

            if (_sprintAction != null)
            {
                _sprintInput = false;
                _sprintAction.performed += ctx => _sprintInput = true;
                _sprintAction.canceled += ctx => _sprintInput = false;
                _sprintAction.Enable();
            }

            _descendAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _sprintAction?.Disable();
            _descendAction?.Disable();
        }
    }
}
