using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// ScriptableObject template for a stat type. Create one asset per stat (HP, Mana, etc.).
    /// </summary>
    [InfoBox("Create one asset per stat type. Negative baseDecayRate = decay, positive = regen.")]
    [CreateAssetMenu(fileName = "NewStat", menuName = "Entity Base/Stat Definition")]
    public class StatDefinition : ScriptableObject
    {
        [Tooltip("Display name for this stat.")]
        public string statName = "New Stat";

        [PreviewField(50)]
        [Tooltip("Icon displayed in HUD or inspector.")]
        public Sprite icon;

        [Tooltip("Minimum value this stat can reach.")]
        public float minValue = 0f;

        [Tooltip("Maximum value this stat can reach.")]
        public float maxValue = 100f;

        [Tooltip("Default starting value.")]
        public float defaultValue = 100f;

        [Tooltip("Base decay rate per second. Negative = decay, positive = regen, 0 = static.")]
        [SuffixLabel("/sec")]
        public float baseDecayRate = 0f;

        [Tooltip("Delay in seconds after taking damage before regen begins.")]
        [ShowIf("@baseDecayRate > 0")]
        [SuffixLabel("sec")]
        public float regenDelay = 0f;

        [ColorPalette]
        [Tooltip("Color for the stat bar in the HUD.")]
        public Color barColor = Color.green;

        [Tooltip("Whether to show this stat in the player HUD.")]
        public bool showInHUD = true;
    }
}
