using System.Collections.Generic;
using KombiShift2.Vehicle;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    public enum RoadRule { SpeedLimit, Stop, GiveWay, NoEntry, NoOvertaking, Crossing, Roadworks, Hump, Direction }
    [RequireComponent(typeof(BoxCollider))]
    public sealed class KombiRoadRuleZone : MonoBehaviour
    {
        public RoadRule rule;
        public float limit = 30f;
        public int fine = 3;
        public Vector3 travelDirection = Vector3.forward;
        private readonly Dictionary<KombiTrafficLaw, float> stopped = new();
        private readonly Dictionary<KombiTrafficLaw, int> contacts = new();
        private void OnEnable() => KombiRoadRules.Active.Add(this);
        private void Awake() => GetComponent<BoxCollider>().isTrigger = true;
        private void OnTriggerEnter(Collider other)
        {
            var law = other.GetComponentInParent<KombiTrafficLaw>(); if (law == null) return;
            if (contacts.TryGetValue(law, out int count)) { contacts[law] = count + 1; return; }
            contacts[law] = 1; stopped[law] = 0f;
            if (rule == RoadRule.SpeedLimit || rule == RoadRule.Crossing || rule == RoadRule.Roadworks) law.EnterLimit(this, limit);
            if (rule == RoadRule.NoEntry) law.Record("entry:" + GetEntityId(), "No entry", fine);
            if (rule == RoadRule.Hump)
            {
                float speed = law.GetComponent<RCCP_CarController>().absoluteSpeed;
                if (speed > limit)
                {
                    float severity = Mathf.Clamp((speed - limit) / 10f, 1f, 5f);
                    var director = FindFirstObjectByType<KombiShiftGameDirector>();
                    director.Quality.Apply("hump", "Passenger: Slow down for the hump!", -severity * 2f, -2);
                    law.GetComponent<KombiVehicleCondition>().ApplyDamage(speed > limit + 20f ? severity : 0f);
                }
            }
        }
        private void OnTriggerStay(Collider other)
        {
            var law = other.GetComponentInParent<KombiTrafficLaw>(); if (law == null) return;
            var vehicle = law.GetComponent<RCCP_CarController>();
            if (rule == RoadRule.Stop && vehicle.absoluteSpeed < 1f) stopped[law] = Time.time;
            if (rule == RoadRule.Direction && vehicle.absoluteSpeed > 8f && Vector3.Dot(vehicle.GetComponent<Rigidbody>().linearVelocity.normalized, travelDirection.normalized) < -.5f)
                law.Record("direction:" + GetEntityId(), "Against traffic direction", fine);
            if (rule == RoadRule.NoOvertaking && vehicle.absoluteSpeed > 12f && Vector3.Dot(law.transform.position - transform.position, Vector3.Cross(Vector3.up, travelDirection.normalized)) > 1f && Vector3.Dot(vehicle.GetComponent<Rigidbody>().linearVelocity.normalized, travelDirection.normalized) > .5f)
                law.Record("overtake:" + GetEntityId(), "Overtaking in a prohibited zone", fine);
        }
        private void OnTriggerExit(Collider other)
        {
            var law = other.GetComponentInParent<KombiTrafficLaw>(); if (law == null || !contacts.TryGetValue(law, out int count)) return;
            if (count > 1) { contacts[law] = count - 1; return; }
            if (rule == RoadRule.Stop && stopped.TryGetValue(law, out float at) && at <= 0f) law.Record("stop:" + GetEntityId(), "Failed to stop", fine);
            law.ExitLimit(this); contacts.Remove(law); stopped.Remove(law);
        }
        private void OnDisable() { KombiRoadRules.Active.Remove(this); foreach (var law in contacts.Keys) if (law != null) law.ExitLimit(this); contacts.Clear(); stopped.Clear(); }
    }
}
