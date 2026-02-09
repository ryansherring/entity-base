using UnityEngine;

namespace EntityBase
{
    /// <summary>
    /// Trigger zone that switches any entity's locomotion mode on enter/exit.
    /// Attach to a GameObject with a Collider set to isTrigger=true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LocomotionZone : MonoBehaviour
    {
        [Tooltip("The locomotion mode to activate when an entity enters this zone.")]
        public LocomotionMode zoneMode = LocomotionMode.Swimming;

        private void OnTriggerEnter(Collider other)
        {
            var motor = other.GetComponentInParent<EntityMotor>();
            if (motor == null) return;

            if (zoneMode == LocomotionMode.Swimming)
            {
                // Set water surface Y from the top of this zone's collider bounds
                motor.waterSurfaceY = GetComponent<Collider>().bounds.max.y;
            }

            motor.SetLocomotionMode(zoneMode);
        }

        private void OnTriggerExit(Collider other)
        {
            var motor = other.GetComponentInParent<EntityMotor>();
            if (motor == null) return;

            // Only revert if the entity is still in this zone's mode
            if (motor.currentMode == zoneMode)
            {
                motor.SetLocomotionMode(LocomotionMode.Ground);
            }
        }
    }
}
