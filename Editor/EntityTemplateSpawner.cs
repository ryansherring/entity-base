using UnityEngine;
using UnityEditor;
using EntityBase;

namespace EntityBase.Editor
{
    /// <summary>
    /// Spawns entities from EntityTemplate assets. Provides both a menu item
    /// with object picker and a custom editor with a Spawn button.
    /// </summary>
    public static class EntityTemplateSpawner
    {
        [MenuItem("Tools/Entity Base/Spawn From Template")]
        public static void SpawnFromTemplateMenu()
        {
            EditorGUIUtility.ShowObjectPicker<EntityTemplate>(null, false, "", 0);
            EditorApplication.update += OnPickerUpdate;
        }

        private static void OnPickerUpdate()
        {
            if (EditorGUIUtility.GetObjectPickerControlID() == 0)
            {
                EditorApplication.update -= OnPickerUpdate;
                return;
            }

            var ev = Event.current;
            if (ev != null && ev.commandName == "ObjectSelectorClosed")
            {
                var template = EditorGUIUtility.GetObjectPickerObject() as EntityTemplate;
                if (template != null)
                    SpawnFromTemplate(template);
                EditorApplication.update -= OnPickerUpdate;
            }
        }

        public static GameObject SpawnFromTemplate(EntityTemplate template)
        {
            if (template == null) return null;

            // Use the existing entity creation flow, then apply template values
            if (template.entityType == EntityType.Player)
                EntityCreator.CreatePlayer();
            else
                EntityCreator.CreateNPC();

            var entity = Selection.activeGameObject;
            if (entity == null) return null;

            // Identity + Animation
            var identity = entity.GetComponent<EntityIdentity>();
            if (identity != null)
            {
                identity.displayName = template.displayName;
                identity.modelPrefab = template.modelPrefab;
                identity.animatorController = template.animatorController;
                identity.animatorOverride = template.animatorOverride;

                // Auto-spawn model if prefab is assigned
                if (template.modelPrefab != null)
                    identity.SpawnModel();
            }

            // Motor
            var motor = entity.GetComponent<EntityMotor>();
            if (motor != null)
            {
                motor.walkSpeed = template.walkSpeed;
                motor.sprintSpeed = template.sprintSpeed;
                motor.swimSpeed = template.swimSpeed;
                motor.swimSprintSpeed = template.swimSprintSpeed;
                motor.flySpeed = template.flySpeed;
                motor.flySprintSpeed = template.flySprintSpeed;
            }

            // Attributes
            var attributes = entity.GetComponent<EntityAttributes>();
            if (attributes != null && template.statDefinitions?.Count > 0)
                attributes.Initialize(template.statDefinitions);

            // Inventory
            var inventory = entity.GetComponent<Inventory>();
            if (inventory != null && template.startingItems != null)
            {
                foreach (var entry in template.startingItems)
                {
                    if (entry.item != null)
                        inventory.AddItem(entry.item, entry.count);
                }
            }

            // Abilities
            var abilitySystem = entity.GetComponent<AbilitySystem>();
            if (abilitySystem != null && template.startingAbilities != null)
            {
                foreach (var abilityDef in template.startingAbilities)
                {
                    if (abilityDef != null)
                        abilitySystem.abilities.Add(new AbilitySlot { definition = abilityDef });
                }
            }

            // Sensors - remove defaults from EntityCreator, apply template's configs
            var existingSensors = entity.GetComponents<SensorBase>();
            foreach (var s in existingSensors)
                Object.DestroyImmediate(s);

            if (template.sensorConfigs != null)
            {
                foreach (var config in template.sensorConfigs)
                    config?.Apply(entity);
            }

            entity.name = template.displayName;

            Debug.Log($"[Entity Template] Spawned '{template.displayName}' from template '{template.name}'.");
            return entity;
        }
    }
}
