using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Per-entity runtime stat data. Inline on EntityAttributes.
    /// Handles ticking (decay/regen) and value manipulation.
    /// </summary>
    [System.Serializable]
    public class StatInstance
    {
        [HorizontalGroup("Row", Width = 0.3f)]
        [HideLabel]
        [ReadOnly]
        public string label;

        [HorizontalGroup("Row")]
        [ProgressBar(0f, "maxValue", ColorGetter = "GetBarColor")]
        [HideLabel]
        public float currentValue;

        [AssetSelector]
        public StatDefinition definition;

        [Tooltip("Multiplier on the base decay rate. 1.0 = normal, 2.0 = double speed.")]
        [PropertyRange(0f, 5f)]
        public float decayModifier = 1.0f;

        // Backing fields
        private float _regenTimer;

        // Used by ProgressBar attribute
        private float maxValue => definition != null ? definition.maxValue : 100f;

        public StatInstance() { }

        public StatInstance(StatDefinition def)
        {
            definition = def;
            label = def != null ? def.statName : "?";
            currentValue = def != null ? def.defaultValue : 0f;
        }

        /// <summary>Set value, clamped to min/max.</summary>
        public void SetValue(float value)
        {
            if (definition == null) return;
            currentValue = Mathf.Clamp(value, definition.minValue, definition.maxValue);
        }

        /// <summary>Add to current value (clamped).</summary>
        public void Add(float amount)
        {
            SetValue(currentValue + amount);
            // Reset regen timer when healed
            if (amount > 0) _regenTimer = 0f;
        }

        /// <summary>Subtract from current value (clamped). Resets regen delay timer.</summary>
        public void Subtract(float amount)
        {
            SetValue(currentValue - amount);
            // Reset regen delay when damaged
            if (definition != null && definition.regenDelay > 0f)
                _regenTimer = definition.regenDelay;
        }

        /// <summary>
        /// Tick this stat by deltaTime. Applies baseDecayRate * decayModifier.
        /// Respects regenDelay for positive (regen) rates.
        /// </summary>
        public void Tick(float dt)
        {
            if (definition == null) return;
            if (definition.baseDecayRate == 0f) return;

            // If the rate is positive (regen), check regen delay
            if (definition.baseDecayRate > 0f && _regenTimer > 0f)
            {
                _regenTimer -= dt;
                return;
            }

            float change = definition.baseDecayRate * decayModifier * dt;
            SetValue(currentValue + change);
        }

        /// <summary>Reset to the definition's default value.</summary>
        public void ResetToDefault()
        {
            if (definition != null)
                currentValue = definition.defaultValue;
        }

        /// <summary>Get normalized value (0-1) for UI bars.</summary>
        public float GetNormalized()
        {
            if (definition == null) return 0f;
            float range = definition.maxValue - definition.minValue;
            if (range <= 0f) return 0f;
            return (currentValue - definition.minValue) / range;
        }

        private Color GetBarColor(float value)
        {
            return definition != null ? definition.barColor : Color.green;
        }
    }
}
