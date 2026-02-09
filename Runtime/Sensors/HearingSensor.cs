using UnityEngine;

namespace EntityBase
{
    /// <summary>
    /// Omnidirectional hearing sensor: range-only distance check.
    /// Draws its own cyan hearing disc gizmo.
    /// </summary>
    public class HearingSensor : SensorBase
    {
        public override string SensorName => "Hearing";

        private void Reset()
        {
            range = 8f;
            checkInterval = 0.2f;
        }

        public override bool CanDetect(Transform target)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= range;
        }

        public override int GetDetected(Collider[] results)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, range,
                SharedBuffer, detectionLayers, QueryTriggerInteraction.Ignore);

            int resultCount = 0;
            for (int i = 0; i < count && resultCount < results.Length; i++)
            {
                if (IsSelf(SharedBuffer[i])) continue;
                results[resultCount++] = SharedBuffer[i];
            }

            detectedCount = resultCount;
            return resultCount;
        }

        // -------------------------------------------------------------------
        // GIZMO: Cyan hearing disc
        // -------------------------------------------------------------------

#if UNITY_EDITOR
        protected override void DrawSensorGizmos(bool isSelected)
        {
            float alphaScale = isSelected ? 1f : 0.5f;

            // Solid disc
            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.08f * alphaScale);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, range);

            // Wire disc
            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.4f * alphaScale);
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, range);
        }
#endif
    }
}
