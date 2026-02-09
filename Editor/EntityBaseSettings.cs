using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EntityBase;
using System.IO;

namespace EntityBase.Editor
{
    /// <summary>
    /// Project settings provider for Entity Base.
    /// Accessible via Edit > Project Settings > Entity Base.
    /// </summary>
    public class EntityBaseSettings : ScriptableObject
    {
        [Header("Default Entity Settings")]
        [Tooltip("Default walk speed for newly created entities.")]
        public float defaultWalkSpeed = 4f;

        [Tooltip("Default sprint speed for newly created entities.")]
        public float defaultSprintSpeed = 7f;

        [Tooltip("Default jump height for newly created entities.")]
        public float defaultJumpHeight = 1.2f;

        [Header("Default Stat Definitions")]
        [Tooltip("Default stat definitions used by Initialize Defaults and template spawning.")]
        public List<StatDefinition> defaultStats = new List<StatDefinition>();

        [Header("Editor Preferences")]
        [Tooltip("Show SceneView gizmos for entities.")]
        public bool showGizmos = true;

        [Tooltip("Auto-validate entities on creation.")]
        public bool autoValidate = true;

        [Tooltip("Path to InputActionAsset for new player entities.")]
        public string inputActionsPath = "Assets/InputSystem_Actions.inputactions";

        // --- Singleton access ---
        private static EntityBaseSettings _instance;

        public static EntityBaseSettings GetOrCreate()
        {
            if (_instance != null) return _instance;

            string path = "Assets/Settings/EntityBaseSettings.asset";
            _instance = AssetDatabase.LoadAssetAtPath<EntityBaseSettings>(path);

            if (_instance == null)
            {
                string dir = Path.GetDirectoryName(path);
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder("Assets", "Settings");

                _instance = CreateInstance<EntityBaseSettings>();
                AssetDatabase.CreateAsset(_instance, path);
                AssetDatabase.SaveAssets();
            }

            return _instance;
        }
    }

    /// <summary>
    /// Registers Entity Base in Project Settings window.
    /// </summary>
    public class EntityBaseSettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedSettings;

        public EntityBaseSettingsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new EntityBaseSettingsProvider("Project/Entity Base", SettingsScope.Project)
            {
                keywords = new[] { "entity", "controller", "player", "npc", "movement", "stats" }
            };
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedSettings == null || _serializedSettings.targetObject == null)
            {
                var settings = EntityBaseSettings.GetOrCreate();
                _serializedSettings = new SerializedObject(settings);
            }

            _serializedSettings.Update();

            EditorGUILayout.LabelField("Entity Base Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Draw all properties
            var iterator = _serializedSettings.GetIterator();
            iterator.NextVisible(true); // Skip "m_Script"
            while (iterator.NextVisible(false))
            {
                EditorGUILayout.PropertyField(iterator, true);
            }

            if (_serializedSettings.hasModifiedProperties)
            {
                _serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedSettings.targetObject);
            }
        }
    }
}
