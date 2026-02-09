using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace EntityBase
{
    /// <summary>
    /// Abstract serializable sensor configuration for EntityTemplate.
    /// Subclasses auto-appear in Odin's [SerializeReference] polymorphic dropdown.
    /// </summary>
    [Serializable]
    public abstract class SensorConfig
    {
        [FoldoutGroup("$ConfigLabel")]
        [PropertyRange(0f, 100f)] [SuffixLabel("units")]
        public float range = 15f;

        [FoldoutGroup("$ConfigLabel")]
        public LayerMask detectionLayers = ~0;

        [FoldoutGroup("$ConfigLabel")]
        [PropertyRange(0.05f, 1f)] [SuffixLabel("sec")]
        public float checkInterval = 0.2f;

        /// <summary>Display label for Odin foldout grouping.</summary>
        public abstract string ConfigLabel { get; }

        /// <summary>
        /// Adds the corresponding sensor component to the entity and configures it.
        /// </summary>
        public abstract SensorBase Apply(GameObject entity);

        /// <summary>Copies shared base fields to a sensor component.</summary>
        protected void ApplyBase(SensorBase sensor)
        {
            sensor.range = range;
            sensor.detectionLayers = detectionLayers;
            sensor.checkInterval = checkInterval;
        }
    }

    /// <summary>
    /// Configuration for SightSensor. Stealth-tuned defaults.
    /// </summary>
    [Serializable]
    public class SightSensorConfig : SensorConfig
    {
        [FoldoutGroup("$ConfigLabel")]
        [PropertyRange(0f, 360f)] [SuffixLabel("deg")]
        public float viewAngle = 110f;

        [FoldoutGroup("$ConfigLabel")]
        [SuffixLabel("units")]
        public float eyeHeight = 1.6f;

        [FoldoutGroup("$ConfigLabel")]
        public LayerMask obstacleLayers = ~0;

        public override string ConfigLabel => "Sight Sensor";

        public SightSensorConfig()
        {
            range = 15f;
        }

        public override SensorBase Apply(GameObject entity)
        {
            var sensor = entity.AddComponent<SightSensor>();
            ApplyBase(sensor);
            sensor.viewAngle = viewAngle;
            sensor.eyeHeight = eyeHeight;
            sensor.obstacleLayers = obstacleLayers;
            return sensor;
        }
    }

    /// <summary>
    /// Configuration for HearingSensor. Stealth-tuned defaults.
    /// </summary>
    [Serializable]
    public class HearingSensorConfig : SensorConfig
    {
        public override string ConfigLabel => "Hearing Sensor";

        public HearingSensorConfig()
        {
            range = 8f;
        }

        public override SensorBase Apply(GameObject entity)
        {
            var sensor = entity.AddComponent<HearingSensor>();
            ApplyBase(sensor);
            return sensor;
        }
    }
}
