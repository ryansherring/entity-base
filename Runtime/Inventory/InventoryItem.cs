using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// ScriptableObject item definition with stacking and consumable support.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Entity Base/Inventory Item")]
    public class InventoryItem : ScriptableObject
    {
        [HorizontalGroup("Header", Width = 64)]
        [PreviewField(64)]
        [HideLabel]
        public Sprite icon;

        [VerticalGroup("Header/Info")]
        [Tooltip("Display name for this item.")]
        public string itemName = "New Item";

        [VerticalGroup("Header/Info")]
        [TextArea(2, 4)]
        [Tooltip("Item description.")]
        public string description;

        [Tooltip("Maximum number of this item that can stack in one slot.")]
        [PropertyRange(1, 999)]
        public int maxStackSize = 1;

        [Tooltip("Whether this item can be consumed (used) from inventory.")]
        public bool consumable = false;

        [ShowIf("consumable")]
        [FoldoutGroup("Consumable Effect")]
        [Tooltip("Which stat this item affects when consumed.")]
        [AssetSelector]
        public StatDefinition affectedStat;

        [ShowIf("consumable")]
        [FoldoutGroup("Consumable Effect")]
        [Tooltip("Amount to add to the stat when consumed. Positive = heal/restore, negative = damage.")]
        public float statEffectAmount = 25f;
    }
}
