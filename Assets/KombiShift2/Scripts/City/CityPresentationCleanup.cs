using UnityEngine;

namespace KombiShift2.City
{
    // Runtime presentation clean-up applied when a shift starts (no scene or prefab is modified):
    // - keeps only the yellow centre line, hiding white edge/lane/stop-line markings;
    // - hides trees (and their trunk colliders) that stand on the driven road surface.
    public static class CityPresentationCleanup
    {
        static readonly string[] HiddenMarkings = { "Roadway white markings", "Road edge markings mesh", "Road carriage markings" };

        public static int Apply(CityRoadNetwork graph)
        {
            foreach (var name in HiddenMarkings)
            {
                var marking = GameObject.Find(name);
                if (marking != null) marking.SetActive(false);
            }
            if (graph == null || graph.Nodes == null || graph.Nodes.Length == 0) return 0;
            int removed = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!IsTree(renderer.name)) continue;
                var b = renderer.bounds;
                if (OnRoad(graph, new Vector3(b.center.x, b.min.y, b.center.z))) { renderer.gameObject.SetActive(false); removed++; }
            }
            foreach (var trunk in Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None))
            {
                if (!IsTree(trunk.name)) continue;
                var b = trunk.bounds;
                if (OnRoad(graph, new Vector3(b.center.x, b.min.y, b.center.z))) trunk.gameObject.SetActive(false);
            }
            return removed;
        }

        // "street" contains "tree": road meshes such as "City Street bz1" must never match.
        static bool IsTree(string name) { string n = name.ToLowerInvariant(); return n.Contains("tree") && !n.Contains("street"); }
        // True when the point lies on a road segment (half the baked width plus a small margin).
        static bool OnRoad(CityRoadNetwork graph, Vector3 point)
        {
            int index = graph.Nearest(point);
            if (index < 0) return false;
            var node = graph.Nodes[index];
            if (Mathf.Abs(node.position.y - point.y) > 6f) return false;
            float limit = node.width * .5f + .6f;
            if (Flat(node.position, point) < limit) return true;
            if (node.links == null) return false;
            foreach (int link in node.links)
            {
                if (link < 0 || link >= graph.Nodes.Length) continue;
                var other = graph.Nodes[link];
                if (SegmentDistance(point, node.position, other.position) < Mathf.Min(limit, other.width * .5f + .6f)) return true;
            }
            return false;
        }
        static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 p2 = new(p.x, p.z), a2 = new(a.x, a.z), b2 = new(b.x, b.z), ab = b2 - a2;
            float t = ab.sqrMagnitude < .0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p2, a2 + ab * t);
        }
    }
}
