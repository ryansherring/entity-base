using UnityEngine;
using UnityEngine.InputSystem;

namespace EntityBase
{
    /// <summary>
    /// Displays a simple on-screen controls reference using IMGUI.
    /// Only added to Player entities. Toggled with backtick key (hidden by default).
    /// Shows mode-aware hints for swimming/flying when active.
    /// </summary>
    public class ControlsHUD : MonoBehaviour
    {
        [Header("Display Settings")]
        [Tooltip("Show or hide the controls overlay.")]
        public bool showHUD = false;

        [Tooltip("Background opacity (0 = transparent, 1 = solid).")]
        [Range(0f, 1f)]
        public float backgroundOpacity = 0.6f;

        [Tooltip("Font size for the controls text.")]
        public int fontSize = 14;

        private CameraPerspectiveManager _perspectiveManager;
        private EntityMotor _motor;
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private bool _stylesInitialized = false;

        private void Start()
        {
            _perspectiveManager = FindAnyObjectByType<CameraPerspectiveManager>();
            _motor = GetComponent<EntityMotor>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
                showHUD = !showHUD;
        }

        private void OnGUI()
        {
            if (!showHUD) return;

            if (!_stylesInitialized)
                InitStyles();

            string modeLabel = "THIRD PERSON";
            if (_perspectiveManager != null && _perspectiveManager.IsFirstPerson)
                modeLabel = "FIRST PERSON";

            // Locomotion mode indicator
            string locoLabel = "";
            if (_motor != null && _motor.currentMode != LocomotionMode.Ground)
                locoLabel = _motor.currentMode == LocomotionMode.Swimming ? " | SWIMMING" : " | FLYING";

            string controls =
                $"<b>[ {modeLabel}{locoLabel} ]</b>\n" +
                "\n" +
                "<b>WASD</b>  -  Move\n" +
                "<b>Mouse</b>  -  Look / Orbit\n" +
                "<b>Scroll</b>  -  Zoom In / Out\n";

            if (_motor != null && _motor.currentMode != LocomotionMode.Ground)
            {
                controls +=
                    "<b>Space</b>  -  Ascend\n" +
                    "<b>Ctrl/C</b>  -  Descend\n";
            }
            else
            {
                controls += "<b>Space</b>  -  Jump\n";
            }

            controls +=
                "<b>Shift</b>  -  Sprint\n" +
                "<b>V</b>  -  Toggle Perspective\n" +
                "<b>~</b>  -  Toggle Dev Mode";

            float padding = 12f;
            float boxWidth = 260f;
            float lineCount = (_motor != null && _motor.currentMode != LocomotionMode.Ground) ? 10 : 9;
            float boxHeight = 60f + lineCount * 20f;
            float x = padding;
            float y = Screen.height - boxHeight - padding;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), "", _boxStyle);
            GUI.Label(new Rect(x + 12f, y + 8f, boxWidth - 24f, boxHeight - 16f),
                controls, _textStyle);
        }

        private void InitStyles()
        {
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, backgroundOpacity));
            bgTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = bgTex;

            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.fontSize = fontSize;
            _textStyle.normal.textColor = Color.white;
            _textStyle.richText = true;
            _textStyle.wordWrap = true;
            _textStyle.alignment = TextAnchor.UpperLeft;

            _stylesInitialized = true;
        }
    }
}
