using UnityEngine;
using UnityEditor;
using EntityBase;

namespace EntityBase.Editor
{
    /// <summary>
    /// SceneView gizmos for visualizing EntityMotor ground checks, NPC destinations,
    /// and entity identity markers.
    /// </summary>
    public static class EntityGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawMotorGizmos(EntityMotor motor, GizmoType type)
        {
            if (motor == null) return;

            // Ground check sphere
            Vector3 spherePos = motor.transform.position + Vector3.up * motor.groundedOffset;
            Gizmos.color = motor.isGrounded ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawSphere(spherePos, motor.groundedRadius);
            Gizmos.color = motor.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(spherePos, motor.groundedRadius);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawNPCGizmos(NPCMovementInput npcInput, GizmoType type)
        {
            if (npcInput == null) return;

            var aiPath = npcInput.GetComponent<Pathfinding.AIPath>();
            if (aiPath == null || !aiPath.hasPath) return;

            // Destination marker
            Vector3 dest = aiPath.destination;
            Gizmos.color = npcInput.HasReachedDestination
                ? new Color(0f, 1f, 0f, 0.8f)
                : new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawWireSphere(dest, 0.3f);
            Gizmos.DrawLine(npcInput.transform.position + Vector3.up * 0.5f, dest + Vector3.up * 0.1f);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawIdentityGizmos(EntityIdentity identity, GizmoType type)
        {
            if (identity == null) return;

            // Draw label above entity
            Vector3 labelPos = identity.transform.position + Vector3.up * 2.5f;
            string label = identity.IsPlayer ? "[P]" : "[NPC]";
            label += $" {identity.displayName}";

            GUIStyle style = new GUIStyle();
            style.normal.textColor = identity.IsPlayer ? Color.cyan : new Color(1f, 0.6f, 0.2f);
            style.fontSize = 11;
            style.alignment = TextAnchor.MiddleCenter;
            Handles.Label(labelPos, label, style);
        }

    }
}
