using System;
using System.Collections;
using System.Collections.Generic;
using KombiShift2.Gameplay;
using KombiShift2.Vehicle;
using UnityEngine;

namespace KombiShift2.Traffic
{
    public enum RivalState { Cruise, ChasePassenger, PullIn, Boarding, Depart }
    [RequireComponent(typeof(Rigidbody), typeof(KombiPassengerCabin))]
    public sealed class RivalKombiAI : MonoBehaviour
    {
        [SerializeField] private Transform[] route;
        [SerializeField] private PassengerStation[] stations;
        [SerializeField] private float speed = 7.5f, departureDelay = 18f;
        [SerializeField] private RivalDrivingProfile profile = new();
        [SerializeField] private float laneWidth = 3.1f;
        private PassengerStation target;
        private KombiPassengerCabin cabin;
        private Rigidbody body;
        private WheelCollider[] wheels;
        private TrafficSignalController[] signals;
        private int routeIndex, destinationIndex, direction = 1;
        private float startedAt, nextEvaluation, nextDeparture, blockedTime, laneOffset, passUntil;
        private bool shiftEnded, passing;
        private Rigidbody passTarget;
        private readonly Dictionary<KombiRoadRuleZone, float> stoppedAt = new();
        private readonly RaycastHit[] obstacles = new RaycastHit[64];
        private readonly Collider[] congestion = new Collider[32];
        public int PassengersTaken { get; private set; }
        public RivalState State { get; private set; }
        public PassengerStation Target => target;
        public float Speed => speed;
        public float DepartureDelay => departureDelay;
        public RivalDrivingProfile Profile => profile;
        public Color MarkerColor { get; private set; } = Color.cyan;
        public string Activity { get; private set; } = "Waiting for shift";
        public event Action<RivalKombiAI, string> ActivityChanged;
        public void Configure(Transform[] points, PassengerStation[] waitingStops, float delay, float cruisingSpeed)
        { route = points; stations = waitingStops; departureDelay = delay; speed = cruisingSpeed; }
        public void SetChallenge(float metresPerSecond, float delay) { speed = metresPerSecond; departureDelay = delay; }
        public void SetPersonality(RivalPersonality value, Color color)
        {
            profile = RivalDrivingProfile.For(value); MarkerColor = color;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                // Tint only pale body paint, preserving windows, wheels and shared assets.
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty("_Color")) continue;
                    string n = material.name.ToLowerInvariant();
                    if (n.Contains("glass") || n.Contains("wheel") || n.Contains("tire") || n.Contains("light") || n.Contains("chrome")) continue;
                    if (material.color.grayscale < .4f) continue;
                    renderer.GetPropertyBlock(block, i); block.SetColor("_Color", color); renderer.SetPropertyBlock(block, i);
                }
            }
        }
        public void StopForShiftEnd() { shiftEnded = true; if (target != null) target.Release(transform); Brake(); }
        private void Say(string text) { Activity = text; ActivityChanged?.Invoke(this, text); }
        private void Awake()
        {
            cabin = GetComponent<KombiPassengerCabin>(); body = GetComponent<Rigidbody>();
            body.isKinematic = false; body.useGravity = true; body.mass = 2200f;
            body.centerOfMass = new Vector3(0f, .45f, 0f); body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            wheels = new WheelCollider[4];
            for (int i = 0; i < 4; i++)
            {
                var mount = new GameObject("Rival physics wheel " + i); mount.transform.SetParent(transform, false);
                mount.transform.localPosition = new Vector3(i % 2 == 0 ? -.78f : .78f, .4f, i < 2 ? 1.35f : -1.35f);
                var wheel = mount.AddComponent<WheelCollider>(); wheel.radius = .34f; wheel.mass = 25f; wheel.suspensionDistance = .24f; wheel.ConfigureVehicleSubsteps(5f, 8, 12);
                var spring = wheel.suspensionSpring; spring.spring = 100000f; spring.damper = 8000f; spring.targetPosition = .5f; wheel.suspensionSpring = spring;
                wheels[i] = wheel;
            }
        }
        private void Start()
        {
            routeIndex = NearestRouteIndex(transform.position); startedAt = Time.time;
            signals = FindObjectsByType<TrafficSignalController>(FindObjectsSortMode.None);
        }
        private void FixedUpdate()
        {
            if (shiftEnded || route == null || route.Length < 2 || stations == null || Time.time - startedAt < departureDelay || State == RivalState.Boarding)
            { Brake(); return; }
            bool full = PassengersTaken >= cabin.Seats.Length;
            if (full)
            {
                target = Array.Find(stations, x => x != null && x.Terminal);
                if (target == null) { CruiseRoute(); return; }
                destinationIndex = NearestRouteIndex(target.ServicePoint.position);
                State = RivalState.Depart;
            }
            else if (Time.time >= nextDeparture && (target == null || !target.HasWaiting || target.Reserved || Time.time >= nextEvaluation)) ChooseTarget();
            if (target == null) { State = RivalState.Cruise; CruiseRoute(); return; }
            float distance = FlatDistance(target.ServicePoint.position, transform.position);
            if (distance < 3f)
            {
                Brake();
                if (body.linearVelocity.magnitude > .5f) return;
                if (full) { Say("Arrived at Mbare"); shiftEnded = true; return; }
                State = RivalState.Boarding; StartCoroutine(Collect()); return;
            }
            if (Mathf.Abs(routeIndex - destinationIndex) <= 1 && distance < 24f)
            { if (!full) State = RivalState.PullIn; Drive(target.ServicePoint.position, Mathf.Min(2.4f, distance)); }
            else
            {
                if (!full) State = RivalState.ChasePassenger;
                FollowRoute();
            }
        }
        private void CruiseRoute() => FollowRoute();
        private void FollowRoute()
        {
            if (FlatDistance(route[routeIndex].position, transform.position) < 5f)
            {
                if (routeIndex == route.Length - 1) direction = -1;
                if (routeIndex == 0) direction = 1;
                routeIndex = Mathf.Clamp(routeIndex + direction, 0, route.Length - 1);
            }
            Drive(LanePoint(routeIndex), speed * profile.speedMultiplier);
        }
        public static Vector3 LeftOffset(Vector3 forward, float width) => -Vector3.Cross(Vector3.up, forward.normalized) * width;
        private Vector3 LanePoint(int index)
        {
            int previous = Mathf.Clamp(index - direction, 0, route.Length - 1);
            int next = Mathf.Clamp(index + direction, 0, route.Length - 1);
            Vector3 forward = route[next].position - route[previous].position; forward.y = 0f;
            return route[index].position + LeftOffset(forward, laneWidth * .5f - laneOffset);
        }
        private float RouteDistance(int index)
        {
            if ((index - routeIndex) * direction < 0) return float.PositiveInfinity;
            float distance = FlatDistance(transform.position, route[routeIndex].position);
            for (int i = routeIndex; i != index; i += direction) distance += FlatDistance(route[i].position, route[i + direction].position);
            return distance;
        }
        private void ChooseTarget()
        {
            nextEvaluation = Time.time + profile.reassessSeconds;
            PassengerStation best = null; float bestValue = float.NegativeInfinity, currentValue = float.NegativeInfinity; int bestIndex = routeIndex;
            foreach (var station in stations)
            {
                if (station == null || !station.HasWaiting || station.Reserved) continue;
                int index = NearestRouteIndex(station.ServicePoint.position);
                int count = Physics.OverlapSphereNonAlloc(station.ServicePoint.position, 10f, congestion, ~0, QueryTriggerInteraction.Ignore);
                int vehicles = 0;
                for (int i = 0; i < count; i++) if (congestion[i].attachedRigidbody != null && congestion[i].attachedRigidbody != body) vehicles++;
                float utility = profile.Utility(station.WaitingCount, cabin.Seats.Length - PassengersTaken, RouteDistance(index), speed, vehicles, station.Reserved);
                if (station == target) currentValue = utility;
                if (utility > bestValue) { best = station; bestValue = utility; bestIndex = index; }
            }
            if (target != null && target.HasWaiting && !target.Reserved && best != target && bestValue < currentValue + profile.switchAdvantage) return;
            if (best != target && best != null) Say("Targeting " + best.name + " (demand " + best.WaitingCount + ")");
            target = best; destinationIndex = bestIndex;
        }
        private IEnumerator Collect()
        {
            var stop = target;
            yield return stop.Board(transform, cabin.Doorway, cabin.Seats, PassengersTaken,
                person => { PassengersTaken++; Say("Picked up passenger — total " + PassengersTaken); },
                () => !shiftEnded && isActiveAndEnabled && stop != null && body.linearVelocity.magnitude < .6f && FlatDistance(transform.position, stop.ServicePoint.position) < 4f);
            stop.Release(transform); target = null; State = RivalState.Depart; nextDeparture = Time.time + 3f;
            Say("Departing " + stop.name); routeIndex = NearestRouteIndex(transform.position);
        }
        private bool Clear(Vector3 origin, Vector3 forward, float distance, out RaycastHit nearest, Rigidbody ignoreBody = null)
        {
            int count = Physics.SphereCastNonAlloc(origin, .8f, forward, obstacles, distance, ~0, QueryTriggerInteraction.Ignore);
            var hits = count == obstacles.Length ? Physics.SphereCastAll(origin, .8f, forward, distance, ~0, QueryTriggerInteraction.Ignore) : obstacles;
            if (hits != obstacles) count = hits.Length;
            nearest = default; float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; if (hit.transform.IsChildOf(transform) || (ignoreBody != null && hit.rigidbody == ignoreBody) || hit.normal.y > .6f) continue;
                if (hit.distance < best) { best = hit.distance; nearest = hit; }
            }
            return best == float.MaxValue;
        }
        private bool CanOvertake(Vector3 origin, float actualSpeed, float steering, RaycastHit obstacle)
        {
            if (!profile.allowOvertaking || State == RivalState.PullIn || actualSpeed < 5f || Mathf.Abs(steering) > 12f ||
                obstacle.rigidbody == null || Vector3.Dot(obstacle.transform.forward, transform.forward) < .7f) return false;
            float closingSpeed = Vector3.Dot(body.linearVelocity - obstacle.rigidbody.linearVelocity, transform.forward);
            if (closingSpeed < 1.5f) return false;
            float passSeconds = Mathf.Clamp((obstacle.distance + 12f) / closingSpeed, 2.5f, 8f);
            float clearDistance = Mathf.Clamp((actualSpeed + 12f) * passSeconds + 16f, 45f, 130f);
            Vector3 passingOrigin = origin + transform.right * laneWidth;
            return Clear(passingOrigin, transform.forward, clearDistance, out _, obstacle.rigidbody) &&
                   Clear(passingOrigin, -transform.forward, Mathf.Max(25f, actualSpeed * passSeconds), out _) &&
                   Clear(origin, transform.right, laneWidth + 1f, out _);
        }
        private void Drive(Vector3 point, float desiredSpeed)
        {
            Vector3 delta = point - transform.position; delta.y = 0f;
            float steering = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
            desiredSpeed *= Mathf.Lerp(1f, .25f, Mathf.InverseLerp(10f, 65f, Mathf.Abs(steering)));
            Vector3 origin = transform.position + Vector3.up;
            float actualSpeed = body.linearVelocity.magnitude;
            bool clear = Clear(origin, transform.forward, Mathf.Max(7f, actualSpeed * profile.followingSeconds + 3f), out var obstacle);
            blockedTime = clear ? 0f : blockedTime + Time.fixedDeltaTime;
            bool noPassing = false;
            foreach (var zone in KombiRoadRules.Active)
            {
                Vector3 toZone = zone.transform.position - transform.position;
                if ((zone.rule == RoadRule.NoOvertaking || zone.rule == RoadRule.Stop || zone.rule == RoadRule.Crossing) &&
                    toZone.magnitude < 35f && Vector3.Dot(transform.forward, toZone.normalized) > .25f) noPassing = true;
            }
            foreach (var signal in signals)
                if (signal != null && signal.MustStop && Vector3.Distance(signal.transform.position, transform.position) < 35f &&
                    Vector3.Dot(transform.forward, (signal.transform.position - transform.position).normalized) > .4f) noPassing = true;

            Vector3 passingOrigin = origin + transform.right * laneWidth;
            float passLookAhead = Mathf.Clamp((actualSpeed + 12f) * Mathf.Max(.5f, passUntil - Time.time) + 20f, 45f, 140f);
            RaycastHit oncoming = default;
            bool passingLaneClear = !passing || Clear(passingOrigin, transform.forward, passLookAhead, out oncoming, passTarget);
            Vector3 returnOrigin = origin - transform.right * laneWidth;
            bool returnLaneClear = Clear(returnOrigin, transform.forward, 12f, out _) &&
                                   Clear(returnOrigin, -transform.forward, 12f, out _);
            bool passedTarget = passTarget == null ||
                                Vector3.Dot(passTarget.position - transform.position, transform.forward) < -4f;
            if (passing && returnLaneClear && (passedTarget || noPassing || !passingLaneClear))
            {
                passing = false; passTarget = null;
                Say(passingLaneClear ? "Returning to the left lane" : "Aborting overtake for oncoming traffic");
            }
            float overtakingDelay = profile.personality == RivalPersonality.Aggressive ? .6f : 1.1f;
            if (!passing && !noPassing && !clear && blockedTime > overtakingDelay && CanOvertake(origin, actualSpeed, steering, obstacle))
            {
                passing = true; passTarget = obstacle.rigidbody; passUntil = Time.time + 6f;
                Say("Clear opposing lane — overtaking slow traffic");
            }
            if (passing && !passingLaneClear && !returnLaneClear)
                desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Max(0f, (oncoming.distance - 10f) / profile.followingSeconds));
            laneOffset = Mathf.MoveTowards(laneOffset, passing ? laneWidth : 0f, Time.fixedDeltaTime * 2f);
            if (!clear && (!passing || obstacle.rigidbody != passTarget))
                desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Max(0f, (obstacle.distance - 4f) / profile.followingSeconds));
            if (blockedTime > 25f && Time.time >= nextEvaluation)
            { target = null; ChooseTarget(); Say("Blocked route: reassessing demand; waiting for clearance"); }
            foreach (var signal in signals)
                if (signal != null && signal.MustStop && Vector3.Distance(signal.transform.position, transform.position) < 15f && Vector3.Dot(transform.forward, (signal.transform.position - transform.position).normalized) > .4f) desiredSpeed = 0f;
            foreach (var zone in KombiRoadRules.Active)
            {
                float distance = Vector3.Distance(zone.transform.position, transform.position);
                if (distance > 25f) { stoppedAt.Remove(zone); continue; }
                if (zone.rule == RoadRule.SpeedLimit || zone.rule == RoadRule.Crossing || zone.rule == RoadRule.Hump) desiredSpeed = Mathf.Min(desiredSpeed, zone.limit / 3.6f * (profile.personality == RivalPersonality.Aggressive ? 1.15f : 1f));
                if (zone.rule == RoadRule.Stop && distance < 18f && profile.personality != RivalPersonality.Aggressive)
                {
                    if (!stoppedAt.ContainsKey(zone))
                    {
                        desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Sqrt(2f * profile.braking * Mathf.Max(0f, distance - 7f)));
                        if (actualSpeed < .3f) stoppedAt[zone] = Time.time;
                    }
                    else if (Time.time - stoppedAt[zone] < 1f) desiredSpeed = 0f;
                }
                if (zone.rule == RoadRule.NoEntry)
                {
                    Vector3 toZone = zone.transform.position - transform.position;
                    float ahead = Vector3.Dot(transform.forward, toZone);
                    float lateral = Mathf.Abs(Vector3.Dot(transform.right, toZone));
                    float halfWidth = zone.TryGetComponent<BoxCollider>(out var zoneCollider) ? zoneCollider.size.x * .5f : 0f;
                    if (ahead > 0f && ahead < 14f && lateral <= laneWidth * .5f + halfWidth) desiredSpeed = 0f;
                }
            }
            float error = desiredSpeed - actualSpeed;
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].steerAngle = i < 2 ? Mathf.Clamp(steering, -profile.maxSteering, profile.maxSteering) : 0f;
                wheels[i].motorTorque = desiredSpeed > .1f && error > 0f ? Mathf.Min(error, profile.acceleration) * body.mass * wheels[i].radius / 4f : 0f;
                wheels[i].brakeTorque = desiredSpeed < .1f || error < -.3f ? profile.braking * body.mass * wheels[i].radius / 4f : 0f;
            }
        }
        private void Brake() { if (wheels == null) return; foreach (var wheel in wheels) { wheel.motorTorque = 0f; wheel.brakeTorque = 4000f; } }
        private void OnDisable() { if (target != null) target.Release(transform); Brake(); }
        private int NearestRouteIndex(Vector3 point)
        {
            int nearest = 0; float best = float.MaxValue;
            if (route == null) return 0;
            for (int i = 0; i < route.Length; i++) { float d = FlatDistance(point, route[i].position); if (d < best) { best = d; nearest = i; } }
            return nearest;
        }
        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y; return Vector3.Distance(a, b); }
    }
}
