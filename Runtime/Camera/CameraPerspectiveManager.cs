using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Manages toggling between first-person and third-person camera perspectives
    /// using Cinemachine 3. Only added to Player entities.
    /// </summary>
    public class CameraPerspectiveManager : MonoBehaviour
    {
        [Header("Cinemachine Cameras")]
        [Required]
        [Tooltip("The orbiting third-person CinemachineCamera (with CinemachineOrbitalFollow).")]
        public CinemachineCamera thirdPersonCamera;

        [Required]
        [Tooltip("The head-locked first-person CinemachineCamera (with CinemachinePanTilt).")]
        public CinemachineCamera firstPersonCamera;

        [Header("Toggle Settings")]
        [Tooltip("Key binding for the perspective toggle. Default: V")]
        public Key toggleKey = Key.V;

        /// <summary>
        /// True when the first-person camera is active.
        /// EntityMotor reads this to decide movement behavior.
        /// </summary>
        public bool IsFirstPerson { get; private set; } = false;

        private InputAction _toggleAction;
        private CinemachinePanTilt _firstPersonPanTilt;

        private void Awake()
        {
            _toggleAction = new InputAction("TogglePerspective", InputActionType.Button,
                $"<Keyboard>/{toggleKey.ToString().ToLower()}");

            if (firstPersonCamera != null)
                _firstPersonPanTilt = firstPersonCamera.GetComponent<CinemachinePanTilt>();
        }

        private void OnEnable()
        {
            _toggleAction.performed += OnTogglePerformed;
            _toggleAction.Enable();
            SetPerspective(false);
            RecenterThirdPersonCamera();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            _toggleAction.performed -= OnTogglePerformed;
            _toggleAction.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            // Escape unlocks cursor, click re-locks it
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                     && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (IsFirstPerson && _firstPersonPanTilt != null)
            {
                transform.rotation = Quaternion.Euler(0f, _firstPersonPanTilt.PanAxis.Value, 0f);
            }
        }

        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            SetPerspective(!IsFirstPerson);
        }

        /// <summary>
        /// Resets the third-person orbital camera to look directly behind the player.
        /// Invalidates Cinemachine state so the pipeline recalculates from scratch.
        /// </summary>
        private void RecenterThirdPersonCamera()
        {
            if (thirdPersonCamera == null) return;

            var orbital = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
            if (orbital != null)
            {
                orbital.HorizontalAxis.Value = orbital.HorizontalAxis.Center;
                orbital.VerticalAxis.Value = orbital.VerticalAxis.Center;
            }

            // Invalidate cached state so Cinemachine recomputes position + orientation
            thirdPersonCamera.PreviousStateIsValid = false;
        }

        /// <summary>
        /// Switches perspective by enabling/disabling CinemachineCamera components.
        /// </summary>
        public void SetPerspective(bool firstPerson)
        {
            IsFirstPerson = firstPerson;

            if (thirdPersonCamera != null)
                thirdPersonCamera.enabled = !firstPerson;

            if (firstPersonCamera != null)
                firstPersonCamera.enabled = firstPerson;

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.enabled = !firstPerson;
        }
    }
}
