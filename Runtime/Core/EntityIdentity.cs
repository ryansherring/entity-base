using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    public enum EntityType
    {
        Player,
        NPC
    }

    /// <summary>
    /// Root component on every entity. Determines Player vs NPC and holds identity info.
    /// </summary>
    [InfoBox("Root component for all entities. Set type to Player or NPC to configure available components.")]
    public class EntityIdentity : MonoBehaviour
    {
        [EnumToggleButtons]
        [Tooltip("Whether this entity is a Player or NPC.")]
        public EntityType entityType = EntityType.Player;

        [Tooltip("Display name for this entity.")]
        public string displayName = "Entity";

        [AssetsOnly]
        [Tooltip("Optional model prefab to spawn as a child of this entity.")]
        public GameObject modelPrefab;

        [FoldoutGroup("Animation")]
        [Tooltip("Animator controller for this entity's model.")]
        public RuntimeAnimatorController animatorController;

        [FoldoutGroup("Animation")]
        [Tooltip("Override controller to swap animation clips without changing the base controller.")]
        public AnimatorOverrideController animatorOverride;

        public bool IsPlayer => entityType == EntityType.Player;
        public bool IsNPC => entityType == EntityType.NPC;
        public string DisplayName => displayName;

        [Button("Spawn Model"), ShowIf("modelPrefab")]
        public void SpawnModel()
        {
            // Remove existing model children first
            RemoveModel();

            if (modelPrefab != null)
            {
                var instance = Instantiate(modelPrefab, transform);
                instance.name = "Model";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                SetupAnimator(instance);
            }
        }

        private void SetupAnimator(GameObject model)
        {
            if (animatorController == null && animatorOverride == null) return;

            var animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();

            if (animatorOverride != null)
                animator.runtimeAnimatorController = animatorOverride;
            else if (animatorController != null)
                animator.runtimeAnimatorController = animatorController;
        }

        /// <summary>
        /// Returns the Animator on the model child, if any.
        /// </summary>
        public Animator GetModelAnimator()
        {
            var model = transform.Find("Model");
            return model != null ? model.GetComponent<Animator>() : null;
        }

        [Button("Remove Model")]
        private void RemoveModel()
        {
            var existing = transform.Find("Model");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }
        }
    }
}
