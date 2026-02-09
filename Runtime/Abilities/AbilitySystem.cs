using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Per-entity ability slot with runtime cooldown tracking.
    /// </summary>
    [System.Serializable]
    public class AbilitySlot
    {
        [TableColumnWidth(60, Resizable = false)]
        [PreviewField(45)]
        [ReadOnly]
        [HideLabel]
        public Sprite Icon => definition != null ? definition.icon : null;

        [TableColumnWidth(120)]
        [AssetSelector]
        public AbilityDefinition definition;

        [TableColumnWidth(100, Resizable = false)]
        [ProgressBar(0f, "MaxCooldown", ColorGetter = "GetCooldownColor")]
        [ReadOnly]
        [HideLabel]
        public float cooldownRemaining;

        [HideInInspector]
        public bool isCasting;

        [HideInInspector]
        public float castTimeRemaining;

        private float MaxCooldown => definition != null ? definition.cooldown : 1f;

        public bool IsReady => cooldownRemaining <= 0f && !isCasting;

        private Color GetCooldownColor(float value)
        {
            return value > 0f ? Color.yellow : Color.green;
        }
    }

    /// <summary>
    /// Manages abilities for an entity. Handles cooldowns, resource costs, and effect spawning.
    /// </summary>
    [RequireComponent(typeof(EntityAttributes))]
    public class AbilitySystem : MonoBehaviour
    {
        [PropertyRange(1, 20)]
        [Tooltip("Maximum number of ability slots this entity can have.")]
        public int maxAbilitySlots = 6;

        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public List<AbilitySlot> abilities = new List<AbilitySlot>();

        private EntityAttributes _attributes;
        private EntityEvents _events;

        private void Awake()
        {
            _attributes = GetComponent<EntityAttributes>();
            _events = GetComponent<EntityEvents>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < abilities.Count; i++)
            {
                var slot = abilities[i];

                // Tick cooldown
                if (slot.cooldownRemaining > 0f)
                    slot.cooldownRemaining = Mathf.Max(0f, slot.cooldownRemaining - dt);

                // Tick cast time
                if (slot.isCasting)
                {
                    slot.castTimeRemaining -= dt;
                    if (slot.castTimeRemaining <= 0f)
                    {
                        slot.isCasting = false;
                        ExecuteAbility(slot);
                    }
                }
            }
        }

        /// <summary>
        /// Try to use an ability by slot index.
        /// Returns true if the ability was successfully activated (or cast started).
        /// </summary>
        public bool TryUseAbility(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= abilities.Count) return false;

            var slot = abilities[slotIndex];
            if (slot.definition == null) return false;

            if (!slot.IsReady)
            {
                _events?.RaiseAbilityFailed(slot.definition, "On cooldown or casting");
                return false;
            }

            var def = slot.definition;

            // Check all resource costs
            if (_attributes != null && def.costs != null)
            {
                for (int i = 0; i < def.costs.Count; i++)
                {
                    var cost = def.costs[i];
                    if (cost.stat == null || cost.amount <= 0f) continue;

                    var stat = _attributes.GetStat(cost.stat);
                    if (stat == null) continue; // stat not on this entity, skip

                    if (stat.currentValue < cost.amount)
                    {
                        _events?.RaiseAbilityFailed(def, $"Insufficient {cost.stat.statName}");
                        return false;
                    }
                }

                // Deduct costs
                for (int i = 0; i < def.costs.Count; i++)
                {
                    var cost = def.costs[i];
                    if (cost.stat == null || cost.amount <= 0f) continue;

                    var stat = _attributes.GetStat(cost.stat);
                    if (stat != null)
                        stat.Subtract(cost.amount);
                }
            }

            // Start cooldown
            slot.cooldownRemaining = def.cooldown;
            _events?.RaiseAbilityStarted(def);

            // Handle cast time
            if (def.castTime > 0f)
            {
                slot.isCasting = true;
                slot.castTimeRemaining = def.castTime;
            }
            else
            {
                ExecuteAbility(slot);
            }

            return true;
        }

        /// <summary>
        /// Try to use an ability by definition reference.
        /// </summary>
        public bool TryUseAbility(AbilityDefinition abilityDef)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].definition == abilityDef)
                    return TryUseAbility(i);
            }
            return false;
        }

        [FoldoutGroup("Test")]
        [Button("Use Ability")]
        private void UseAbilityTest([PropertyRange(0, 19)] int slotIndex = 0)
        {
            TryUseAbility(slotIndex);
        }

        [Button("Reset All Cooldowns")]
        private void ResetAllCooldowns()
        {
            foreach (var slot in abilities)
            {
                slot.cooldownRemaining = 0f;
                slot.isCasting = false;
                slot.castTimeRemaining = 0f;
            }
        }

        private void ExecuteAbility(AbilitySlot slot)
        {
            if (slot.definition == null) return;

            // Spawn effect prefab if assigned
            if (slot.definition.effectPrefab != null)
            {
                var effectInstance = Instantiate(
                    slot.definition.effectPrefab,
                    transform.position,
                    transform.rotation);

                // Auto-destroy effects after 5 seconds
                Destroy(effectInstance, 5f);
            }

            _events?.RaiseAbilityExecuted(slot.definition);
        }
    }
}
