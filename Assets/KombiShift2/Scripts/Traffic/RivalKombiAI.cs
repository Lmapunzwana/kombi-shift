using System.Collections;
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
        [SerializeField] private float speed = 7.5f;
        [SerializeField] private float departureDelay = 18f;
        private PassengerStation target;
        private KombiPassengerCabin cabin;
        private Rigidbody body;
        private int routeIndex;
        private int destinationIndex;
        private float blockedTime;
        private float startedAt;
        private readonly RaycastHit[] obstacles = new RaycastHit[20];
        public int PassengersTaken { get; private set; }
        public RivalState State { get; private set; }
        public PassengerStation Target => target;
        public float Speed => speed;
        public void Configure(Transform[] points, PassengerStation[] waitingStops, float delay, float cruisingSpeed)
        { route = points; stations = waitingStops; departureDelay = delay; speed = cruisingSpeed; }

        private void Awake()
        {
            cabin = GetComponent<KombiPassengerCabin>(); body = GetComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false;
        }
        private void Start()
        {
            routeIndex = NearestRouteIndex(transform.position);
            startedAt = Time.time;
        }

        private void Update()
        {
            if (route == null || route.Length < 2 || stations == null || Time.time - startedAt < departureDelay || State == RivalState.Boarding) return;
            if (PassengersTaken >= cabin.Seats.Length) { State = RivalState.Depart; return; }
            if (target == null || !target.HasWaiting || (target.Reserved && State != RivalState.Boarding)) ChooseTarget();
            if (target == null) { State = RivalState.Cruise; return; }
            Vector3 toService = target.ServicePoint.position - transform.position; toService.y = 0f;
            if (toService.magnitude < 2.2f)
            {
                State = RivalState.Boarding;
                StartCoroutine(Collect()); return;
            }
            if (routeIndex == destinationIndex)
            {
                State = RivalState.PullIn;
                Drive(target.ServicePoint.position, 2.4f);
            }
            else
            {
                State = RivalState.ChasePassenger;
                Vector3 waypoint = route[routeIndex].position;
                if (FlatDistance(waypoint, transform.position) < 2.5f)
                { routeIndex += destinationIndex > routeIndex ? 1 : -1; waypoint = route[routeIndex].position; }
                Drive(waypoint, speed);
            }
        }

        private void ChooseTarget()
        {
            target = null; float best = float.MaxValue;
            foreach (var station in stations)
            {
                if (station == null || !station.HasWaiting || station.Reserved) continue;
                int index = NearestRouteIndex(station.ServicePoint.position);
                float distance = Mathf.Abs(index - routeIndex);
                if (distance >= best) continue;
                best = distance; target = station; destinationIndex = index;
            }
        }

        private IEnumerator Collect()
        {
            var stop = target;
            yield return stop.Board(transform, cabin.Doorway, cabin.Seats, PassengersTaken,
                person => PassengersTaken++, () => isActiveAndEnabled && stop != null);
            target = null;
            routeIndex = NearestRouteIndex(transform.position);
            State = RivalState.Depart;
        }

        private void Drive(Vector3 targetPosition, float metresPerSecond)
        {
            Vector3 delta = targetPosition - transform.position; delta.y = 0f;
            if (delta.sqrMagnitude < .01f) return;
            Vector3 direction = delta.normalized;
            int count = Physics.SphereCastNonAlloc(transform.position + Vector3.up * .65f, .5f, direction, obstacles, 4.5f, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                var hit = obstacles[i];
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.rigidbody != null || hit.collider.GetComponentInParent<KombiTrafficVehicle>() != null)
                { blocked = true; break; }
            }
            blockedTime = blocked ? blockedTime + Time.deltaTime : 0f;
            // Wait behind vehicles instead of driving through the player or teleporting.
            if (blocked) return;
            Vector3 position = Vector3.MoveTowards(transform.position, targetPosition, metresPerSecond * Time.deltaTime);
            position.y = Mathf.MoveTowards(transform.position.y, targetPosition.y + .08f, Time.deltaTime * 2f);
            body.MovePosition(position);
            body.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 3.5f));
        }

        private int NearestRouteIndex(Vector3 point)
        {
            int nearest = 0; float best = float.MaxValue;
            if (route == null) return nearest;
            for (int i = 0; i < route.Length; i++)
            {
                float distance = FlatDistance(point, route[i].position);
                if (distance < best) { best = distance; nearest = i; }
            }
            return nearest;
        }
        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y; return Vector3.Distance(a,b); }
    }
}
