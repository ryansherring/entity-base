using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace EntityBase.Editor
{
    /// <summary>
    /// Builds locomotion test geometry, zones, and animator setup on top of the existing scene.
    /// Menu: Tools/Entity Base/Build Locomotion Test Scene
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string AnimControllerPath = "Assets/Animation/EntityBaseAnimator.controller";
        private const string AnimOverridePath = "Assets/Animation/EntityBaseAnimator_Override.overrideController";
        private const string TestClipsPath = "Assets/Animation/TestAnimationClips.asset";

        // Material paths (saved as assets so they persist)
        private const string MaterialFolder = "Assets/Materials/TestScene";

        [MenuItem("Tools/Entity Base/Build Locomotion Test Scene")]
        public static void Build()
        {
            // Clean up any previous test objects (idempotent)
            CleanupPrevious();

            // Create materials
            var waterMat = CreateMaterial("WaterMaterial", new Color(0.1f, 0.3f, 0.8f, 0.4f), transparent: true);
            var flyPadMat = CreateMaterial("FlyPadMaterial", new Color(0.7f, 0.2f, 0.7f, 1f));
            var platformMat = CreateMaterial("PlatformMaterial", new Color(0.5f, 0.45f, 0.4f, 1f));
            var wallMat = CreateMaterial("WallMaterial", new Color(0.3f, 0.3f, 0.35f, 1f));
            var rampMat = CreateMaterial("RampMaterial", new Color(0.6f, 0.5f, 0.3f, 1f));

            // --- Scene geometry ---
            var parent = new GameObject("--- Test Scene Geometry ---");
            parent.transform.position = Vector3.zero;
            parent.isStatic = true;
            Undo.RegisterCreatedObjectUndo(parent, "Build Locomotion Test Scene");

            // Water Pool (swim zone)
            var waterPool = CreateCube("WaterPool", new Vector3(15f, -0.5f, 0f), new Vector3(10f, 3f, 10f), waterMat, parent.transform);
            var waterCol = waterPool.GetComponent<BoxCollider>();
            waterCol.isTrigger = true;
            var waterZone = waterPool.AddComponent<LocomotionZone>();
            waterZone.zoneMode = LocomotionMode.Swimming;

            // Water surface visual
            var waterSurface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterSurface.name = "WaterSurface";
            waterSurface.transform.SetParent(parent.transform);
            waterSurface.transform.position = new Vector3(15f, 1f, 0f);
            waterSurface.transform.localScale = new Vector3(1f, 1f, 1f);
            waterSurface.GetComponent<Renderer>().sharedMaterial = waterMat;
            Object.DestroyImmediate(waterSurface.GetComponent<Collider>()); // no collision on surface visual

            // Fly Pad (visual platform)
            CreateCube("FlyPad", new Vector3(-12f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f), flyPadMat, parent.transform);

            // Fly Zone (invisible trigger above fly pad)
            var flyZone = CreateCube("FlyZone", new Vector3(-12f, 10f, 0f), new Vector3(20f, 20f, 20f), null, parent.transform);
            flyZone.GetComponent<BoxCollider>().isTrigger = true;
            var flyRenderer = flyZone.GetComponent<MeshRenderer>();
            flyRenderer.enabled = false;
            var flyLocZone = flyZone.AddComponent<LocomotionZone>();
            flyLocZone.zoneMode = LocomotionMode.Flying;

            // Platforms
            CreateCube("Platform1", new Vector3(0f, 2f, -8f), new Vector3(4f, 1f, 4f), platformMat, parent.transform);
            CreateCube("Platform2", new Vector3(5f, 4f, -12f), new Vector3(3f, 1f, 3f), platformMat, parent.transform);
            CreateCube("Platform3", new Vector3(-5f, 6f, -12f), new Vector3(3f, 1f, 3f), platformMat, parent.transform);

            // Ramp (rotated 20° on X)
            var ramp = CreateCube("Ramp", new Vector3(0f, 1f, -4f), new Vector3(2f, 0.1f, 6f), rampMat, parent.transform);
            ramp.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

            // Walls
            CreateCube("WallA", new Vector3(0f, 1.5f, -15f), new Vector3(12f, 3f, 0.3f), wallMat, parent.transform);
            CreateCube("WallB", new Vector3(8f, 1.5f, -8f), new Vector3(0.3f, 3f, 12f), wallMat, parent.transform);

            // Signs (TextMesh)
            CreateSign("SignWater", "SWIM ZONE", new Vector3(10f, 2.5f, 5.5f), parent.transform);
            CreateSign("SignFly", "FLY ZONE", new Vector3(-12f, 1.5f, 2.5f), parent.transform);

            // --- Animator setup ---
            SetupAnimators();

            EditorUtility.SetDirty(parent);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Entity Base] Locomotion test scene built successfully!\n" +
                      "  - Water pool at (15, -0.5, 0) — walk in to swim\n" +
                      "  - Fly zone at (-12, 10, 0) — step on magenta pad to fly\n" +
                      "  - 3 platforms, ramp, 2 walls\n" +
                      "  - Animator controllers generated and assigned to Player/NPC");
        }

        private static void CleanupPrevious()
        {
            var existing = GameObject.Find("--- Test Scene Geometry ---");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (mat != null)
                go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void CreateSign(string name, string text, Vector3 position, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            tm.fontStyle = FontStyle.Bold;
        }

        // ---------------------------------------------------------------
        // Material creation (URP Lit)
        // ---------------------------------------------------------------

        private static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            // Check if already exists
            string matPath = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                // Update color in case it changed
                existing.color = color;
                if (transparent) SetTransparent(existing, color);
                return existing;
            }

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    AssetDatabase.CreateFolder("Assets", "Materials");
                AssetDatabase.CreateFolder("Assets/Materials", "TestScene");
            }

            // Find URP Lit shader
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard"); // fallback

            var mat = new Material(shader);
            mat.name = name;
            mat.color = color;

            if (transparent)
                SetTransparent(mat, color);

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        private static void SetTransparent(Material mat, Color color)
        {
            // URP Lit transparency setup
            mat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend", 0);   // Alpha blend
            mat.SetFloat("_AlphaClip", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.color = color;
        }

        // ---------------------------------------------------------------
        // Animator setup
        // ---------------------------------------------------------------

        private static void SetupAnimators()
        {
            // 1. Generate base animator controller (or reuse existing)
            var overrideController = AnimatorControllerGenerator.GenerateAtPath(AnimControllerPath);

            // 2. Generate test animation clips
            var testClips = AnimationClipFactory.GenerateAtPath(TestClipsPath);

            // 3. Map test clips into the override controller
            // The override controller was created from the base — get override list
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);

            // Build a lookup from placeholder name to test clip
            var testClipMap = new Dictionary<string, AnimationClip>();
            for (int i = 0; i < AnimationClipFactory.ClipNames.Length; i++)
            {
                testClipMap[AnimationClipFactory.ClipNames[i]] = testClips[i];
            }

            // Map placeholder names to test clip names
            var nameMap = new Dictionary<string, string>
            {
                { "Placeholder_Idle",        "Test_Idle" },
                { "Placeholder_Walk",        "Test_Walk" },
                { "Placeholder_Run",         "Test_Run" },
                { "Placeholder_Jump",        "Test_Jump" },
                { "Placeholder_Fall",        "Test_Fall" },
                { "Placeholder_Land",        "Test_Land" },
                { "Placeholder_SwimIdle",    "Test_SwimIdle" },
                { "Placeholder_SwimSlow",    "Test_SwimSlow" },
                { "Placeholder_SwimFast",    "Test_SwimFast" },
                { "Placeholder_SwimAscend",  "Test_SwimAscend" },
                { "Placeholder_SwimDescend", "Test_SwimDescend" },
                { "Placeholder_FlyIdle",     "Test_FlyIdle" },
                { "Placeholder_FlySlow",     "Test_FlySlow" },
                { "Placeholder_FlyFast",     "Test_FlyFast" },
                { "Placeholder_FlyAscend",   "Test_FlyAscend" },
                { "Placeholder_FlyDescend",  "Test_FlyDescend" }
            };

            for (int i = 0; i < overrides.Count; i++)
            {
                var original = overrides[i].Key;
                if (original != null && nameMap.TryGetValue(original.name, out string testName))
                {
                    if (testClipMap.TryGetValue(testName, out var testClip))
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, testClip);
                    }
                }
            }
            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);

            // 4. Assign to Player and NPC model children
            AssignAnimatorToEntity("Player", overrideController);
            AssignAnimatorToEntity("NPC", overrideController);

            AssetDatabase.SaveAssets();
        }

        private static void AssignAnimatorToEntity(string entityName, AnimatorOverrideController overrideCtrl)
        {
            var entityGo = GameObject.Find(entityName);
            if (entityGo == null)
            {
                Debug.LogWarning($"[Entity Base] Could not find '{entityName}' in scene.");
                return;
            }

            // Find or create Model child
            var modelTransform = entityGo.transform.Find("Model");
            if (modelTransform == null)
            {
                // Create a Model child with capsule mesh (matching the entity setup)
                var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Model";
                model.transform.SetParent(entityGo.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                // Remove the collider from the model child (root has CharacterController)
                Object.DestroyImmediate(model.GetComponent<Collider>());
                modelTransform = model.transform;
            }

            // Add or get Animator on Model
            var animator = modelTransform.GetComponent<Animator>();
            if (animator == null)
                animator = modelTransform.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = overrideCtrl;
            animator.applyRootMotion = false;

            // Also set the override on EntityIdentity so EntityAnimator can find it
            var identity = entityGo.GetComponent<EntityIdentity>();
            if (identity != null)
            {
                identity.animatorOverride = overrideCtrl;
                EditorUtility.SetDirty(identity);
            }

            // Remove the capsule renderer from the root if it exists (only model child should render)
            var rootRenderer = entityGo.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                Object.DestroyImmediate(rootRenderer);
            var rootFilter = entityGo.GetComponent<MeshFilter>();
            if (rootFilter != null)
                Object.DestroyImmediate(rootFilter);

            EditorUtility.SetDirty(animator);
        }
    }
}
