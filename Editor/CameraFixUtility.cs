using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

namespace EntityBase.Editor
{
    /// <summary>
    /// Consolidated camera fix tool. Properly assigns CinemachineCamera targets
    /// using struct copy-back pattern and resets all orbital follow settings.
    /// </summary>
    public static class CameraFixUtility
    {
        [MenuItem("Tools/Entity Base/Fix Camera Settings")]
        public static void Execute()
        {
            // --- Find the third-person camera ---
            var tpCamObj = GameObject.Find("ThirdPersonCinemachineCamera");
            if (tpCamObj == null)
            {
                Debug.LogError("[Camera Fix] ThirdPersonCinemachineCamera not found in scene!");
                return;
            }

            var orbital = tpCamObj.GetComponent<CinemachineOrbitalFollow>();
            if (orbital == null)
            {
                Debug.LogError("[Camera Fix] CinemachineOrbitalFollow not found on ThirdPersonCinemachineCamera!");
                return;
            }

            // --- Fix Radius ---
            orbital.Radius = 8f;

            // --- Fix RadialAxis (scroll zoom) ---
            orbital.RadialAxis = new InputAxis
            {
                Value = 1.0f,
                Range = new Vector2(0.3f, 3.0f),
                Center = 1.0f,
                Wrap = false,
                Recentering = new InputAxis.RecenteringSettings { Enabled = false }
            };

            // --- Fix VerticalAxis ---
            orbital.VerticalAxis = new InputAxis
            {
                Value = 20f,
                Range = new Vector2(-20f, 60f),
                Center = 20f,
                Wrap = false,
                Recentering = new InputAxis.RecenteringSettings { Enabled = false }
            };

            // --- Fix HorizontalAxis ---
            orbital.HorizontalAxis = new InputAxis
            {
                Value = 0f,
                Range = new Vector2(-180f, 180f),
                Center = 0f,
                Wrap = true,
                Recentering = new InputAxis.RecenteringSettings { Enabled = false }
            };

            // --- Fix target offset ---
            orbital.TargetOffset = new Vector3(0f, 1.4f, 0f);

            // --- Fix tracking damping (struct copy-back) ---
            var tracker = orbital.TrackerSettings;
            tracker.PositionDamping = new Vector3(0.3f, 0.3f, 0.3f);
            tracker.RotationDamping = new Vector3(0f, 0.2f, 0f);
            orbital.TrackerSettings = tracker;

            // --- Fix Rotation Composer ---
            var rotComposer = tpCamObj.GetComponent<CinemachineRotationComposer>();
            if (rotComposer != null)
            {
                rotComposer.TargetOffset = new Vector3(0f, 1.4f, 0f);
                rotComposer.Damping = new Vector2(0f, 0f);
                rotComposer.CenterOnActivate = true;
            }

            // --- Fix CinemachineCamera lens + targets ---
            var cmCam = tpCamObj.GetComponent<CinemachineCamera>();
            if (cmCam != null)
            {
                cmCam.Lens = new LensSettings
                {
                    FieldOfView = 50f,
                    NearClipPlane = 0.1f,
                    FarClipPlane = 1000f,
                };

                // Find player and assign as target (struct copy-back pattern)
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    var target = cmCam.Target;
                    target.TrackingTarget = player.transform;
                    target.LookAtTarget = player.transform;
                    cmCam.Target = target;
                }
                else
                {
                    Debug.LogWarning("[Camera Fix] No GameObject tagged 'Player' found.");
                }

                EditorUtility.SetDirty(cmCam);
            }

            // --- Fix first-person camera target ---
            var fpCamObj = GameObject.Find("FirstPersonCinemachineCamera");
            if (fpCamObj != null)
            {
                var fpCam = fpCamObj.GetComponent<CinemachineCamera>();
                if (fpCam != null)
                {
                    var player = GameObject.FindWithTag("Player");
                    if (player != null)
                    {
                        var headTarget = player.transform.Find("HeadTarget");
                        if (headTarget != null)
                        {
                            var fpTarget = fpCam.Target;
                            fpTarget.TrackingTarget = headTarget;
                            fpCam.Target = fpTarget;
                            EditorUtility.SetDirty(fpCam);
                        }
                    }
                }
            }

            // Mark dirty
            EditorUtility.SetDirty(orbital);
            EditorUtility.SetDirty(tpCamObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Camera Fix] Camera settings updated:\n" +
                      $"  - Radius: {orbital.Radius}\n" +
                      $"  - RadialAxis range: {orbital.RadialAxis.Range} (scroll zoom)\n" +
                      $"  - VerticalAxis start: {orbital.VerticalAxis.Value} deg\n" +
                      "  - Scroll wheel zoom: 0.3x to 3.0x base radius\n" +
                      "  - First-person camera target fixed (if found)");
        }
    }
}
