using UnityEngine;
using UnityEditor;

namespace EntityBase.Editor
{
    /// <summary>
    /// Generates visible test animation clips that animate localPosition, localRotation,
    /// and localScale on the Model child so you can see state changes on capsule primitives.
    /// </summary>
    public static class AnimationClipFactory
    {
        private const string DefaultPath = "Assets/Animation/TestAnimationClips.asset";

        // Clip names matching the placeholder naming convention
        public static readonly string[] ClipNames =
        {
            "Test_Idle", "Test_Walk", "Test_Run",
            "Test_Jump", "Test_Fall", "Test_Land",
            "Test_SwimIdle", "Test_SwimSlow", "Test_SwimFast",
            "Test_SwimAscend", "Test_SwimDescend",
            "Test_FlyIdle", "Test_FlySlow", "Test_FlyFast",
            "Test_FlyAscend", "Test_FlyDescend"
        };

        [MenuItem("Tools/Entity Base/Generate Test Animation Clips")]
        public static void GenerateMenu()
        {
            var clips = GenerateAtPath(DefaultPath);
            Selection.activeObject = clips[0];
            Debug.Log($"[Entity Base] Generated {clips.Length} test animation clips at: {DefaultPath}");
        }

        /// <summary>
        /// Generates all test clips as sub-assets of a container ScriptableObject at the given path.
        /// Returns an array of clips in the same order as ClipNames.
        /// </summary>
        public static AnimationClip[] GenerateAtPath(string path)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string[] parts = dir.Replace("\\", "/").Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            // Delete existing asset to rebuild cleanly
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            // Create a container ScriptableObject to hold the clips as sub-assets
            var container = ScriptableObject.CreateInstance<AnimationClipContainer>();
            container.name = "TestAnimationClips";
            AssetDatabase.CreateAsset(container, path);

            var clips = new AnimationClip[ClipNames.Length];

            clips[0]  = MakeIdle();
            clips[1]  = MakeWalk();
            clips[2]  = MakeRun();
            clips[3]  = MakeJump();
            clips[4]  = MakeFall();
            clips[5]  = MakeLand();
            clips[6]  = MakeSwimIdle();
            clips[7]  = MakeSwimSlow();
            clips[8]  = MakeSwimFast();
            clips[9]  = MakeSwimAscend();
            clips[10] = MakeSwimDescend();
            clips[11] = MakeFlyIdle();
            clips[12] = MakeFlySlow();
            clips[13] = MakeFlyFast();
            clips[14] = MakeFlyAscend();
            clips[15] = MakeFlyDescend();

            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].name = ClipNames[i];
                clips[i].hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(clips[i], path);
            }

            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return clips;
        }

        // ---------------------------------------------------------------
        // Ground Clips
        // ---------------------------------------------------------------

        private static AnimationClip MakeIdle()
        {
            // Subtle Y bob: 0 → 0.02 → 0 over 2s, looping
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(2f, 0f, 0.02f, 0f));
            return clip;
        }

        private static AnimationClip MakeWalk()
        {
            // Faster Y bob + slight Z tilt over 0.6s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(0.6f, 0f, 0.05f, 0f));
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z",
                MakeCurve(0.6f, 0f, 3f, 0f));
            return clip;
        }

        private static AnimationClip MakeRun()
        {
            // Fast Y bob + bigger tilt over 0.4s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(0.4f, 0f, 0.08f, 0f));
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z",
                MakeCurve(0.4f, 0f, 5f, 0f));
            return clip;
        }

        private static AnimationClip MakeJump()
        {
            // Scale Y stretch: 1 → 1.2 over 0.3s, once
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localScale.y",
                AnimationCurve.Linear(0f, 1f, 0.3f, 1.2f));
            clip.SetCurve("", typeof(Transform), "localScale.x",
                AnimationCurve.Linear(0f, 1f, 0.3f, 0.9f));
            clip.SetCurve("", typeof(Transform), "localScale.z",
                AnimationCurve.Linear(0f, 1f, 0.3f, 0.9f));
            return clip;
        }

        private static AnimationClip MakeFall()
        {
            // Scale Y squash: 1 → 0.85, looping over 0.5s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localScale.y",
                MakeCurve(0.5f, 1f, 0.85f, 1f));
            clip.SetCurve("", typeof(Transform), "localScale.x",
                MakeCurve(0.5f, 1f, 1.1f, 1f));
            clip.SetCurve("", typeof(Transform), "localScale.z",
                MakeCurve(0.5f, 1f, 1.1f, 1f));
            return clip;
        }

        private static AnimationClip MakeLand()
        {
            // Scale Y squash then restore: 1 → 0.8 → 1 over 0.3s, once
            var clip = new AnimationClip();
            var keys = new Keyframe[] { new(0f, 1f), new(0.12f, 0.8f), new(0.3f, 1f) };
            clip.SetCurve("", typeof(Transform), "localScale.y", new AnimationCurve(keys));
            var keysXZ = new Keyframe[] { new(0f, 1f), new(0.12f, 1.15f), new(0.3f, 1f) };
            clip.SetCurve("", typeof(Transform), "localScale.x", new AnimationCurve(keysXZ));
            clip.SetCurve("", typeof(Transform), "localScale.z", new AnimationCurve(keysXZ));
            return clip;
        }

        // ---------------------------------------------------------------
        // Swim Clips
        // ---------------------------------------------------------------

        private static AnimationClip MakeSwimIdle()
        {
            // Roll Z oscillate: 0 → 5 → -5 → 0 over 2s
            var clip = new AnimationClip();
            SetLoop(clip);
            var keys = new Keyframe[]
            {
                new(0f, 0f), new(0.5f, 5f), new(1f, 0f), new(1.5f, -5f), new(2f, 0f)
            };
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z", new AnimationCurve(keys));
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(2f, 0f, 0.03f, 0f));
            return clip;
        }

        private static AnimationClip MakeSwimSlow()
        {
            // Roll Z + Y bob over 1s
            var clip = new AnimationClip();
            SetLoop(clip);
            var keys = new Keyframe[]
            {
                new(0f, 0f), new(0.25f, 4f), new(0.5f, 0f), new(0.75f, -4f), new(1f, 0f)
            };
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z", new AnimationCurve(keys));
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(1f, 0f, 0.04f, 0f));
            return clip;
        }

        private static AnimationClip MakeSwimFast()
        {
            // Roll Z faster + Y bob faster over 0.5s
            var clip = new AnimationClip();
            SetLoop(clip);
            var keys = new Keyframe[]
            {
                new(0f, 0f), new(0.125f, 6f), new(0.25f, 0f), new(0.375f, -6f), new(0.5f, 0f)
            };
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z", new AnimationCurve(keys));
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(0.5f, 0f, 0.05f, 0f));
            return clip;
        }

        private static AnimationClip MakeSwimAscend()
        {
            // Tilt X forward (-15°) over 0.4s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x",
                MakeCurve(0.4f, 0f, -15f, 0f));
            return clip;
        }

        private static AnimationClip MakeSwimDescend()
        {
            // Tilt X backward (15°) over 0.4s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x",
                MakeCurve(0.4f, 0f, 15f, 0f));
            return clip;
        }

        // ---------------------------------------------------------------
        // Fly Clips
        // ---------------------------------------------------------------

        private static AnimationClip MakeFlyIdle()
        {
            // Hover Y oscillate: 0 → 0.1 → 0 over 1.5s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(1.5f, 0f, 0.1f, 0f));
            return clip;
        }

        private static AnimationClip MakeFlySlow()
        {
            // Tilt forward (-5°) + Y bob over 0.8s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x",
                MakeCurve(0.8f, 0f, -5f, 0f));
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(0.8f, 0f, 0.06f, 0f));
            return clip;
        }

        private static AnimationClip MakeFlyFast()
        {
            // Tilt forward (-15°) + Y bob over 0.5s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x",
                MakeCurve(0.5f, 0f, -15f, 0f));
            clip.SetCurve("", typeof(Transform), "localPosition.y",
                MakeCurve(0.5f, 0f, 0.08f, 0f));
            return clip;
        }

        private static AnimationClip MakeFlyAscend()
        {
            // Tilt back (10°) over 0.4s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x",
                MakeCurve(0.4f, 0f, 10f, 0f));
            return clip;
        }

        private static AnimationClip MakeFlyDescend()
        {
            // Tilt forward (-20°) over 0.4s
            var clip = new AnimationClip();
            SetLoop(clip);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x",
                MakeCurve(0.4f, 0f, -20f, 0f));
            return clip;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static void SetLoop(AnimationClip clip)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        /// <summary>
        /// Creates a 3-keyframe curve: start → mid → end over the given duration.
        /// </summary>
        private static AnimationCurve MakeCurve(float duration, float startVal, float midVal, float endVal)
        {
            return new AnimationCurve(
                new Keyframe(0f, startVal),
                new Keyframe(duration * 0.5f, midVal),
                new Keyframe(duration, endVal)
            );
        }
    }

    /// <summary>
    /// Simple container ScriptableObject to host animation clips as sub-assets.
    /// </summary>
    public class AnimationClipContainer : ScriptableObject { }
}
