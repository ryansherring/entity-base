using System;
using UnityEngine;

namespace EntityBase
{
    /// <summary>
    /// Central event bus for entity lifecycle and gameplay events.
    /// External systems (AI, combat, dialogue) subscribe to these to react
    /// without coupling to package internals.
    /// </summary>
    public class EntityEvents : MonoBehaviour
    {
        // --- Stat Events ---

        /// <summary>Fired when any stat value changes. Args: stat instance, old value, new value.</summary>
        public event Action<StatInstance, float, float> OnStatChanged;

        /// <summary>Fired when a stat reaches its minimum value. Args: stat instance.</summary>
        public event Action<StatInstance> OnStatDepleted;

        /// <summary>Fired when a stat reaches its maximum value. Args: stat instance.</summary>
        public event Action<StatInstance> OnStatFull;

        // --- Inventory Events ---

        /// <summary>Fired when an item is added to inventory. Args: item, amount added.</summary>
        public event Action<InventoryItem, int> OnItemAdded;

        /// <summary>Fired when an item is removed from inventory. Args: item, amount removed.</summary>
        public event Action<InventoryItem, int> OnItemRemoved;

        /// <summary>Fired when a consumable item is used. Args: item.</summary>
        public event Action<InventoryItem> OnItemConsumed;

        // --- Ability Events ---

        /// <summary>Fired when an ability starts (or begins casting). Args: ability definition.</summary>
        public event Action<AbilityDefinition> OnAbilityStarted;

        /// <summary>Fired when an ability finishes executing. Args: ability definition.</summary>
        public event Action<AbilityDefinition> OnAbilityExecuted;

        /// <summary>Fired when an ability fails (insufficient resources, on cooldown). Args: ability definition, reason.</summary>
        public event Action<AbilityDefinition, string> OnAbilityFailed;

        // --- Movement Events ---

        /// <summary>Fired when the entity lands after being airborne.</summary>
        public event Action OnLanded;

        /// <summary>Fired when the entity jumps.</summary>
        public event Action OnJumped;

        /// <summary>Fired when the entity starts or stops sprinting. Args: is sprinting.</summary>
        public event Action<bool> OnSprintChanged;

        // --- Locomotion Events ---

        /// <summary>Fired when the locomotion mode changes. Args: previous mode, new mode.</summary>
        public event Action<LocomotionMode, LocomotionMode> OnLocomotionModeChanged;

        /// <summary>Fired when the entity enters swimming mode.</summary>
        public event Action OnStartedSwimming;

        /// <summary>Fired when the entity enters flying mode.</summary>
        public event Action OnStartedFlying;

        /// <summary>Fired when the entity returns to ground locomotion.</summary>
        public event Action OnReturnedToGround;

        // --- NPC Events ---

        /// <summary>Fired when an NPC reaches its destination.</summary>
        public event Action OnDestinationReached;

        /// <summary>Fired when an NPC is given a new destination. Args: destination position.</summary>
        public event Action<Vector3> OnDestinationSet;

        // --- Invoke Helpers (called by package internals) ---

        internal void RaiseStatChanged(StatInstance stat, float oldValue, float newValue) =>
            OnStatChanged?.Invoke(stat, oldValue, newValue);

        internal void RaiseStatDepleted(StatInstance stat) =>
            OnStatDepleted?.Invoke(stat);

        internal void RaiseStatFull(StatInstance stat) =>
            OnStatFull?.Invoke(stat);

        internal void RaiseItemAdded(InventoryItem item, int amount) =>
            OnItemAdded?.Invoke(item, amount);

        internal void RaiseItemRemoved(InventoryItem item, int amount) =>
            OnItemRemoved?.Invoke(item, amount);

        internal void RaiseItemConsumed(InventoryItem item) =>
            OnItemConsumed?.Invoke(item);

        internal void RaiseAbilityStarted(AbilityDefinition def) =>
            OnAbilityStarted?.Invoke(def);

        internal void RaiseAbilityExecuted(AbilityDefinition def) =>
            OnAbilityExecuted?.Invoke(def);

        internal void RaiseAbilityFailed(AbilityDefinition def, string reason) =>
            OnAbilityFailed?.Invoke(def, reason);

        internal void RaiseLanded() => OnLanded?.Invoke();
        internal void RaiseJumped() => OnJumped?.Invoke();
        internal void RaiseSprintChanged(bool sprinting) => OnSprintChanged?.Invoke(sprinting);
        internal void RaiseDestinationReached() => OnDestinationReached?.Invoke();
        internal void RaiseDestinationSet(Vector3 dest) => OnDestinationSet?.Invoke(dest);

        internal void RaiseLocomotionModeChanged(LocomotionMode from, LocomotionMode to)
        {
            OnLocomotionModeChanged?.Invoke(from, to);
            switch (to)
            {
                case LocomotionMode.Swimming: OnStartedSwimming?.Invoke(); break;
                case LocomotionMode.Flying: OnStartedFlying?.Invoke(); break;
                case LocomotionMode.Ground: OnReturnedToGround?.Invoke(); break;
            }
        }
    }
}
