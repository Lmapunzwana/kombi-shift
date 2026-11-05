using UnityEngine;

namespace KombiShift2.Vehicle
{
    [CreateAssetMenu(menuName = "Kombi Shift/Vehicle Definition", fileName = "VehicleDefinition")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Kombi";
        [Min(1)] public int passengerCapacity = 15;

        [Header("Body")]
        [Min(100f)] public float massKg = 2300f;
        public Vector3 centreOfMass = new(0f, -0.55f, 0.1f);

        [Header("Performance")]
        [Min(1f)] public float motorTorque = 3600f;
        [Min(1f)] public float brakeTorque = 8000f;
        [Min(1f)] public float handbrakeTorque = 12000f;
        [Range(5f, 50f)] public float maximumSteeringAngle = 32f;
        [Min(1f)] public float maximumForwardSpeedKph = 95f;
        [Min(1f)] public float maximumReverseSpeedKph = 22f;

        [Header("Suspension")]
        [Min(0.01f)] public float suspensionDistance = 0.22f;
        [Min(1f)] public float suspensionSpring = 35000f;
        [Min(1f)] public float suspensionDamper = 4500f;
        [Range(0f, 1f)] public float suspensionTargetPosition = 0.5f;

        [Header("Economy")]
        [Min(0f)] public float litresPer100Km = 12f;
    }
}
