using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Holds all stats for an entity as a dynamic list.
    /// Stats are configured via EntityTemplate or added at runtime.
    /// </summary>
    [InfoBox("Add StatDefinition assets to the stats list. Cross-stat decay modifiers apply automatically for NPCs.")]
    [RequireComponent(typeof(EntityIdentity))]
    public class EntityAttributes : MonoBehaviour
    {
        [TableList]
        public List<StatInstance> stats = new List<StatInstance>();

        private EntityIdentity _identity;

        private bool IsNPC
        {
            get
            {
                if (_identity == null) _identity = GetComponent<EntityIdentity>();
                return _identity != null && _identity.IsNPC;
            }
        }

        private void Awake()
        {
            _identity = GetComponent<EntityIdentity>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < stats.Count; i++)
                stats[i].Tick(dt);

            if (IsNPC)
                UpdateDecayModifiers();
        }

        /// <summary>
        /// Look up a stat by its definition asset reference.
        /// </summary>
        public StatInstance GetStat(StatDefinition def)
        {
            if (def == null) return null;
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].definition == def)
                    return stats[i];
            }
            return null;
        }

        /// <summary>
        /// Look up a stat by its definition's statName string.
        /// </summary>
        public StatInstance GetStat(string statName)
        {
            if (string.IsNullOrEmpty(statName)) return null;
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].definition != null && stats[i].definition.statName == statName)
                    return stats[i];
            }
            return null;
        }

        /// <summary>
        /// Add a stat from a definition. Returns the new StatInstance.
        /// </summary>
        public StatInstance AddStat(StatDefinition def)
        {
            if (def == null) return null;
            var instance = new StatInstance(def);
            stats.Add(instance);
            return instance;
        }

        /// <summary>
        /// Remove a stat by its definition.
        /// </summary>
        public bool RemoveStat(StatDefinition def)
        {
            if (def == null) return false;
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].definition == def)
                {
                    stats.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clear all stats and initialize from a list of definitions.
        /// Each stat starts at its definition's defaultValue.
        /// </summary>
        public void Initialize(List<StatDefinition> definitions)
        {
            stats.Clear();
            if (definitions == null) return;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null)
                    AddStat(definitions[i]);
            }
        }

        /// <summary>
        /// Cross-stat influence per SPEC section 5.4:
        /// - Low energy (&lt; 30%) -> hunger decayModifier *= 1.5
        /// - Low hunger (&lt; 20%) -> energy decayModifier *= 1.3
        /// - Low thirst (&lt; 20%) -> energy decayModifier *= 1.3, happiness decayModifier *= 1.5
        /// - High rest + high hunger + high thirst -> happiness decayModifier *= 0.5
        /// </summary>
        private void UpdateDecayModifiers()
        {
            var energy = GetStat("Energy");
            var hunger = GetStat("Hunger");
            var thirst = GetStat("Thirst");
            var rest = GetStat("Rest");
            var happiness = GetStat("Happiness");

            // Reset modifiers each frame
            if (hunger != null) hunger.decayModifier = 1.0f;
            if (energy != null) energy.decayModifier = 1.0f;
            if (happiness != null) happiness.decayModifier = 1.0f;

            // Low energy (< 30%) -> hunger decays 50% faster
            if (energy != null && energy.definition != null && energy.GetNormalized() < 0.3f)
            {
                if (hunger != null) hunger.decayModifier *= 1.5f;
            }

            // Low hunger (< 20%) -> energy decays 30% faster
            if (hunger != null && hunger.definition != null && hunger.GetNormalized() < 0.2f)
            {
                if (energy != null) energy.decayModifier *= 1.3f;
            }

            // Low thirst (< 20%) -> energy decays 30% faster, happiness decays 50% faster
            if (thirst != null && thirst.definition != null && thirst.GetNormalized() < 0.2f)
            {
                if (energy != null) energy.decayModifier *= 1.3f;
                if (happiness != null) happiness.decayModifier *= 1.5f;
            }

            // High rest + high hunger + high thirst -> happiness decays 50% slower
            if (rest != null && rest.definition != null && rest.GetNormalized() > 0.7f &&
                hunger != null && hunger.definition != null && hunger.GetNormalized() > 0.7f &&
                thirst != null && thirst.definition != null && thirst.GetNormalized() > 0.7f)
            {
                if (happiness != null) happiness.decayModifier *= 0.5f;
            }
        }

        [Button("Reset All Stats")]
        private void ResetAllStats()
        {
            for (int i = 0; i < stats.Count; i++)
                stats[i].ResetToDefault();
        }

        [FoldoutGroup("Test")]
        [Button("Damage First Stat")]
        private void DamageFirstStat(float amount = 10f)
        {
            if (stats.Count > 0)
                stats[0].Subtract(amount);
        }
    }
}
