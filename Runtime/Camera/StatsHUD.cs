using System.Collections.Generic;
using UnityEngine;

namespace EntityBase
{
    /// <summary>
    /// IMGUI-based stat bar display. Shows colored bars for stats with showInHUD enabled.
    /// Player-only component.
    /// </summary>
    public class StatsHUD : MonoBehaviour
    {
        [Header("Display Settings")]
        [Range(0f, 1f)]
        [Tooltip("Background opacity for the bar background.")]
        public float backgroundOpacity = 0.4f;

        [Tooltip("Width of each stat bar in pixels.")]
        public float barWidth = 200f;

        [Tooltip("Height of each stat bar in pixels.")]
        public float barHeight = 20f;

        [Tooltip("Spacing between bars in pixels.")]
        public float spacing = 4f;

        [Tooltip("Font size for stat labels.")]
        public int fontSize = 12;

        private EntityAttributes _attributes;
        private Dictionary<Color, Texture2D> _colorTextures = new Dictionary<Color, Texture2D>();
        private Texture2D _bgTexture;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;

        private void Awake()
        {
            _attributes = GetComponent<EntityAttributes>();
        }

        private void OnGUI()
        {
            if (_attributes == null || _attributes.stats.Count == 0) return;

            if (!_stylesInitialized)
                InitStyles();

            float x = 12f;
            float y = 12f;

            for (int i = 0; i < _attributes.stats.Count; i++)
            {
                var stat = _attributes.stats[i];
                if (stat.definition == null || !stat.definition.showInHUD) continue;

                DrawStatBar(x, y, stat);
                y += barHeight + spacing;
            }
        }

        private void DrawStatBar(float x, float y, StatInstance stat)
        {
            // Background
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), _bgTexture);

            // Filled portion
            float fill = stat.GetNormalized();
            if (fill > 0f)
            {
                var barTex = GetColorTexture(stat.definition.barColor);
                GUI.DrawTexture(new Rect(x, y, barWidth * fill, barHeight), barTex);
            }

            // Label
            string label = $"{stat.definition.statName} {Mathf.CeilToInt(stat.currentValue)}/{Mathf.CeilToInt(stat.definition.maxValue)}";
            GUI.Label(new Rect(x + 4f, y, barWidth - 8f, barHeight), label, _labelStyle);
        }

        private Texture2D GetColorTexture(Color color)
        {
            if (_colorTextures.TryGetValue(color, out var tex))
                return tex;

            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            _colorTextures[color] = tex;
            return tex;
        }

        private void InitStyles()
        {
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, backgroundOpacity));
            _bgTexture.Apply();

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = fontSize;
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.alignment = TextAnchor.MiddleLeft;

            _stylesInitialized = true;
        }
    }
}
