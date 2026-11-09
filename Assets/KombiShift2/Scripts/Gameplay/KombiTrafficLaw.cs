using System;
using System.Collections.Generic;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    [Serializable] public sealed class TrafficOffence
    { public string incident, description; public int fine; public float time; }
    public sealed class KombiTrafficLaw : MonoBehaviour
    {
        [SerializeField] private float defaultLimit = 50f, speedGrace = 3f, tolerance = 5f;
        [SerializeField] private int speedingFine = 3;
        private readonly Dictionary<string, float> cooldowns = new();
        private readonly List<TrafficOffence> offences = new();
        private readonly Dictionary<KombiRoadRuleZone, float> limits = new();
        private KombiShiftGameDirector director;
        private float speedingSeconds;
        private bool speedingIncident;
        public float SpeedLimit { get { float limit = defaultLimit; foreach (var value in limits.Values) limit = Mathf.Min(limit, value); return limit; } }
        public int TotalFines { get; private set; }
        public IReadOnlyList<TrafficOffence> Offences => offences;
        public void Configure(KombiShiftGameDirector value) => director = value;
        public void EnterLimit(KombiRoadRuleZone zone, float limit) => limits[zone] = limit;
        public void ExitLimit(KombiRoadRuleZone zone) => limits.Remove(zone);
        public bool Record(string incident, string description, int fine, float cooldown = 30f, float severity = 1f)
        {
            if (director == null || director.Finished || director.Paused || (cooldowns.TryGetValue(incident, out float next) && Time.time < next)) return false;
            cooldowns[incident] = Time.time + cooldown;
            offences.Add(new TrafficOffence { incident = incident, description = description, fine = fine, time = Time.time });
            TotalFines += fine; director.DeductFine(fine);
            director.Quality.Apply("law:" + incident, description + " — $" + fine + " game fine", -5f * severity, -Mathf.CeilToInt(5f * severity), 0f);
            return true;
        }
        public void SampleSpeed(float kph, float seconds)
        {
            if (director == null || director.Finished || director.Paused) return;
            if (kph > SpeedLimit + tolerance)
            {
                speedingSeconds += Mathf.Max(0f, seconds);
                if (!speedingIncident && speedingSeconds >= speedGrace)
                {
                    float severity = Mathf.Clamp((kph-SpeedLimit)/10f,1f,3f);
                    Record("speeding", "Speed limit exceeded", Mathf.CeilToInt(speedingFine*severity), 0f, severity);
                    speedingIncident = true;
                }
            }
            else { speedingSeconds = 0f; if (kph < SpeedLimit) speedingIncident = false; }
        }
        private void Update()
        {
            if (director != null) SampleSpeed(director.Vehicle.absoluteSpeed, Time.deltaTime);
        }
    }
}
