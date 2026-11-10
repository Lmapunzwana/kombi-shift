using UnityEngine;

namespace KombiShift2.Traffic
{
    public sealed class KombiTrafficVehicle : MonoBehaviour
    {
        [SerializeField] private Transform[] route;
        [SerializeField] private int targetIndex;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float laneOffset = 1.55f;
        [SerializeField] private bool reverseRoute;
        private int routeDirection = 1;
        private TrafficSignalController[] signals;
        private float groundClearance = .05f;
        private readonly RaycastHit[] obstacleHits = new RaycastHit[64];
        private readonly RaycastHit[] groundHits = new RaycastHit[128];

        private void Start()
        {
            routeDirection = reverseRoute ? -1 : 1;
            signals = FindObjectsByType<TrafficSignalController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                groundClearance = Mathf.Max(.03f, transform.position.y - bounds.min.y + .03f);
            }
            GroundAt(transform.position);
        }

        public void Configure(Transform[] points, int startIndex, float metresPerSecond, bool reverse)
        {
            route = points;
            targetIndex = Mathf.Clamp(startIndex, 0, points.Length - 1);
            speed = metresPerSecond;
            reverseRoute = reverse;
            routeDirection = reverse ? -1 : 1;
            SnapToRoute();
        }

        private void Update()
        {
            if (route == null || route.Length < 2) return;
            Vector3 target = OffsetPoint(targetIndex);
            Vector3 delta = target - transform.position;
            delta.y = 0f;
            if (delta.magnitude < 4f)
            {
                targetIndex += routeDirection;
                if (targetIndex >= route.Length - 1) { targetIndex = route.Length - 1; routeDirection = -1; }
                else if (targetIndex <= 0) { targetIndex = 0; routeDirection = 1; }
                return;
            }
            Vector3 direction = delta.normalized;
            bool blocked = ObstacleAhead(direction);
            if (!blocked && signals != null)
            {
                foreach (TrafficSignalController signal in signals)
                {
                    Vector3 toSignal = signal.transform.position - transform.position;
                    if (signal.MustStop && toSignal.sqrMagnitude < 196f && Vector3.Dot(transform.forward, toSignal.normalized) > .45f)
                    {
                        blocked = true;
                        break;
                    }
                }
            }
            float movement = (blocked ? 0f : speed) * Time.deltaTime;
            Vector3 nextPosition = transform.position + direction * movement;
            GroundAt(nextPosition);
            if (direction.sqrMagnitude > .1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 4f);
        }

        private bool ObstacleAhead(Vector3 direction)
        {
            int count = Physics.SphereCastNonAlloc(transform.position + Vector3.up * .8f, .55f, direction, obstacleHits, 6f);
            var hits = count == obstacleHits.Length ? Physics.SphereCastAll(transform.position + Vector3.up * .8f, .55f, direction, 6f) : obstacleHits;
            if (hits != obstacleHits) count = hits.Length;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.rigidbody != null || hit.collider.GetComponentInParent<KombiTrafficVehicle>() != null) return true;
            }
            return false;
        }
#if UNITY_EDITOR
        // Same two queries as driving, measured in a controlled editor fixture.
        public int ProfileQueries(bool allocatingBaseline)
        {
            if (!allocatingBaseline)
            {
                int a = Physics.SphereCastNonAlloc(transform.position + Vector3.up * .8f, .55f, transform.forward, obstacleHits, 6f);
                int b = Physics.RaycastNonAlloc(transform.position + Vector3.up * 8f, Vector3.down, groundHits, 24f, ~0, QueryTriggerInteraction.Ignore);
                return a + b;
            }
            return Physics.SphereCastAll(transform.position + Vector3.up * .8f, .55f, transform.forward, 6f).Length
                + Physics.RaycastAll(transform.position + Vector3.up * 8f, Vector3.down, 24f, ~0, QueryTriggerInteraction.Ignore).Length;
        }
#endif

        private Vector3 OffsetPoint(int index)
        {
            Vector3 point = route[index].position;
            int neighbour = Mathf.Clamp(index + (routeDirection >= 0 ? 1 : -1), 0, route.Length - 1);
            bool endpoint = neighbour == index;
            if (endpoint) neighbour = Mathf.Clamp(index - routeDirection, 0, route.Length - 1);
            Vector3 forward = endpoint ? point - route[neighbour].position : route[neighbour].position - point;
            forward.y = 0f;
            Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
            return point - right * laneOffset;
        }

        private void SnapToRoute()
        {
            if (route == null || route.Length < 2) return;
            transform.position = OffsetPoint(targetIndex) + Vector3.up * .5f;
            Vector3 direction = OffsetPoint(Mathf.Clamp(targetIndex + routeDirection, 0, route.Length - 1)) - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > .1f) transform.rotation = Quaternion.LookRotation(direction);
        }

        private void GroundAt(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * 8f, Vector3.down, groundHits, 24f, ~0, QueryTriggerInteraction.Ignore);
            // Saturated buffers fall back to the complete query, preserving collision correctness.
            RaycastHit[] hits = count == groundHits.Length ? Physics.RaycastAll(position + Vector3.up * 8f, Vector3.down, 24f, ~0, QueryTriggerInteraction.Ignore) : groundHits;
            if (hits != groundHits) count = hits.Length;
            float bestY = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<KombiTrafficVehicle>() != null) continue;
                if (hit.point.y > bestY) bestY = hit.point.y;
            }
            position.y = float.IsNegativeInfinity(bestY) ? position.y : bestY + groundClearance;
            transform.position = position;
        }
    }
}
