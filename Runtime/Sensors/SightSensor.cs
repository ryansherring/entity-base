using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Vision cone sensor: range + angle + line-of-sight raycast.
    /// Draws its own yellow sight cone gizmo.
    /// </summary>
    public class SightSensor : SensorBase
    {
        [FoldoutGroup("Sensor")]
        [PropertyRange(0f, 360f)] [SuffixLabel("deg")]
        [Tooltip("Field of view angle (full arc, centered on forward).")]
        public float viewAngle = 110f;

        [FoldoutGroup("Sensor")]
        [SuffixLabel("units")]
        [Tooltip("Height offset for the eye position (raycast origin).")]
        public float eyeHeight = 1.6f;

        [FoldoutGroup("Sensor")]
        [Tooltip("Which layers block line of sight.")]
        public LayerMask obstacleLayers = ~0;

        public override string SensorName => "Sight";

        /// <summary>Eye position in world space.</summary>
        public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

        private void Reset()
        {
            range = 15f;
            viewAngle = 110f;
            eyeHeight = 1.6f;
            checkInterval = 0.2f;
        }

        public override bool CanDetect(Transform target)
        {
            if (target == null) return false;

            Vector3 eyePos = EyePosition;
            Vector3 dirToTarget = target.position - eyePos;
            float distance = dirToTarget.magnitude;

            if (distance > range) return false;

            float angle = Vector3.Angle(transform.forward, dirToTarget);
            if (angle > viewAngle * 0.5f) return false;

            if (Physics.Raycast(eyePos, dirToTarget.normalized, distance, obstacleLayers,
                QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

        public override int GetDetected(Collider[] results)
        {
            Vector3 eyePos = EyePosition;
            int count = Physics.OverlapSphereNonAlloc(eyePos, range, SharedBuffer,
                detectionLayers, QueryTriggerInteraction.Ignore);

            int resultCount = 0;
            for (int i = 0; i < count && resultCount < results.Length; i++)
            {
                if (IsSelf(SharedBuffer[i])) continue;

                Vector3 dirToTarget = SharedBuffer[i].transform.position - eyePos;
                float angle = Vector3.Angle(transform.forward, dirToTarget);
                if (angle > viewAngle * 0.5f) continue;

                float distance = dirToTarget.magnitude;
                if (Physics.Raycast(eyePos, dirToTarget.normalized, distance, obstacleLayers,
                    QueryTriggerInteraction.Ignore))
                    continue;

                results[resultCount++] = SharedBuffer[i];
            }

            detectedCount = resultCount;
            return resultCount;
        }

        // -------------------------------------------------------------------
        // GIZMO: Yellow sight cone
        // -------------------------------------------------------------------

#if UNITY_EDITOR
        protected override void DrawSensorGizmos(bool isSelected)
        {
            float alphaScale = isSelected ? 1f : 0.5f;
            Vector3 eyePos = EyePosition;
            float halfAngle = viewAngle * 0.5f;
            Vector3 forward = transform.forward;

            // Solid arc
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.15f * alphaScale);
            UnityEditor.Handles.DrawSolidArc(eyePos, Vector3.up,
                Quaternion.Euler(0, -halfAngle, 0) * forward, viewAngle, range);

            // Wire arc
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.8f * alphaScale);
            UnityEditor.Handles.DrawWireArc(eyePos, Vector3.up,
                Quaternion.Euler(0, -halfAngle, 0) * forward, viewAngle, range);

            // Edge lines
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            UnityEditor.Handles.DrawLine(eyePos, eyePos + leftDir * range);
            UnityEditor.Handles.DrawLine(eyePos, eyePos + rightDir * range);
        }
#endif
    }
}
