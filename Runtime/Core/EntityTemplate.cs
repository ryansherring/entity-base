using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    [System.Serializable]
    public class ItemEntry
    {
        [AssetSelector]
        public InventoryItem item;

        [PropertyRange(1, 999)]
        public int count = 1;
    }

    /// <summary>
    /// ScriptableObject defining an entity archetype. Used by EntityTemplateSpawner
    /// to create fully configured entities from data (WC3-style unit type).
    /// </summary>
    [CreateAssetMenu(fileName = "NewEntityTemplate", menuName = "Entity Base/Entity Template")]
    public class EntityTemplate : ScriptableObject
    {
        [EnumToggleButtons]
        public EntityType entityType = EntityType.NPC;

        [Required]
        public string displayName = "New Entity";

        [AssetsOnly]
        [Tooltip("Optional model prefab to spawn as a child of the entity.")]
        public GameObject modelPrefab;

        [FoldoutGroup("Animation")]
        [Tooltip("Animator controller for this entity's model.")]
        public RuntimeAnimatorController animatorController;

        [FoldoutGroup("Animation")]
        [Tooltip("Override controller to swap animation clips without changing the base controller.")]
        public AnimatorOverrideController animatorOverride;

        [FoldoutGroup("Movement")]
        [SuffixLabel("u/s")]
        public float walkSpeed = 4f;

        [FoldoutGroup("Movement")]
        [SuffixLabel("u/s")]
        public float sprintSpeed = 7f;

        [FoldoutGroup("Movement")]
        [SuffixLabel("u/s")]
        public float swimSpeed = 3f;

        [FoldoutGroup("Movement")]
        [SuffixLabel("u/s")]
        public float swimSprintSpeed = 5f;

        [FoldoutGroup("Movement")]
        [SuffixLabel("u/s")]
        public float flySpeed = 6f;

        [FoldoutGroup("Movement")]
        [SuffixLabel("u/s")]
        public float flySprintSpeed = 12f;

        [FoldoutGroup("Stats")]
        [AssetSelector]
        public List<StatDefinition> statDefinitions = new List<StatDefinition>();

        [FoldoutGroup("Inventory")]
        [TableList]
        public List<ItemEntry> startingItems = new List<ItemEntry>();

        [FoldoutGroup("Abilities")]
        [AssetSelector]
        public List<AbilityDefinition> startingAbilities = new List<AbilityDefinition>();

        [FoldoutGroup("Sensors")]
        [SerializeReference]
        public List<SensorConfig> sensorConfigs = new List<SensorConfig>();
    }
}
