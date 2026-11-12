using UnityEngine;

namespace KombiShift2.Gameplay
{
    // Resolve once per session. Stage assets remain the authoring source; legacy city defaults
    // keep existing serialized scenes/assets compatible until an author overrides them.
    public sealed class KombiShiftConfiguration
    {
        public KombiStage Stage { get; }
        public float Duration { get; }
        public int DeliveryTarget { get; }
        public int CityDeliveryTarget { get; }
        public int CityTrafficVehicles { get; }
        public int CityQueueSize { get; }
        public float RivalSpeed { get; }
        public float RivalDelay { get; }
        public float TrafficFraction { get; }
        public float Fare { get; }
        public float CityCruisingSpeed { get; }
        public float ServiceAllowance { get; }
        public float TripAllowance { get; }

        public KombiShiftConfiguration(KombiStage stage)
        {
            Stage = stage;
            bool rush = stage == KombiStage.RushHour;
            var settings = KombiStageSettings.Load(stage);
            Duration = Mathf.Max(60f, settings != null ? settings.duration : rush ? 420f : 600f);
            DeliveryTarget = Mathf.Max(1, settings != null ? settings.deliveryTarget : rush ? 6 : 1);
            CityDeliveryTarget = settings != null && settings.cityDeliveryTarget > 0 ? settings.cityDeliveryTarget : rush ? 6 : 4;
            CityTrafficVehicles = settings != null && settings.cityTrafficVehicles > 0 ? settings.cityTrafficVehicles : rush ? 48 : 28;
            CityQueueSize = Mathf.Clamp(settings != null && settings.cityQueueSize > 0 ? settings.cityQueueSize : rush ? 8 : 6, 1, 12);
            RivalSpeed = Mathf.Max(1f, settings != null ? settings.rivalSpeed : rush ? 10.5f : 7.5f);
            RivalDelay = Mathf.Max(0f, settings != null ? settings.rivalHeadStart : rush ? 4f : 22f);
            TrafficFraction = Mathf.Clamp01(settings != null ? settings.trafficFraction : rush ? 1f : .6f);
            Fare = Mathf.Max(1f, settings != null ? settings.fare : rush ? 3f : 2f);
            CityCruisingSpeed = settings != null && settings.cityCruisingSpeed > 0 ? settings.cityCruisingSpeed : rush ? 7f : 6f;
            ServiceAllowance = settings != null ? Mathf.Max(0f, settings.cityServiceAllowance) : 40f;
            TripAllowance = settings != null ? Mathf.Max(0f, settings.cityTripAllowance) : 150f;
        }

        public float TripDuration(float metres, int stops) => Mathf.Max(Duration,
            Mathf.Max(0f, metres) / CityCruisingSpeed + Mathf.Max(0, stops) * ServiceAllowance + TripAllowance);
        public int TripTarget(int demand) => Mathf.Min(Mathf.Max(1, demand), CityDeliveryTarget);
        public int TripDemand(int stopCount) => Mathf.Max(0, stopCount - 1) * CityQueueSize;
    }
}
