using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using EntityBase;
using Pathfinding;

namespace EntityBase.Editor
{
    /// <summary>
    /// Menu items under Tools/Entity Base/ for creating Player and NPC entities,
    /// and setting up test scenes.
    /// </summary>
    public static class EntityCreator
    {
        // ===================================================================
        // CREATE PLAYER
        // ===================================================================

        [MenuItem("Tools/Entity Base/Create Player")]
        public static void CreatePlayer()
        {
            // Empty root GameObject
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.08f, 0f);

            // Model child (swappable visual)
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Model";
            model.transform.SetParent(player.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var capsuleCollider = model.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null) Object.DestroyImmediate(capsuleCollider);

            // Core components
            var cc = player.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.height = 2f;
            cc.radius = 0.5f;

            // Entity identity
            var identity = player.AddComponent<EntityIdentity>();
            identity.entityType = EntityType.Player;
            identity.displayName = "Player";

            // Motor + Events
            player.AddComponent<EntityMotor>();
            player.AddComponent<EntityEvents>();

            // Player input
            var playerInput = player.AddComponent<PlayerMovementInput>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (inputAsset != null)
                playerInput.inputActions = inputAsset;
            else
                Debug.LogWarning("[Entity Creator] InputSystem_Actions.inputactions not found at Assets/InputSystem_Actions.inputactions");

            // Camera perspective manager
            player.AddComponent<CameraPerspectiveManager>();

            // Attributes, Inventory, Abilities
            player.AddComponent<EntityAttributes>();
            player.AddComponent<Inventory>();
            player.AddComponent<AbilitySystem>();

            // HUD components
            player.AddComponent<ControlsHUD>();
            player.AddComponent<StatsHUD>();

            // Animator bridge
            player.AddComponent<EntityAnimator>();

            // Sensors
            player.AddComponent<SightSensor>();
            player.AddComponent<HearingSensor>();

            // Material (on model child)
            AssignEntityMaterial(model, "PlayerMaterial", new Color(0.2f, 0.4f, 0.8f));

            // HeadTarget child for first-person camera
            GameObject headTarget = new GameObject("HeadTarget");
            headTarget.transform.SetParent(player.transform);
            headTarget.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            Undo.RegisterCreatedObjectUndo(player, "Create Player Entity");
            Selection.activeGameObject = player;

            Debug.Log("[Entity Creator] Player entity created with all components.");
        }

        // ===================================================================
        // CREATE NPC
        // ===================================================================

        [MenuItem("Tools/Entity Base/Create NPC")]
        public static void CreateNPC()
        {
            // Empty root GameObject
            GameObject npc = new GameObject("NPC");
            npc.transform.position = new Vector3(3f, 1.08f, 3f);

            // Model child (swappable visual)
            GameObject npcModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcModel.name = "Model";
            npcModel.transform.SetParent(npc.transform);
            npcModel.transform.localPosition = Vector3.zero;
            npcModel.transform.localRotation = Quaternion.identity;
            var capsuleCollider = npcModel.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null) Object.DestroyImmediate(capsuleCollider);

            // Core components
            var cc = npc.AddComponent<CharacterController>();
            cc.center = Vector3.zero;
            cc.height = 2f;
            cc.radius = 0.5f;

            // Entity identity
            var identity = npc.AddComponent<EntityIdentity>();
            identity.entityType = EntityType.NPC;
            identity.displayName = "NPC";

            // Motor + Events
            npc.AddComponent<EntityMotor>();
            npc.AddComponent<EntityEvents>();

            // NPC input (adds Seeker + AIPath)
            // AIPath.canMove = false lets EntityMotor handle movement via CharacterController.
            // AIPath.updatePosition stays true so AIPath reads the CharacterController-moved
            // transform position for correct path calculations.
            npc.AddComponent<Seeker>();
            var aiPath = npc.AddComponent<AIPath>();
            aiPath.canMove = false;
            aiPath.updateRotation = false;
            npc.AddComponent<NPCMovementInput>();

            // Attributes, Inventory, Abilities
            npc.AddComponent<EntityAttributes>();
            npc.AddComponent<Inventory>();
            npc.AddComponent<AbilitySystem>();

            // Animator bridge
            npc.AddComponent<EntityAnimator>();

            // Sensors
            npc.AddComponent<SightSensor>();
            npc.AddComponent<HearingSensor>();

            // Material (on model child)
            AssignEntityMaterial(npcModel, "NPCMaterial", new Color(0.8f, 0.3f, 0.2f));

            // No camera components for NPC

            Undo.RegisterCreatedObjectUndo(npc, "Create NPC Entity");
            Selection.activeGameObject = npc;

            Debug.Log("[Entity Creator] NPC entity created with all components (including A* pathfinding).");
        }

        // ===================================================================
        // SETUP TEST SCENE
        // ===================================================================

        [MenuItem("Tools/Entity Base/Setup Test Scene")]
        public static void SetupTestScene()
        {
            // --- Materials ---
            string matFolder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder))
                AssetDatabase.CreateFolder("Assets", "Materials");

            Material groundMat = CreateURPMaterial("GroundMaterial",
                new Color(0.35f, 0.45f, 0.35f), matFolder);
            Material playerMat = CreateURPMaterial("PlayerMaterial",
                new Color(0.2f, 0.4f, 0.8f), matFolder);
            Material npcMat = CreateURPMaterial("NPCMaterial",
                new Color(0.8f, 0.3f, 0.2f), matFolder);

            // --- Clean up previous runs ---
            DestroyExisting("Ground");
            DestroyExisting("Player");
            DestroyExisting("NPC");
            DestroyExisting("ThirdPersonCinemachineCamera");
            DestroyExisting("FirstPersonCinemachineCamera");

            // --- Ground ---
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

            // --- Player ---
            CreatePlayer();
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var playerRenderer = player.GetComponentInChildren<Renderer>();
                if (playerRenderer != null)
                    playerRenderer.sharedMaterial = playerMat;
            }

            // --- NPC ---
            CreateNPC();
            var npc = GameObject.Find("NPC");
            if (npc != null)
            {
                var npcRenderer = npc.GetComponentInChildren<Renderer>();
                if (npcRenderer != null)
                    npcRenderer.sharedMaterial = npcMat;
            }

            // --- Third-Person Camera ---
            GameObject tpCamObj = new GameObject("ThirdPersonCinemachineCamera");
            tpCamObj.transform.position = new Vector3(0f, 2.5f, -5f);
            Undo.RegisterCreatedObjectUndo(tpCamObj, "Create Third-Person Camera");

            var tpCam = tpCamObj.AddComponent<CinemachineCamera>();
            if (player != null)
            {
                var tpTarget = tpCam.Target;
                tpTarget.TrackingTarget = player.transform;
                tpTarget.LookAtTarget = player.transform;
                tpCam.Target = tpTarget;
            }
            tpCam.Priority.Value = 10;
            tpCam.Lens = new LensSettings
            {
                FieldOfView = 50f,
                NearClipPlane = 0.1f,
                FarClipPlane = 1000f,
            };

            var orbitalFollow = tpCamObj.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.TargetOffset = new Vector3(0f, 1.4f, 0f);
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.Radius = 8f;

            orbitalFollow.VerticalAxis = new InputAxis
            {
                Value = 20f,
                Range = new Vector2(-20f, 60f),
                Center = 20f,
                Wrap = false,
                Recentering = new InputAxis.RecenteringSettings { Enabled = false }
            };

            orbitalFollow.HorizontalAxis = new InputAxis
            {
                Value = 0f,
                Range = new Vector2(-180f, 180f),
                Center = 0f,
                Wrap = true,
                Recentering = new InputAxis.RecenteringSettings { Enabled = false }
            };

            orbitalFollow.RadialAxis = new InputAxis
            {
                Value = 1.0f,
                Range = new Vector2(0.3f, 3.0f),
                Center = 1.0f,
                Wrap = false,
                Recentering = new InputAxis.RecenteringSettings { Enabled = false }
            };

            var trackerSettings = orbitalFollow.TrackerSettings;
            trackerSettings.PositionDamping = new Vector3(0.3f, 0.3f, 0.3f);
            trackerSettings.RotationDamping = new Vector3(0f, 0.2f, 0f);
            orbitalFollow.TrackerSettings = trackerSettings;

            var tpRotComposer = tpCamObj.AddComponent<CinemachineRotationComposer>();
            tpRotComposer.TargetOffset = new Vector3(0f, 1.4f, 0f);
            tpRotComposer.Damping = new Vector2(0f, 0f);
            tpRotComposer.CenterOnActivate = true;
            tpRotComposer.Composition.ScreenPosition = new Vector2(0.5f, 0.5f);

            tpCamObj.AddComponent<CinemachineInputAxisController>();

            // --- First-Person Camera ---
            GameObject fpCamObj = new GameObject("FirstPersonCinemachineCamera");
            var headTarget = player != null ? player.transform.Find("HeadTarget") : null;
            fpCamObj.transform.position = headTarget != null
                ? headTarget.position
                : new Vector3(0f, 1.83f, 0f);
            Undo.RegisterCreatedObjectUndo(fpCamObj, "Create First-Person Camera");

            var fpCam = fpCamObj.AddComponent<CinemachineCamera>();
            if (headTarget != null)
            {
                var fpTarget = fpCam.Target;
                fpTarget.TrackingTarget = headTarget;
                fpCam.Target = fpTarget;
            }
            fpCam.enabled = false;
            fpCam.Lens = new LensSettings
            {
                FieldOfView = 70f,
                NearClipPlane = 0.05f,
                FarClipPlane = 1000f,
            };

            fpCamObj.AddComponent<CinemachineHardLockToTarget>();

            var panTilt = fpCamObj.AddComponent<CinemachinePanTilt>();
            panTilt.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
            panTilt.PanAxis.Value = 0f;
            panTilt.PanAxis.Wrap = true;
            panTilt.TiltAxis.Range = new Vector2(-80f, 80f);
            panTilt.TiltAxis.Value = 0f;
            panTilt.PanAxis.Recentering.Enabled = false;
            panTilt.TiltAxis.Recentering.Enabled = false;

            fpCamObj.AddComponent<CinemachineInputAxisController>();

            // --- Wire up perspective manager ---
            if (player != null)
            {
                var perspectiveManager = player.GetComponent<CameraPerspectiveManager>();
                if (perspectiveManager != null)
                {
                    perspectiveManager.thirdPersonCamera = tpCam;
                    perspectiveManager.firstPersonCamera = fpCam;
                }
            }

            // --- Main Camera + CinemachineBrain ---
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            if (mainCam.GetComponent<CinemachineBrain>() == null)
            {
                var brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
                brain.DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);
            }

            // --- Finalize ---
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            if (player != null)
                Selection.activeGameObject = player;

            Debug.Log("[Entity Creator] Test scene setup complete!\n" +
                      "  - Ground (100x100 plane)\n" +
                      "  - Player (EntityMotor + PlayerMovementInput + CameraPerspectiveManager)\n" +
                      "  - NPC (EntityMotor + NPCMovementInput + A* Pathfinding)\n" +
                      "  - ThirdPersonCinemachineCamera (OrbitalFollow)\n" +
                      "  - FirstPersonCinemachineCamera (PanTilt)\n" +
                      "  - Main Camera (CinemachineBrain, 0.5s blend)\n\n" +
                      "Press Play -> WASD to move, Mouse to look, V to toggle perspective.");
        }

        // ===================================================================
        // CREATE DEFAULT STAT ASSETS
        // ===================================================================

        [MenuItem("Tools/Entity Base/Create Default Stat Assets")]
        public static void CreateDefaultStatAssets()
        {
            string folder = "Assets/ScriptableObjects/Stats";
            EnsureFolderExists(folder);

            CreateStatAsset(folder, "HP", 0f, 100f, 100f, 1f, 2f, new Color(0.8f, 0.2f, 0.2f), true);
            CreateStatAsset(folder, "Energy", 0f, 100f, 100f, 5f, 0f, new Color(1f, 0.8f, 0.2f), true);
            CreateStatAsset(folder, "Mana", 0f, 100f, 100f, 2f, 3f, new Color(0.2f, 0.4f, 1f), true);
            CreateStatAsset(folder, "Rest", 0f, 100f, 100f, 0f, 0f, new Color(0.6f, 0.5f, 0.8f), false);
            CreateStatAsset(folder, "Hunger", 0f, 100f, 100f, -0.5f, 0f, new Color(0.6f, 0.4f, 0.2f), false);
            CreateStatAsset(folder, "Thirst", 0f, 100f, 100f, -0.8f, 0f, new Color(0.3f, 0.6f, 0.9f), false);
            CreateStatAsset(folder, "Happiness", 0f, 100f, 75f, 0f, 0f, new Color(1f, 0.6f, 0.8f), false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Entity Creator] Created 7 default StatDefinition assets in {folder}/");
        }

        [MenuItem("Tools/Entity Base/Create Sample Items")]
        public static void CreateSampleItems()
        {
            string folder = "Assets/ScriptableObjects/Items";
            EnsureFolderExists(folder);

            var hpDef = FindStatDefinition("HP");

            // Health Potion
            var healthPotion = ScriptableObject.CreateInstance<InventoryItem>();
            healthPotion.itemName = "Health Potion";
            healthPotion.description = "Restores 25 HP when consumed.";
            healthPotion.maxStackSize = 10;
            healthPotion.consumable = true;
            healthPotion.affectedStat = hpDef;
            healthPotion.statEffectAmount = 25f;
            AssetDatabase.CreateAsset(healthPotion, $"{folder}/HealthPotion.asset");

            // Bread
            var bread = ScriptableObject.CreateInstance<InventoryItem>();
            bread.itemName = "Bread";
            bread.description = "A simple loaf of bread.";
            bread.maxStackSize = 20;
            bread.consumable = false;
            AssetDatabase.CreateAsset(bread, $"{folder}/Bread.asset");

            // Iron Sword
            var sword = ScriptableObject.CreateInstance<InventoryItem>();
            sword.itemName = "Iron Sword";
            sword.description = "A sturdy iron sword.";
            sword.maxStackSize = 1;
            sword.consumable = false;
            AssetDatabase.CreateAsset(sword, $"{folder}/IronSword.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Entity Creator] Created 3 sample InventoryItem assets in {folder}/");
        }

        [MenuItem("Tools/Entity Base/Create Sample Abilities")]
        public static void CreateSampleAbilities()
        {
            string folder = "Assets/ScriptableObjects/Abilities";
            EnsureFolderExists(folder);

            var manaDef = FindStatDefinition("Mana");
            var energyDef = FindStatDefinition("Energy");

            // Fireball
            var fireball = ScriptableObject.CreateInstance<AbilityDefinition>();
            fireball.abilityName = "Fireball";
            fireball.description = "Launches a ball of fire at the target.";
            fireball.targetType = AbilityTargetType.Directional;
            fireball.costs = new System.Collections.Generic.List<AbilityCost>();
            if (manaDef != null) fireball.costs.Add(new AbilityCost { stat = manaDef, amount = 30f });
            if (energyDef != null) fireball.costs.Add(new AbilityCost { stat = energyDef, amount = 10f });
            fireball.cooldown = 3f;
            fireball.castTime = 0.5f;
            AssetDatabase.CreateAsset(fireball, $"{folder}/Fireball.asset");

            // Heal
            var heal = ScriptableObject.CreateInstance<AbilityDefinition>();
            heal.abilityName = "Heal";
            heal.description = "Restores health over time.";
            heal.targetType = AbilityTargetType.Self;
            heal.costs = new System.Collections.Generic.List<AbilityCost>();
            if (manaDef != null) heal.costs.Add(new AbilityCost { stat = manaDef, amount = 20f });
            if (energyDef != null) heal.costs.Add(new AbilityCost { stat = energyDef, amount = 5f });
            heal.cooldown = 8f;
            heal.castTime = 1.5f;
            AssetDatabase.CreateAsset(heal, $"{folder}/Heal.asset");

            // Dash
            var dash = ScriptableObject.CreateInstance<AbilityDefinition>();
            dash.abilityName = "Dash";
            dash.description = "Quick dash forward.";
            dash.targetType = AbilityTargetType.Self;
            dash.costs = new System.Collections.Generic.List<AbilityCost>();
            if (energyDef != null) dash.costs.Add(new AbilityCost { stat = energyDef, amount = 25f });
            dash.cooldown = 5f;
            dash.castTime = 0f;
            AssetDatabase.CreateAsset(dash, $"{folder}/Dash.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Entity Creator] Created 3 sample AbilityDefinition assets in {folder}/");
        }

        // ===================================================================
        // HELPERS
        // ===================================================================

        private static void EnsureFolderExists(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void CreateStatAsset(string folder, string statName,
            float min, float max, float defaultVal, float decayRate, float regenDelay,
            Color barColor, bool showInHUD)
        {
            string path = $"{folder}/{statName}.asset";
            if (AssetDatabase.LoadAssetAtPath<StatDefinition>(path) != null)
            {
                Debug.Log($"[Entity Creator] {statName} stat already exists, skipping.");
                return;
            }

            var stat = ScriptableObject.CreateInstance<StatDefinition>();
            stat.statName = statName;
            stat.minValue = min;
            stat.maxValue = max;
            stat.defaultValue = defaultVal;
            stat.baseDecayRate = decayRate;
            stat.regenDelay = regenDelay;
            stat.barColor = barColor;
            stat.showInHUD = showInHUD;
            AssetDatabase.CreateAsset(stat, path);
        }

        private static StatDefinition FindStatDefinition(string statName)
        {
            string[] guids = AssetDatabase.FindAssets($"t:StatDefinition {statName}");
            foreach (string guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<StatDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && def.statName == statName)
                    return def;
            }
            return null;
        }

        private static Material CreateURPMaterial(string name, Color color, string folder)
        {
            string path = $"{folder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetColor("_BaseColor", color);
                return existing;
            }
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = name;
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void AssignEntityMaterial(GameObject entity, string matName, Color color)
        {
            string matFolder = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder))
                AssetDatabase.CreateFolder("Assets", "Materials");

            Material mat = CreateURPMaterial(matName, color, matFolder);
            var renderer = entity.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
        }

        private static void DestroyExisting(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }
    }
}
