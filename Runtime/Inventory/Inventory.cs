using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Represents a single slot in the inventory.
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [TableColumnWidth(60, Resizable = false)]
        [PreviewField(45)]
        [ReadOnly]
        [HideLabel]
        public Sprite Icon => item != null ? item.icon : null;

        [TableColumnWidth(120)]
        [AssetSelector]
        public InventoryItem item;

        [TableColumnWidth(60, Resizable = false)]
        [PropertyRange(0, "MaxStack")]
        public int count;

        private int MaxStack => item != null ? item.maxStackSize : 1;
    }

    /// <summary>
    /// Functional inventory system with stacking and consumable items.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [Tooltip("Maximum number of inventory slots.")]
        [PropertyRange(1, 100)]
        public int maxSlots = 20;

        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public List<InventorySlot> slots = new List<InventorySlot>();

        private EntityAttributes _attributes;
        private EntityEvents _events;

        private void Awake()
        {
            _attributes = GetComponent<EntityAttributes>();
            _events = GetComponent<EntityEvents>();
        }

        /// <summary>
        /// Add an item to inventory. Stacks with existing items if possible.
        /// Returns the number of items that could NOT be added (overflow).
        /// </summary>
        public int AddItem(InventoryItem item, int amount = 1)
        {
            if (item == null || amount <= 0) return amount;

            int remaining = amount;

            // First pass: try to stack with existing slots
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].item == item && slots[i].count < item.maxStackSize)
                {
                    int canAdd = Mathf.Min(remaining, item.maxStackSize - slots[i].count);
                    slots[i].count += canAdd;
                    remaining -= canAdd;
                }
            }

            // Second pass: create new slots for remaining items
            while (remaining > 0 && slots.Count < maxSlots)
            {
                int canAdd = Mathf.Min(remaining, item.maxStackSize);
                slots.Add(new InventorySlot { item = item, count = canAdd });
                remaining -= canAdd;
            }

            int added = amount - remaining;
            if (added > 0)
                _events?.RaiseItemAdded(item, added);

            return remaining;
        }

        /// <summary>
        /// Remove a specific amount of an item. Returns true if successful.
        /// </summary>
        public bool RemoveItem(InventoryItem item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            int totalAvailable = 0;
            foreach (var slot in slots)
            {
                if (slot.item == item) totalAvailable += slot.count;
            }

            if (totalAvailable < amount) return false;

            int remaining = amount;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (slots[i].item == item)
                {
                    int canRemove = Mathf.Min(remaining, slots[i].count);
                    slots[i].count -= canRemove;
                    remaining -= canRemove;

                    if (slots[i].count <= 0)
                        slots.RemoveAt(i);
                }
            }

            _events?.RaiseItemRemoved(item, amount);
            return true;
        }

        /// <summary>
        /// Check if inventory contains at least a certain amount of an item.
        /// </summary>
        public bool HasItem(InventoryItem item, int amount = 1)
        {
            if (item == null) return false;

            int total = 0;
            foreach (var slot in slots)
            {
                if (slot.item == item) total += slot.count;
            }
            return total >= amount;
        }

        /// <summary>
        /// Get total count of a specific item across all slots.
        /// </summary>
        public int GetItemCount(InventoryItem item)
        {
            if (item == null) return 0;

            int total = 0;
            foreach (var slot in slots)
            {
                if (slot.item == item) total += slot.count;
            }
            return total;
        }

        /// <summary>
        /// Consume one of the specified item. Applies stat effect if the item is consumable.
        /// Returns true if consumed successfully.
        /// </summary>
        public bool ConsumeItem(InventoryItem item)
        {
            if (item == null || !item.consumable) return false;
            if (!RemoveItem(item, 1)) return false;

            // Apply stat effect
            if (item.affectedStat != null && _attributes != null)
            {
                StatInstance stat = FindStatByDefinition(item.affectedStat);
                if (stat != null)
                {
                    stat.Add(item.statEffectAmount);
                }
            }

            _events?.RaiseItemConsumed(item);
            return true;
        }

        [FoldoutGroup("Test")]
        [Button("Add Test Item")]
        private void AddTestItem([AssetSelector] InventoryItem testItem, int amount = 1)
        {
            if (testItem == null) return;
            AddItem(testItem, amount);
        }

        [Button("Clear Inventory")]
        private void ClearInventory()
        {
            slots.Clear();
        }

        private StatInstance FindStatByDefinition(StatDefinition def)
        {
            return _attributes?.GetStat(def);
        }
    }
}
