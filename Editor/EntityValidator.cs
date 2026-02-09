using UnityEngine;
using UnityEditor;
using EntityBase;
using System.Collections.Generic;

namespace EntityBase.Editor
{
    /// <summary>
    /// Validates entity GameObjects for missing or misconfigured components.
    /// Accessible via Tools menu and as a context menu on EntityIdentity.
    /// </summary>
    public static class EntityValidator
    {
        [MenuItem("Tools/Entity Base/Validate All Entities")]
        public static void ValidateAllEntities()
        {
            var identities = Object.FindObjectsByType<EntityIdentity>(FindObjectsSortMode.None);
            if (identities.Length == 0)
            {
                Debug.Log("[Entity Validator] No entities found in scene.");
                return;
            }

            int totalIssues = 0;
            foreach (var identity in identities)
            {
                totalIssues += ValidateEntity(identity);
            }

            if (totalIssues == 0)
                Debug.Log($"[Entity Validator] All {identities.Length} entities are valid.");
            else
                Debug.LogWarning($"[Entity Validator] Found {totalIssues} issue(s) across {identities.Length} entities.");
        }

        [MenuItem("CONTEXT/EntityIdentity/Validate Entity")]
        private static void ValidateFromContextMenu(MenuCommand cmd)
        {
            var identity = cmd.context as EntityIdentity;
            if (identity != null)
            {
                int issues = ValidateEntity(identity);
                if (issues == 0)
                    Debug.Log($"[Entity Validator] '{identity.displayName}' is valid.");
            }
        }

        /// <summary>
        /// Validates a single entity. Returns the number of issues found.
        /// </summary>
        public static int ValidateEntity(EntityIdentity identity)
        {
            var issues = new List<string>();
            var go = identity.gameObject;
            string name = $"'{identity.displayName}' ({go.name})";

            // --- Required on ALL entities ---
            if (go.GetComponent<CharacterController>() == null)
                issues.Add("Missing CharacterController");

            if (go.GetComponent<EntityMotor>() == null)
                issues.Add("Missing EntityMotor");

            if (go.GetComponent<IMovementInput>() == null)
                issues.Add("Missing IMovementInput implementation (PlayerMovementInput or NPCMovementInput)");

            // --- Player-specific ---
            if (identity.IsPlayer)
            {
                var playerInput = go.GetComponent<PlayerMovementInput>();
                if (playerInput == null)
                {
                    issues.Add("Player missing PlayerMovementInput");
                }
                else if (playerInput.inputActions == null)
                {
                    issues.Add("PlayerMovementInput has no InputActionAsset assigned");
                }

                var perspective = go.GetComponent<CameraPerspectiveManager>();
                if (perspective == null)
                {
                    issues.Add("Player missing CameraPerspectiveManager");
                }
                else
                {
                    if (perspective.thirdPersonCamera == null)
                        issues.Add("CameraPerspectiveManager: thirdPersonCamera not assigned");
                    if (perspective.firstPersonCamera == null)
                        issues.Add("CameraPerspectiveManager: firstPersonCamera not assigned");
                }

                if (go.transform.Find("HeadTarget") == null)
                    issues.Add("Player missing HeadTarget child object");

                if (go.GetComponent<StatsHUD>() == null)
                    issues.Add("Player missing StatsHUD");

                if (go.GetComponent<NPCMovementInput>() != null)
                    issues.Add("Player should not have NPCMovementInput");
            }

            // --- NPC-specific ---
            if (identity.IsNPC)
            {
                if (go.GetComponent<NPCMovementInput>() == null)
                    issues.Add("NPC missing NPCMovementInput");

                if (go.GetComponent<Pathfinding.Seeker>() == null)
                    issues.Add("NPC missing Seeker (A* Pathfinding)");

                if (go.GetComponent<Pathfinding.AIPath>() == null)
                    issues.Add("NPC missing AIPath (A* Pathfinding)");

                if (go.GetComponent<CameraPerspectiveManager>() != null)
                    issues.Add("NPC should not have CameraPerspectiveManager");

                if (go.GetComponent<ControlsHUD>() != null)
                    issues.Add("NPC should not have ControlsHUD");

                if (go.GetComponent<StatsHUD>() != null)
                    issues.Add("NPC should not have StatsHUD");

                if (go.GetComponent<PlayerMovementInput>() != null)
                    issues.Add("NPC should not have PlayerMovementInput");
            }

            // --- Stats ---
            var attributes = go.GetComponent<EntityAttributes>();
            if (attributes != null && attributes.stats.Count == 0)
                issues.Add("EntityAttributes has no stats configured");

            // --- Sensors ---
            if (go.GetComponents<SensorBase>().Length == 0)
                issues.Add("No sensors found (expected at least SightSensor + HearingSensor)");

            // --- Animator check ---
            var entityAnimator = go.GetComponent<EntityAnimator>();
            if (entityAnimator != null)
            {
                var modelAnimator = identity.GetModelAnimator();
                if (modelAnimator == null)
                    issues.Add("EntityAnimator present but no Animator found on model child (add Animator to Model or assign an AnimatorController)");
            }

            // --- Optional components check ---
            var motor = go.GetComponent<EntityMotor>();
            if (motor != null)
            {
                if (motor.groundLayers == 0)
                    issues.Add("EntityMotor: groundLayers is empty (nothing will count as ground)");
            }

            // --- Report ---
            foreach (var issue in issues)
            {
                Debug.LogWarning($"[Entity Validator] {name}: {issue}", go);
            }

            return issues.Count;
        }
    }
}
