using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace EntityBase.Editor
{
    /// <summary>
    /// Generates a base AnimatorController with Ground/Swim/Fly sub-state machines,
    /// placeholder clips, and an AnimatorOverrideController for easy clip swapping.
    /// </summary>
    public static class AnimatorControllerGenerator
    {
        // Parameter names (must match EntityAnimator hashes)
        private const string ParamSpeed = "Speed";
        private const string ParamVerticalSpeed = "VerticalSpeed";
        private const string ParamIsGrounded = "IsGrounded";
        private const string ParamIsSprinting = "IsSprinting";
        private const string ParamLocomotionMode = "LocomotionMode";
        private const string ParamJump = "Jump";
        private const string ParamLand = "Land";

        [MenuItem("Tools/Entity Base/Generate Base Animator Controller")]
        public static void Generate()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Base Animator Controller",
                "EntityBaseAnimator",
                "controller",
                "Choose a location for the generated Animator Controller.");

            if (string.IsNullOrEmpty(path)) return;

            GenerateAtPath(path);
        }

        /// <summary>
        /// Generates the base Animator Controller at the given asset path (no save dialog).
        /// Returns the generated AnimatorOverrideController.
        /// </summary>
        public static AnimatorOverrideController GenerateAtPath(string path)
        {
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            // Delete existing assets to avoid sub-asset conflicts
            string overridePathToClean = path.Replace(".controller", "_Override.overrideController");
            if (AssetDatabase.LoadAssetAtPath<Object>(overridePathToClean) != null)
                AssetDatabase.DeleteAsset(overridePathToClean);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Add parameters
            controller.AddParameter(ParamSpeed, AnimatorControllerParameterType.Float);
            controller.AddParameter(ParamVerticalSpeed, AnimatorControllerParameterType.Float);
            controller.AddParameter(ParamIsGrounded, AnimatorControllerParameterType.Bool);
            controller.AddParameter(ParamIsSprinting, AnimatorControllerParameterType.Bool);
            controller.AddParameter(ParamLocomotionMode, AnimatorControllerParameterType.Int);
            controller.AddParameter(ParamJump, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(ParamLand, AnimatorControllerParameterType.Trigger);

            // Create placeholder clips as sub-assets
            var clipIdle = CreateClip("Placeholder_Idle", controller, path);
            var clipWalk = CreateClip("Placeholder_Walk", controller, path);
            var clipRun = CreateClip("Placeholder_Run", controller, path);
            var clipJump = CreateClip("Placeholder_Jump", controller, path);
            var clipFall = CreateClip("Placeholder_Fall", controller, path);
            var clipLand = CreateClip("Placeholder_Land", controller, path);
            var clipSwimIdle = CreateClip("Placeholder_SwimIdle", controller, path);
            var clipSwimSlow = CreateClip("Placeholder_SwimSlow", controller, path);
            var clipSwimFast = CreateClip("Placeholder_SwimFast", controller, path);
            var clipSwimAscend = CreateClip("Placeholder_SwimAscend", controller, path);
            var clipSwimDescend = CreateClip("Placeholder_SwimDescend", controller, path);
            var clipFlyIdle = CreateClip("Placeholder_FlyIdle", controller, path);
            var clipFlySlow = CreateClip("Placeholder_FlySlow", controller, path);
            var clipFlyFast = CreateClip("Placeholder_FlyFast", controller, path);
            var clipFlyAscend = CreateClip("Placeholder_FlyAscend", controller, path);
            var clipFlyDescend = CreateClip("Placeholder_FlyDescend", controller, path);

            // Get the base layer state machine
            var rootStateMachine = controller.layers[0].stateMachine;

            // --- Ground Sub-State Machine ---
            var groundSM = rootStateMachine.AddStateMachine("GroundLocomotion", new Vector3(300, 0, 0));

            var idleState = groundSM.AddState("Idle", new Vector3(250, 0, 0));
            idleState.motion = clipIdle;
            groundSM.defaultState = idleState;

            // WalkRun blend tree
            var walkRunState = groundSM.AddState("WalkRun", new Vector3(250, 80, 0));
            var walkRunTree = new BlendTree();
            walkRunTree.name = "WalkRunBlend";
            walkRunTree.blendParameter = ParamSpeed;
            walkRunTree.AddChild(clipIdle, 0f);
            walkRunTree.AddChild(clipWalk, 4f);
            walkRunTree.AddChild(clipRun, 7f);
            walkRunTree.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(walkRunTree, path);
            walkRunState.motion = walkRunTree;

            var jumpState = groundSM.AddState("Jump", new Vector3(500, 0, 0));
            jumpState.motion = clipJump;

            var fallState = groundSM.AddState("Fall", new Vector3(500, 80, 0));
            fallState.motion = clipFall;

            var landState = groundSM.AddState("Land", new Vector3(500, 160, 0));
            landState.motion = clipLand;

            // Ground transitions
            // Idle -> WalkRun when Speed > 0.1
            var t = idleState.AddTransition(walkRunState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);

            // WalkRun -> Idle when Speed < 0.1
            t = walkRunState.AddTransition(idleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);

            // Idle/WalkRun -> Jump on Jump trigger
            t = idleState.AddTransition(jumpState);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, ParamJump);

            t = walkRunState.AddTransition(jumpState);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, ParamJump);

            // Jump -> Fall when !IsGrounded and VerticalSpeed < -1
            t = jumpState.AddTransition(fallState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Less, -1f, ParamVerticalSpeed);

            // Any -> Fall when !IsGrounded (catch falling off edges)
            t = idleState.AddTransition(fallState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Less, -1f, ParamVerticalSpeed);

            // Fall -> Land on Land trigger
            t = fallState.AddTransition(landState);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, ParamLand);

            // Jump -> Land on Land trigger
            t = jumpState.AddTransition(landState);
            t.hasExitTime = false;
            t.duration = 0.1f;
            t.AddCondition(AnimatorConditionMode.If, 0, ParamLand);

            // Land -> Idle (exit time)
            t = landState.AddTransition(idleState);
            t.hasExitTime = true;
            t.exitTime = 0.9f;
            t.duration = 0.15f;

            // --- Swim Sub-State Machine ---
            var swimSM = rootStateMachine.AddStateMachine("SwimLocomotion", new Vector3(300, 200, 0));

            var swimIdleState = swimSM.AddState("SwimIdle", new Vector3(250, 0, 0));
            swimIdleState.motion = clipSwimIdle;
            swimSM.defaultState = swimIdleState;

            var swimMoveState = swimSM.AddState("SwimMove", new Vector3(250, 80, 0));
            var swimTree = new BlendTree();
            swimTree.name = "SwimBlend";
            swimTree.blendParameter = ParamSpeed;
            swimTree.AddChild(clipSwimIdle, 0f);
            swimTree.AddChild(clipSwimSlow, 3f);
            swimTree.AddChild(clipSwimFast, 5f);
            swimTree.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(swimTree, path);
            swimMoveState.motion = swimTree;

            var swimAscendState = swimSM.AddState("SwimAscend", new Vector3(500, 0, 0));
            swimAscendState.motion = clipSwimAscend;

            var swimDescendState = swimSM.AddState("SwimDescend", new Vector3(500, 80, 0));
            swimDescendState.motion = clipSwimDescend;

            // Swim transitions
            t = swimIdleState.AddTransition(swimMoveState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);

            t = swimMoveState.AddTransition(swimIdleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);

            t = swimIdleState.AddTransition(swimAscendState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, ParamVerticalSpeed);

            t = swimIdleState.AddTransition(swimDescendState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, -0.5f, ParamVerticalSpeed);

            t = swimAscendState.AddTransition(swimIdleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, 0.5f, ParamVerticalSpeed);

            t = swimDescendState.AddTransition(swimIdleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, -0.5f, ParamVerticalSpeed);

            // --- Fly Sub-State Machine ---
            var flySM = rootStateMachine.AddStateMachine("FlyLocomotion", new Vector3(300, 400, 0));

            var flyIdleState = flySM.AddState("FlyIdle", new Vector3(250, 0, 0));
            flyIdleState.motion = clipFlyIdle;
            flySM.defaultState = flyIdleState;

            var flyMoveState = flySM.AddState("FlyMove", new Vector3(250, 80, 0));
            var flyTree = new BlendTree();
            flyTree.name = "FlyBlend";
            flyTree.blendParameter = ParamSpeed;
            flyTree.AddChild(clipFlyIdle, 0f);
            flyTree.AddChild(clipFlySlow, 6f);
            flyTree.AddChild(clipFlyFast, 12f);
            flyTree.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(flyTree, path);
            flyMoveState.motion = flyTree;

            var flyAscendState = flySM.AddState("FlyAscend", new Vector3(500, 0, 0));
            flyAscendState.motion = clipFlyAscend;

            var flyDescendState = flySM.AddState("FlyDescend", new Vector3(500, 80, 0));
            flyDescendState.motion = clipFlyDescend;

            // Fly transitions
            t = flyIdleState.AddTransition(flyMoveState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);

            t = flyMoveState.AddTransition(flyIdleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);

            t = flyIdleState.AddTransition(flyAscendState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, ParamVerticalSpeed);

            t = flyIdleState.AddTransition(flyDescendState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, -0.5f, ParamVerticalSpeed);

            t = flyAscendState.AddTransition(flyIdleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Less, 0.5f, ParamVerticalSpeed);

            t = flyDescendState.AddTransition(flyIdleState);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.Greater, -0.5f, ParamVerticalSpeed);

            // --- Cross-mode transitions (via root state machine) ---
            // Default is GroundLocomotion
            rootStateMachine.defaultState = rootStateMachine.stateMachines[0].stateMachine == groundSM
                ? FindEntryState(rootStateMachine, groundSM)
                : null;

            // Ground -> Swim (LocomotionMode == 1)
            // Note: AnimatorTransition (sub-state machine) has no hasExitTime/duration —
            // it transitions immediately when the condition is met.
            var groundToSwim = rootStateMachine.AddStateMachineTransition(groundSM, swimSM);
            groundToSwim.AddCondition(AnimatorConditionMode.Equals, 1, ParamLocomotionMode);

            // Ground -> Fly (LocomotionMode == 2)
            var groundToFly = rootStateMachine.AddStateMachineTransition(groundSM, flySM);
            groundToFly.AddCondition(AnimatorConditionMode.Equals, 2, ParamLocomotionMode);

            // Swim -> Ground (LocomotionMode == 0)
            var swimToGround = rootStateMachine.AddStateMachineTransition(swimSM, groundSM);
            swimToGround.AddCondition(AnimatorConditionMode.Equals, 0, ParamLocomotionMode);

            // Swim -> Fly (LocomotionMode == 2)
            var swimToFly = rootStateMachine.AddStateMachineTransition(swimSM, flySM);
            swimToFly.AddCondition(AnimatorConditionMode.Equals, 2, ParamLocomotionMode);

            // Fly -> Ground (LocomotionMode == 0)
            var flyToGround = rootStateMachine.AddStateMachineTransition(flySM, groundSM);
            flyToGround.AddCondition(AnimatorConditionMode.Equals, 0, ParamLocomotionMode);

            // Fly -> Swim (LocomotionMode == 1)
            var flyToSwim = rootStateMachine.AddStateMachineTransition(flySM, swimSM);
            flyToSwim.AddCondition(AnimatorConditionMode.Equals, 1, ParamLocomotionMode);

            // Save and refresh
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // --- Generate Override Controller ---
            string overridePath = path.Replace(".controller", "_Override.overrideController");
            var overrideController = new AnimatorOverrideController(controller);
            AssetDatabase.CreateAsset(overrideController, overridePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the controller in project
            Selection.activeObject = controller;

            Debug.Log($"[Entity Base] Generated Animator Controller at: {path}\n" +
                      $"  - 7 parameters (Speed, VerticalSpeed, IsGrounded, IsSprinting, LocomotionMode, Jump, Land)\n" +
                      $"  - 3 sub-state machines (Ground, Swim, Fly)\n" +
                      $"  - 16 placeholder clips (assign real clips via the Override Controller)\n" +
                      $"  - Override Controller at: {overridePath}");

            return overrideController;
        }

        private static AnimationClip CreateClip(string name, AnimatorController controller, string path)
        {
            var clip = new AnimationClip();
            clip.name = name;
            // Add a single keyframe so it's a valid 1-frame clip
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0, 1f / 60f, 0));
            clip.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(clip, path);
            return clip;
        }

        private static AnimatorState FindEntryState(AnimatorStateMachine rootSM, AnimatorStateMachine subSM)
        {
            foreach (var child in rootSM.stateMachines)
            {
                if (child.stateMachine == subSM)
                    return null; // Sub-state machines don't have a single "state" in the root
            }
            return null;
        }
    }
}
