using System;
using KombiShift2.City;
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
        private float speedingSeconds,nextCitySample;
        private float cityLimit=50f;
        private CityIntersection lastJunction;
        private bool stoppedForJunction,enteredJunction;
        private bool speedingIncident;
        public float SpeedLimit { get { float limit = Mathf.Min(defaultLimit,cityLimit); foreach (var value in limits.Values) limit = Mathf.Min(limit, value); return limit; } }
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
        private void SampleCity()
        {
            var graph=CityRoadNetwork.Active;if(graph==null)return;
            int index=graph.Nearest(transform.position,0);if(index<0)return;var node=graph.Nodes[index];cityLimit=node.speedLimit;
            var junction=CityIntersection.Near(transform.position);
            if(junction!=lastJunction){lastJunction=junction;stoppedForJunction=false;enteredJunction=false;}
            float speed=director.Vehicle.absoluteSpeed;
            if(junction!=null)
            {
                if(speed<1f)stoppedForJunction=true;
                float distance=Vector3.Distance(transform.position,junction.transform.position);
                if(distance<junction.radius*.7f&&!enteredJunction&&speed>1.5f)
                {
                    enteredJunction=true;
                    if(junction.MustStop(transform.forward))Record("city-red:"+junction.GetEntityId(),"Red traffic signal",5);
                    if(junction.RequiresStop(transform.forward)&&!stoppedForJunction)Record("city-stop:"+junction.GetEntityId(),"Failed to stop at the junction",3);
                }
            }
            else if(speed>8f && Vector3.Distance(node.position,transform.position)<10f)
            {
                float best=-1;Vector3 forward=transform.forward;
                foreach(int next in node.links){var f=(graph.Nodes[next].position-node.position).normalized;float dot=Vector3.Dot(f,transform.forward);if(dot>best){best=dot;forward=f;}}
                float right=Vector3.Dot(transform.position-node.position,Vector3.Cross(Vector3.up,forward));
                if(best>.7f&&right>1f)Record("city-wrong-way","Driving against the signed left-hand lanes",4);
            }
        }
        private void Update()
        {
            if (director != null)
            {
                SampleSpeed(director.Vehicle.absoluteSpeed, Time.deltaTime);
                if(Time.time>=nextCitySample && !director.Paused && !director.Finished){nextCitySample=Time.time+.2f;SampleCity();}
            }
        }
    }
}
