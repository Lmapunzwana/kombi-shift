using System;
using KombiShift2.City;
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
        private int routeIndex, destinationIndex, direction = 1;
        private float startedAt, nextEvaluation, nextDeparture, blockedTime;
        private bool shiftEnded;
        private CityRivalNavigation cityNavigation;
        private bool passing;
        private float passUntil,passOffset,nextPass,cityStoppedAt;
        private CityIntersection cityStop;
        private bool cityStopCompleted;
        private bool headingHome;
        private float nextHomeCheck, progressAt, forceUntil, junctionWaitStart;
        private Vector3 progressAnchor;
        private KombiShiftGameDirector director;
        public bool FinishedTrip { get; private set; }
        private readonly RaycastHit[] obstacles = new RaycastHit[64];
        private readonly Collider[] congestion = new Collider[32];
        public int PassengersTaken { get; private set; }
        // Riders currently on board and the stop each one wants to get off at.
        private readonly Dictionary<StationPassenger, PassengerStation> riders = new();
        public int Onboard => riders.Count;
        private bool servicePass;
        private int Drops(PassengerStation station) { int n = 0; foreach (var r in riders) if (r.Key != null && r.Value == station) n++; return n; }
        private PassengerStation PickDestination(PassengerStation from)
        {
            int start = Array.IndexOf(stations, from);
            var terminal = Array.Find(stations, x => x != null && x.Terminal);
            if (start < 0 || UnityEngine.Random.value < .35f) return terminal;
            var ahead = new List<PassengerStation>();
            for (int i = start + 1; i < stations.Length; i++) if (stations[i] != null) ahead.Add(stations[i]);
            return ahead.Count > 0 ? ahead[UnityEngine.Random.Range(0, ahead.Count)] : terminal;
        }
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
            director = FindFirstObjectByType<KombiShiftGameDirector>();
            if(CityTripSession.Active)cityNavigation=gameObject.AddComponent<CityRivalNavigation>();
        }
        private void FixedUpdate()
        {
            bool waitingForStart = director != null && !director.RaceStarted;
            if (waitingForStart) startedAt = Time.time;
            if (waitingForStart || shiftEnded || route == null || route.Length < 2 || stations == null || Time.time - startedAt < departureDelay || State == RivalState.Boarding)
            { Brake(); return; }
            var terminal = Array.Find(stations, x => x != null && x.Terminal);
            bool checkHome = !headingHome && terminal != null && Time.time >= nextHomeCheck;
            if (checkHome) nextHomeCheck = Time.time + 1f;
            if (checkHome && ShouldHeadToTerminal(terminal))
            {
                headingHome = true;
                if (target != null && target != terminal) target.Release(transform);
                Say("Heading for " + terminal.name + " with " + Onboard + " passengers");
            }
            bool full = headingHome;
            if (full)
            {
                target = terminal;
                if (target == null) { CruiseRoute(); return; }
                destinationIndex = NearestRouteIndex(target.ServicePoint.position);
                State = RivalState.Depart;
                // At the terminal any stop near the bay counts: exact alignment is not required to drop off.
                float toBay = FlatDistance(target.ServicePoint.position, transform.position);
                if (toBay < 6f || (toBay < 12f && blockedTime > 3f))
                {
                    Brake();
                    if (body.linearVelocity.magnitude < 1f) FinishTrip();
                    return;
                }
            }
            else if (Time.time >= nextDeparture && (target == null || !(target.HasWaiting || Drops(target) > 0) || (target.Reserved && Drops(target) == 0) || Time.time >= nextEvaluation)) ChooseTarget();
            if (target == null) { State = RivalState.Cruise; CruiseRoute(); return; }
            float distance = FlatDistance(target.ServicePoint.position, transform.position);
            if (distance < 2.2f && target.MatchesApproach(transform))
            {
                Brake();
                if (body.linearVelocity.magnitude > .5f) return;
                if (full) { FinishTrip(); return; }
                State = RivalState.Boarding; StartCoroutine(Collect()); return;
            }
            if (Mathf.Abs(routeIndex - destinationIndex) <= 1 && distance < 10f)
            { if (!full) State = RivalState.PullIn; Drive(target.ServicePoint.position, Mathf.Min(2.4f, distance)); }
            else
            {
                if (!full) State = RivalState.ChasePassenger;
                FollowRoute();
            }
        }
        // Go to the terminal when full, when no passengers are left ahead, or when the clock says it is time to finish.
        private bool ShouldHeadToTerminal(PassengerStation terminal)
        {
            // Still worth stopping ahead: a stop where riders get off, or (with free seats) a waiting queue.
            bool demandAhead = false;
            foreach (var station in stations)
            {
                if (station == null || station.Terminal) continue;
                if (Drops(station) == 0 && !(station.HasWaiting && Onboard < cabin.Seats.Length)) continue;
                if (station == target || !float.IsPositiveInfinity(RouteDistance(NearestRouteIndex(station.ServicePoint.position)))) { demandAhead = true; break; }
            }
            if (!demandAhead) return true;
            if (director == null) return false;
            int terminalIndex = NearestRouteIndex(terminal.ServicePoint.position);
            float distance = RouteDistance(terminalIndex);
            if (float.IsPositiveInfinity(distance)) distance = FlatDistance(transform.position, terminal.ServicePoint.position) * 1.5f;
            float eta = distance / Mathf.Max(3f, speed * profile.speedMultiplier * .55f);
            return director.TimeRemaining <= eta + 30f;
        }
        private void FinishTrip()
        {
            if (FinishedTrip) return;
            FinishedTrip = true; shiftEnded = true; Brake();
            if (target != null) target.Release(transform);
            Say("Reached " + (target != null ? target.name : "the terminal") + " - dropping off " + Onboard + " passengers");
            StartCoroutine(LeaveTerminal(target));
        }
        private IEnumerator LeaveTerminal(PassengerStation terminal)
        {
            // Passengers walk out through the door to the terminal, then the kombi clears the bay
            // so it never blocks the player's stop.
            var onboard = new List<StationPassenger>();
            foreach (var person in GetComponentsInChildren<StationPassenger>()) if (person.Boarded) onboard.Add(person);
            for (int i = 0; i < onboard.Count; i++)
            {
                Transform arrival = null; bool seated = false;
                if (terminal != null) arrival = terminal.ClaimArrival(onboard[i], out seated);
                onboard[i].LeaveAt(arrival != null ? arrival : terminal != null ? terminal.ServicePoint : null, seated, cabin.Doorway, i * .55f);
            }
            float deadline = Time.time + 6f + onboard.Count * .6f;
            while (Time.time < deadline && onboard.Exists(p => p != null && p.State != PassengerState.Delivered)) { Brake(); yield return null; }
            yield return new WaitForSeconds(1f);
            Say("Finished the trip and left the terminal");
            gameObject.SetActive(false);
        }
        private void CruiseRoute() => FollowRoute();
        private void FollowRoute()
        {
            if(cityNavigation!=null && target!=null)
            {
                routeIndex=NearestRouteIndex(transform.position);
                Vector3 point=cityNavigation.Point(target.ServicePoint);
                if(!cityNavigation.HasPath){Brake();return;}
                Drive(point,speed*profile.speedMultiplier);
                return;
            }
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
            return route[index].position + LeftOffset(forward, laneWidth * .5f);
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
                if (station == null || station.Terminal || direction < 0) continue;
                int drops = Drops(station);
                bool canBoard = station.HasWaiting && !station.Reserved && Onboard < cabin.Seats.Length;
                if (!canBoard && drops == 0) continue;
                int index = NearestRouteIndex(station.ServicePoint.position);
                int count = Physics.OverlapSphereNonAlloc(station.ServicePoint.position, 10f, congestion, ~0, QueryTriggerInteraction.Ignore);
                int vehicles = 0;
                for (int i = 0; i < count; i++) if (congestion[i].attachedRigidbody != null && congestion[i].attachedRigidbody != body) vehicles++;
                float utility = profile.Utility(canBoard ? station.WaitingCount : 0, cabin.Seats.Length - Onboard, RouteDistance(index), speed, vehicles, false) + drops * profile.passengerValue;
                if (station == target) currentValue = utility;
                if (utility > bestValue) { best = station; bestValue = utility; bestIndex = index; }
            }
            if (target != null && (target.HasWaiting || Drops(target) > 0) && best != target && bestValue < currentValue + profile.switchAdvantage) return;
            if (best != target && best != null) Say("Targeting " + best.name + " (waiting " + best.WaitingCount + ", dropping " + Drops(best) + ")");
            target = best; destinationIndex = bestIndex;
        }
        private IEnumerator Collect()
        {
            var stop = target;
            // Riders whose stop this is get off first, walking out through the door.
            var leaving = new List<StationPassenger>();
            foreach (var r in riders) if (r.Key != null && r.Value == stop) leaving.Add(r.Key);
            for (int i = 0; i < leaving.Count; i++)
            {
                riders.Remove(leaving[i]);
                var arrival = stop.ClaimArrival(leaving[i], out bool seated);
                leaving[i].LeaveAt(arrival != null ? arrival : stop.transform, seated, cabin.Doorway, i * .55f);
            }
            if (leaving.Count > 0)
            {
                Say("Dropping off " + leaving.Count + " at " + stop.name);
                float until = Time.time + 3f + leaving.Count * .6f;
                while (Time.time < until && leaving.Exists(x => x != null && x.State != PassengerState.Delivered)) { Brake(); yield return null; }
            }
            if (stop.HasWaiting && Onboard < cabin.Seats.Length)
                yield return stop.Board(transform, cabin.Doorway, cabin.Seats, Onboard,
                    person => { PassengersTaken++; riders[person] = PickDestination(stop); Say("Picked up passenger for " + (riders[person] != null ? riders[person].name : "the terminal") + " - " + Onboard + " on board"); },
                    () => !shiftEnded && isActiveAndEnabled && stop != null && stop.MatchesApproach(transform) && body.linearVelocity.magnitude < .6f && FlatDistance(transform.position, stop.ServicePoint.position) < 4f);
            stop.Release(transform); target = null; State = RivalState.Depart; nextDeparture = Time.time + 2f;
            Say("Departing " + stop.name); routeIndex = NearestRouteIndex(transform.position);
        }
        // A vehicle standing at a bus stop (a loading rival, the player or a stopped car) may be overtaken.
        private bool ServiceStopAhead(RaycastHit hit)
        {
            var other = hit.rigidbody;
            if (other == null || other.linearVelocity.magnitude > 1f) return false;
            var rival = other.GetComponentInParent<RivalKombiAI>();
            if (rival != null && (rival.State == RivalState.Boarding || rival.FinishedTrip)) return true;
            foreach (var station in stations)
                if (station != null && FlatDistance(station.ServicePoint.position, other.position) < 15f) return true;
            return false;
        }
        private bool Clear(Vector3 origin, Vector3 forward, float distance, out RaycastHit nearest)
        {
            int count = Physics.SphereCastNonAlloc(origin, 1f, forward, obstacles, distance, ~0, QueryTriggerInteraction.Ignore);
            var hits = count == obstacles.Length ? Physics.SphereCastAll(origin, 1f, forward, distance, ~0, QueryTriggerInteraction.Ignore) : obstacles;
            if (hits != obstacles) count = hits.Length;
            nearest = default; float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i]; if (hit.transform.IsChildOf(transform) || hit.normal.y > .6f) continue;
                // Small static props (signal poles, sign posts, kerb furniture) beside the lane are not traffic ahead.
                if (hit.rigidbody == null && hit.collider.bounds.extents.magnitude < 3f) continue;
                if (hit.distance < best) { best = hit.distance; nearest = hit; }
            }
            return best == float.MaxValue;
        }
        private void Drive(Vector3 point, float desiredSpeed)
        {
            var road=cityNavigation!=null?cityNavigation.Node:null;
            passOffset=Mathf.MoveTowards(passOffset,passing?3.1f:0f,Time.fixedDeltaTime*1.2f);
            point+=transform.right*passOffset;
            var condition=GetComponent<KombiShift2.Vehicle.KombiVehicleCondition>();
            if(condition!=null)desiredSpeed*=condition.PowerFactor;
            Vector3 delta = point - transform.position; delta.y = 0f;
            float steering = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
            desiredSpeed *= Mathf.Lerp(1f, .25f, Mathf.InverseLerp(10f, 65f, Mathf.Abs(steering)));
            Vector3 origin = transform.position + Vector3.up;
            float actualSpeed = body.linearVelocity.magnitude;
            float braking = Mathf.Max(.5f, profile.braking);
            float detectionDistance = Mathf.Max(14f, Mathf.Max(actualSpeed * profile.followingSeconds + 8f, actualSpeed * actualSpeed / (2f * braking) + 8f));
            bool clear = Clear(origin, transform.forward, detectionDistance, out var obstacle);
            blockedTime = clear ? 0f : blockedTime + Time.fixedDeltaTime;
            // Passing is restricted to a second same-direction lane in the baked road metadata.
            var junction=CityIntersection.Near(point);
            if(road!=null)
            {
                desiredSpeed=Mathf.Min(desiredSpeed,road.speedLimit/3.6f*profile.LimitTolerance);
                // Overtake a kombi loading at a stop, or (aggressive kombis) a queue that has stood still for 4 s.
                bool stoppedAtStop=!clear&&(ServiceStopAhead(obstacle)||(profile.personality==RivalPersonality.Aggressive&&obstacle.rigidbody!=null&&obstacle.rigidbody.linearVelocity.magnitude<1f&&blockedTime>4f));
                if(!passing && profile.allowOvertaking && (road.allowPassing && road.width>=13f || stoppedAtStop) && junction==null && !clear && obstacle.rigidbody!=null && obstacle.distance>4f && blockedTime>(stoppedAtStop?.8f:2f) && Mathf.Abs(steering)<8f && Time.time>nextPass)
                {
                    Vector3 lane=origin+transform.right*3.1f;
                    if(Clear(lane,transform.forward,stoppedAtStop?35f:45f,out _) && Clear(lane,-transform.forward,stoppedAtStop?12f:20f,out _)){passing=true;servicePass=stoppedAtStop;passUntil=Time.time+(stoppedAtStop?5f:7f);Say(stoppedAtStop?"Overtaking a kombi loading at the stop":"Passing slow traffic in a clear same-direction lane");}
                }
                if(passing && (Time.time>passUntil||(!road.allowPassing&&!servicePass)||junction!=null))
                {
                    Vector3 lane=origin-transform.right*3.1f;
                    if(Clear(lane,transform.forward,12f,out _)&&Clear(lane,-transform.forward,12f,out _)){passing=false;servicePass=false;nextPass=Time.time+3;}
                }
            }
            // Stuck recovery: a rival that has not moved for a while (and is not simply waiting out a red
            // light) squeezes through slowly, the way a kombi driver would, instead of freezing forever.
            bool redLight = junction != null && junction.signalised && !junction.SignalAllows(transform.forward);
            float stuckLimit = redLight ? (CityIntersection.GreenSeconds + CityIntersection.AmberSeconds + CityIntersection.AllRedSeconds) * junction.PhaseCount + 5f : 3f;
            if ((transform.position - progressAnchor).sqrMagnitude > 4f) { progressAnchor = transform.position; progressAt = Time.time; }
            if (Time.time - progressAt > stuckLimit && Time.time >= forceUntil)
            { forceUntil = Time.time + 4f; progressAt = Time.time; Say("Squeezing through the hold-up"); }
            bool forcing = Time.time < forceUntil;
            if (forcing) desiredSpeed = Mathf.Min(desiredSpeed, 7f);
            // Squeezing through never rams a vehicle close ahead.
            if (!clear && (!forcing || (obstacle.rigidbody != null && obstacle.distance < 6f)))
            {
                float leadSpeed=obstacle.rigidbody!=null?Mathf.Max(0f,Vector3.Dot(obstacle.rigidbody.linearVelocity,transform.forward)):0f;
                float safeDistance=Mathf.Max(0f,obstacle.distance-4f);
                float safeSpeed=Mathf.Sqrt(leadSpeed*leadSpeed+2f*braking*safeDistance);
                desiredSpeed=Mathf.Min(desiredSpeed,safeSpeed);
            }
            if(junction!=cityStop){cityStop=junction;cityStopCompleted=false;cityStoppedAt=0;junctionWaitStart=0f;}
            if(junction!=null && !forcing)
            {
                bool occasionalRollingStop=profile.personality==RivalPersonality.Aggressive&&Mathf.RoundToInt(Mathf.Abs(junction.transform.position.x+junction.transform.position.z))%7==0;
                bool stopRequired=junction.RequiresStop(transform.forward)&&!occasionalRollingStop;
                if(stopRequired&&!cityStopCompleted)
                {
                    if(actualSpeed<.3f){if(cityStoppedAt==0)cityStoppedAt=Time.time;cityStopCompleted=Time.time-cityStoppedAt>.4f;}
                    desiredSpeed=0;
                }
                bool mayEnter=junction.MayEnter(body,transform.forward,point+transform.forward*12f);
                // Aggressive kombis do not wait for a fully empty junction on green: after 2 s they push in.
                if(!mayEnter&&profile.personality==RivalPersonality.Aggressive&&junction.SignalAllows(transform.forward))
                {if(junctionWaitStart==0f)junctionWaitStart=Time.time;mayEnter=Time.time-junctionWaitStart>2f;}
                else if(mayEnter)junctionWaitStart=0f;
                if(stopRequired&&!cityStopCompleted || !mayEnter)desiredSpeed=0;
            }
            if (!headingHome && blockedTime > 25f && Time.time >= nextEvaluation)
            { target = null; ChooseTarget(); Say("Blocked route: reassessing demand; waiting for clearance"); }
            foreach (var zone in KombiRoadRules.Active)
            {
                float distance = Vector3.Distance(zone.transform.position, transform.position);
                if (distance > 25f) continue;
                if (zone.rule == RoadRule.SpeedLimit || zone.rule == RoadRule.Crossing || zone.rule == RoadRule.Hump) desiredSpeed = Mathf.Min(desiredSpeed, zone.limit / 3.6f * profile.LimitTolerance);
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
        private void Brake() { progressAt = Time.time; if (wheels == null) return; foreach (var wheel in wheels) { wheel.motorTorque = 0f; wheel.brakeTorque = 4000f; } }
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
