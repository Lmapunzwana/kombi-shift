using UnityEngine;

namespace KombiShift2.City
{
    // Puts a stuck or flipped vehicle back on the road: on the left-hand lane of the nearest road
    // segment, facing along the road (the direction closest to where it was heading), at rest.
    public static class CityRoadRecovery
    {
        const float LaneOffset = 1.55f;

        public static bool Place(Rigidbody body, Vector3 preferredForward, float aheadMetres = 6f)
        {
            var graph = CityRoadNetwork.Active;
            if (body == null || graph == null || graph.Nodes == null || graph.Nodes.Length == 0) return false;
            int index = graph.Nearest(body.position);
            if (index < 0) return false;
            var node = graph.Nodes[index];
            preferredForward.y = 0f;
            if (preferredForward.sqrMagnitude < .01f) preferredForward = body.transform.forward;
            preferredForward.y = 0f; preferredForward.Normalize();

            // Choose the linked segment that best matches the vehicle's intended direction.
            Vector3 forward = preferredForward; float best = -2f;
            if (node.links != null)
                foreach (int link in node.links)
                {
                    if (link < 0 || link >= graph.Nodes.Length) continue;
                    Vector3 d = graph.Nodes[link].position - node.position; d.y = 0f;
                    if (d.sqrMagnitude < .01f) continue;
                    d.Normalize();
                    float score = Vector3.Dot(d, preferredForward);
                    if (score > best) { best = score; forward = d; }
                }
            Vector3 left = -Vector3.Cross(Vector3.up, forward);
            Vector3 position = node.position + forward * aheadMetres + left * LaneOffset;
            position = Ground(position) + Vector3.up * .35f;

            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.position = position; body.rotation = Quaternion.LookRotation(forward, Vector3.up);
            body.transform.SetPositionAndRotation(position, body.rotation);
            Physics.SyncTransforms();
            return true;
        }

        public static bool IsFlipped(Transform vehicle) => vehicle.up.y < .45f;

        static Vector3 Ground(Vector3 p)
        {
            var hits = Physics.RaycastAll(p + Vector3.up * 4f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float y = float.NegativeInfinity;
            foreach (var hit in hits)
                if (hit.rigidbody == null && hit.normal.y > .7f && hit.point.y > y) y = hit.point.y;
            if (!float.IsNegativeInfinity(y)) p.y = y;
            return p;
        }
    }
}
