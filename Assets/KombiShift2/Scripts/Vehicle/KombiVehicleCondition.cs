using System;
using KombiShift2.Gameplay;
using UnityEngine;

namespace KombiShift2.Vehicle
{
    public sealed class KombiVehicleCondition : MonoBehaviour
    {
        [SerializeField] private float harmlessImpact = 2.5f, severeImpact = 12f, damageScale = 2f;
        [SerializeField] private float repairCostPerPoint = .15f;
        private float nextImpact;
        private KombiShiftGameDirector director;
        public float Health { get; private set; } = 100f;
        public float PowerFactor => Mathf.Lerp(.45f, 1f, Health / 100f);
        public float BrakeFactor => Mathf.Lerp(.75f, 1f, Health / 100f);
        public event Action<float> Damaged;
        public void Configure(KombiShiftGameDirector value) => director = value;
        public static float ImpactDamage(float speed, float impulsePerMass, float threshold = 2.5f, float scale = 2f)
        { return speed <= threshold || impulsePerMass < .5f ? 0f : Mathf.Clamp((speed - threshold) * scale, 0f, 35f); }
        public void ApplyDamage(float amount)
        {
            if (amount <= 0f) return;
            Health = Mathf.Max(10f, Health - amount); Damaged?.Invoke(amount);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (director == null || director.Paused || director.Finished || Time.time < nextImpact) return;
            var rb = GetComponent<Rigidbody>();
            float speed = collision.relativeVelocity.magnitude;
            float amount = ImpactDamage(speed, collision.impulse.magnitude / Mathf.Max(1f, rb.mass), harmlessImpact, damageScale);
            if (amount <= 0f) return;
            nextImpact = Time.time + 1f; ApplyDamage(amount);
            director.Quality.Apply("collision", speed >= severeImpact ? "Passenger: That was a serious crash!" : "Passenger: Please drive carefully!", -amount * .5f, -Mathf.CeilToInt(amount * .3f), 1f);
        }
        public int RepairPrice => Mathf.CeilToInt((100f - Health) * repairCostPerPoint);
        public bool Repair()
        {
            if (director == null || director.Finished || Health >= 100f || director.Vehicle.absoluteSpeed >= 2f || !director.AtServicePoint || !director.TrySpend(RepairPrice)) return false;
            Health = 100f; return true;
        }
    }
}
