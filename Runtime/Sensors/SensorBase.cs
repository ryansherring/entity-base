using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Abstract base for all sensor types. Each subclass is a self-contained MonoBehaviour
    /// that provides detection APIs and draws its own gizmos.
    /// </summary>
    public abstract class SensorBase : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // SHARED FIELDS
        // -------------------------------------------------------------------

        [FoldoutGroup("Sensor")]
        [PropertyRange(0f, 100f)] [SuffixLabel("units")]
        [Tooltip("Maximum detection range.")]
        public float range = 15f;

        [FoldoutGroup("Sensor")]
        [Tooltip("Which layers can be detected.")]
        public LayerMask detectionLayers = ~0;

        [FoldoutGroup("Sensor")]
        [PropertyRange(0.05f, 1f)] [SuffixLabel("sec")]
        [Tooltip("How often to run detection checks (performance tuning).")]
        public float checkInterval = 0.2f;

        // -------------------------------------------------------------------
        // GIZMOS
        // -------------------------------------------------------------------

        [FoldoutGroup("Gizmos")]
        [Tooltip("Always draw sensor gizmos in the scene view, even when not selected.")]
        public bool alwaysShowGizmos = true;

        [FoldoutGroup("Gizmos")]
        [Tooltip("Draw sensor gizmos during play mode.")]
        public bool showGizmosInPlayMode = false;

        // -------------------------------------------------------------------
        // DEBUG
        // -------------------------------------------------------------------

        [FoldoutGroup("Debug"), ReadOnly]
        public int detectedCount;

        // -------------------------------------------------------------------
        // SHARED BUFFER
        // -------------------------------------------------------------------

        protected static readonly Collider[] SharedBuffer = new Collider[32];

        // -------------------------------------------------------------------
        // ABSTRACT API
        // -------------------------------------------------------------------

        /// <summary>Display name for this sensor type (used in editor labels).</summary>
        public abstract string SensorName { get; }

        /// <summary>
        /// Checks if a specific target is detectable by this sensor.
        /// </summary>
        public abstract bool CanDetect(Transform target);

        /// <summary>
        /// Fills results with all detected colliders. Returns the count written.
        /// Results are only valid until the next call (shared static buffer).
        /// </summary>
        public abstract int GetDetected(Collider[] results);

        // -------------------------------------------------------------------
        // SELF-FILTER
        // -------------------------------------------------------------------

        /// <summary>
        /// Returns true if the collider belongs to this entity (self or child).
        /// </summary>
        protected bool IsSelf(Collider col)
        {
            return col.transform.IsChildOf(transform) || col.gameObject == gameObject;
        }

        // -------------------------------------------------------------------
        // GIZMO FRAMEWORK
        // -------------------------------------------------------------------

#if UNITY_EDITOR
        /// <summary>
        /// Whether gizmos should be drawn right now based on settings and play state.
        /// </summary>
        protected bool ShouldDrawGizmos(bool isSelected)
        {
            if (Application.isPlaying && !showGizmosInPlayMode) return false;
            if (!isSelected && !alwaysShowGizmos) return false;
            return true;
        }

        /// <summary>Override to draw sensor-specific gizmos.</summary>
        protected abstract void DrawSensorGizmos(bool isSelected);

        private void OnDrawGizmos()
        {
            if (!ShouldDrawGizmos(false)) return;
            DrawSensorGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!ShouldDrawGizmos(true)) return;
            DrawSensorGizmos(true);
        }
#endif
    }
}
