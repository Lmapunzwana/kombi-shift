using UnityEngine;

namespace KombiShift2.Gameplay
{
    [CreateAssetMenu(menuName = "Kombi Shift/Stage Settings")]
    public sealed class KombiStageSettings : ScriptableObject
    {
        [Min(60f)] public float duration = 600f;
        [Min(1)] public int deliveryTarget = 1;
        [Min(1f)] public float rivalSpeed = 7.5f;
        [Min(0f)] public float rivalHeadStart = 22f;
        [Range(.1f,1f)] public float trafficFraction = .6f;
        [Min(1)] public int fare = 2;
        [Header("Selected city trips (zero uses the stage default)")]
        [Min(0)] public int cityDeliveryTarget;
        [Min(0)] public int cityTrafficVehicles;
        [Range(0,12)] public int cityQueueSize;
        [Min(0f)] public float cityCruisingSpeed;
        [Min(0f)] public float cityServiceAllowance = 40f;
        [Min(0f)] public float cityTripAllowance = 150f;
        public static KombiStageSettings Load(KombiStage stage) => Resources.Load<KombiStageSettings>(stage == KombiStage.Morning ? "MorningShift" : "RushHour");
    }
}
