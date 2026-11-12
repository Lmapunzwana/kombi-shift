using System;
using System.Collections.Generic;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    // One sampler for the manifest; reactions are broadcast as events.
    public sealed class KombiServiceQuality : MonoBehaviour
    {
        [SerializeField] private float eventCooldown = 5f;
        [SerializeField] private float harshBrake = 6f, harshAcceleration = 5f, harshTurn = 4.5f;
        private readonly Dictionary<string, float> nextEvent = new();
        private KombiShiftGameDirector director;
        private Rigidbody body;
        private Vector3 previousVelocity;
        private float nextSample, nextSmoothReward;
        public int XP { get; private set; }
        public string Reaction { get; private set; } = "Drive smoothly for happy passengers.";
        public event Action<string, int> Changed;
        public void Configure(KombiShiftGameDirector value)
        { director = value; body = value.Vehicle.GetComponent<Rigidbody>(); previousVelocity = body.linearVelocity; nextSample = Time.time + 1f; nextSmoothReward = Time.time + 30f; }
        public bool Apply(string key, string message, float happiness, int xp, float cooldown = -1f)
        {
            if (director == null || director.Finished || director.Paused) return false;
            if (nextEvent.TryGetValue(key, out float next) && Time.time < next) return false;
            nextEvent[key] = Time.time + (cooldown < 0f ? eventCooldown : cooldown);
            if (happiness < 0f) nextSmoothReward = Time.time + 30f;
            XP += xp; Reaction = message;
            foreach (var trip in director.Trips) if (trip.state != TripState.Completed) trip.Adjust(happiness);
            Changed?.Invoke(message, xp); return true;
        }
        private void FixedUpdate()
        {
            if (director == null || director.Paused || director.Finished || Time.time < nextSample) return;
            const float interval = .5f;
            Vector3 velocity = body.linearVelocity;
            Vector3 acceleration = (velocity - previousVelocity) / interval;
            previousVelocity = velocity; nextSample = Time.time + interval;
            float longitudinal = Vector3.Dot(acceleration, transform.forward);
            float lateral = Mathf.Abs(Vector3.Dot(acceleration, transform.right));
            if (longitudinal < -harshBrake) Apply("brake", "Passenger: Easy on the brakes!", -Mathf.Min(12f, -longitudinal), -3);
            else if (longitudinal > harshAcceleration) Apply("acceleration", "Passenger: Please accelerate gently.", -5f, -2);
            else if (lateral > harshTurn && velocity.magnitude > 5f) Apply("turn", "Passenger: Slow down for the turn!", -6f, -3);
            else if (Time.time >= nextSmoothReward && velocity.magnitude > 3f && director.Passengers > 0)
            { Apply("smooth", "Passengers appreciate the smooth driving.", 2f, 2, 30f); nextSmoothReward = Time.time + 30f; }
        }
    }
}
