using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    public enum AbilityTargetType
    {
        Self,
        SingleTarget,
        AreaOfEffect,
        Directional
    }

    /// <summary>
    /// A single stat cost entry for an ability (e.g. 30 Mana, 10 Energy).
    /// </summary>
    [System.Serializable]
    public class AbilityCost
    {
        [AssetSelector]
        public StatDefinition stat;

        [PropertyRange(0f, 200f)]
        public float amount;
    }

    /// <summary>
    /// ScriptableObject ability template defining an ability's properties and costs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Entity Base/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [HorizontalGroup("Header", Width = 64)]
        [PreviewField(64)]
        [HideLabel]
        public Sprite icon;

        [VerticalGroup("Header/Info")]
        [Tooltip("Display name for this ability.")]
        public string abilityName = "New Ability";

        [VerticalGroup("Header/Info")]
        [TextArea(2, 4)]
        [Tooltip("Ability description.")]
        public string description;

        [VerticalGroup("Header/Info")]
        [EnumToggleButtons]
        public AbilityTargetType targetType = AbilityTargetType.Self;

        [FoldoutGroup("Costs")]
        [TableList]
        public List<AbilityCost> costs = new List<AbilityCost>();

        [FoldoutGroup("Timing")]
        [Tooltip("Cooldown in seconds before this ability can be used again.")]
        [PropertyRange(0f, 300f)]
        [SuffixLabel("sec")]
        public float cooldown = 1f;

        [FoldoutGroup("Timing")]
        [Tooltip("Cast time in seconds. 0 = instant.")]
        [PropertyRange(0f, 10f)]
        [SuffixLabel("sec")]
        public float castTime = 0f;

        [FoldoutGroup("Effect")]
        [AssetsOnly]
        [Tooltip("Optional prefab to spawn when this ability is used.")]
        public GameObject effectPrefab;
    }
}
