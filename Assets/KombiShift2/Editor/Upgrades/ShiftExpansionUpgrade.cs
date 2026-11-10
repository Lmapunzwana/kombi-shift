using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BezierSolution;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class KombiShiftExpansionUpgrade
{
    const string RootName = "Shift Expansion 2026";
    [MenuItem("Kombi Shift/Apply Service And Route Expansion")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        if (GameObject.Find(RootName) != null) throw new Exception("Expansion already applied; refusing to duplicate route/stations.");
        var root = new GameObject(RootName).transform;
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var director = Object.FindFirstObjectByType<KombiShiftGameDirector>();
        var splines = Object.FindObjectsByType<BezierSpline>(FindObjectsSortMode.None).OrderBy(x => x.name).ToArray();
        var prefix = new List<Transform>();
        foreach (var road in splines.Where(x => x.name == "bz0" || x.name == "bz1"))
        {
            int samples = road.name == "bz0" ? 20 : 42;
            for (int i = road.name == "bz0" ? 4 : 1; i <= samples; i++)
            {
                // Exclude the bz1 endpoint already present as the first old route point.
                if (road.name == "bz1" && i == samples) break;
                Vector3 point = Ground(road.GetPoint(i / (float)samples));
                var waypoint = new GameObject("Extended lane centre " + prefix.Count).transform;
                waypoint.SetParent(root); waypoint.position = point; prefix.Add(waypoint);
            }
        }
        var route = prefix.Concat(hud.RoutePoints).ToArray();
        var directorData = new SerializedObject(director); var stopArray = directorData.FindProperty("stops");
        var existing = Enumerable.Range(0, stopArray.arraySize).Select(i => ((Transform)stopArray.GetArrayElementAtIndex(i).objectReferenceValue).GetComponent<PassengerStation>()).ToArray();
        var newStops = new List<PassengerStation>();
        int[] indices = { 6, 26, 46 };
        string[] names = { "West Market", "Arterial Junction", "CBD Approach" };
        for (int i = 0; i < indices.Length; i++)
        {
            int index = indices[i];
            Vector3 forward = route[index + 1].position - route[index - 1].position; forward.y = 0; forward.Normalize();
            Vector3 left = RivalKombiAI.LeftOffset(forward, 1f);
            var original = existing[0].ServicePoint.parent;
            var group = Object.Instantiate(original.gameObject, root).transform;
            group.name = names[i]; group.position = route[index].position + left * 7f; group.rotation = Quaternion.LookRotation(-left);
            var point = group.Find("Kombi Service Point"); point.position = route[index].position + left * 2.7f; point.rotation = Quaternion.LookRotation(forward);
            var station = group.gameObject.AddComponent<PassengerStation>();
            var people = group.GetComponentsInChildren<StationPassenger>();
            var seats = group.GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith("Bench Seat")).ToArray();
            station.Configure(people, point, seats, false);
            foreach (var text in group.GetComponentsInChildren<TextMesh>()) text.text = names[i].ToUpperInvariant();
            newStops.Add(station);
        }
        var stations = newStops.Concat(existing).ToArray();
        // Existing stops remain untouched; navigation references include both old and new stations.
        director.Configure(director.Vehicle, stations.Select(x => x.transform).ToArray());
        hud.Configure(director.Vehicle, route, director);
        var player = director.Vehicle.transform;
        Vector3 heading = route[2].position - route[1].position; heading.y = 0;
        player.SetPositionAndRotation(route[1].position + RivalKombiAI.LeftOffset(heading, 1.55f) + Vector3.up * .12f, Quaternion.LookRotation(heading));
        int rivalIndex = 0;
        foreach (var rival in Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None))
        {
            int index = 2 + rivalIndex * 2;
            Vector3 forward = route[index + 1].position - route[index].position; forward.y = 0;
            rival.transform.SetPositionAndRotation(route[index].position + RivalKombiAI.LeftOffset(forward, 1.55f) + Vector3.up * .12f, Quaternion.LookRotation(forward));
            rival.Configure(route, stations, 18f + rivalIndex * 18f, 7.5f); rivalIndex++;
        }
        foreach (var traffic in Object.FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))
        {
            var data = new SerializedObject(traffic);
            var path = data.FindProperty("route"); path.arraySize = route.Length;
            for (int i = 0; i < route.Length; i++) path.GetArrayElementAtIndex(i).objectReferenceValue = route[i];
            data.FindProperty("targetIndex").intValue += prefix.Count;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        for (int n = 0; n < stations.Length; n++)
        {
            int index = Nearest(route, stations[n].ServicePoint.position);
            Vector3 forward = route[Mathf.Min(route.Length - 1, index + 1)].position - route[Mathf.Max(0, index - 1)].position; forward.y = 0; forward.Normalize();
            MakeSign(root, route[index].position, forward, RoadRule.SpeedLimit, n % 2 == 0 ? 30f : 50f, 35f);
            if (n < stations.Length - 1)
            {
                int approach = Mathf.Max(1, index - 2);
                Vector3 tangent = route[approach + 1].position - route[approach - 1].position; tangent.y = 0; tangent.Normalize();
                MakeSign(root, route[approach].position, tangent, RoadRule.Crossing, 20f, 12f);
                MakeSign(root, route[approach].position - tangent * 10f, tangent, RoadRule.Hump, 20f, 4f);
                MakeHump(root, Ground(route[approach].position - tangent * 10f), tangent);
                MakeSign(root, route[index].position - forward * 14f, forward, RoadRule.NoOvertaking, 30f, 30f);
            }
        }
        MakeSign(root, route[25].position, (route[26].position-route[24].position).normalized, RoadRule.Stop, 0f, 20f);
        MakeSign(root, route[38].position, (route[39].position-route[37].position).normalized, RoadRule.GiveWay, 20f, 18f);
        MakeSign(root, route[43].position, (route[44].position-route[42].position).normalized, RoadRule.Roadworks, 20f, 18f);
        MakeSign(root, route[12].position, (route[13].position-route[11].position).normalized, RoadRule.Direction, 30f, 40f);
        // Prohibited side access does not obstruct the through route.
        Vector3 side = route[38].position + RivalKombiAI.LeftOffset(route[39].position-route[37].position, -8f);
        MakeSign(root, side, Vector3.right, RoadRule.NoEntry, 0f, 5f);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        float length = 0; for (int i = 1; i < route.Length; i++) length += Vector3.Distance(route[i-1].position,route[i].position);
        Directory.CreateDirectory("Docs/Validation/Expansion");
        File.WriteAllText("Docs/Validation/Expansion/scene-upgrade.txt", $"Unity {Application.unityVersion}\nUTC {DateTime.UtcNow:o}\nRoute points {route.Length}; stations {stations.Length}; route length {length:F1} m\nReused connected bz0/bz1 road geometry, colliders and existing bz2. No scene GUID changes.\n");
        Debug.Log("KOMBI_EXPANSION_SCENE_READY " + length);
    }
    public static void PositionRivalsBehindPlayer()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var director = Object.FindFirstObjectByType<KombiShiftGameDirector>();
        var old = hud.RoutePoints;
        var first = GameObject.Find("Rival head start route point");
        var route = old;
        if (first == null)
        {
            var road = Object.FindObjectsByType<BezierSpline>(FindObjectsSortMode.None).First(x => x.name == "bz0");
            first = new GameObject("Rival head start route point"); first.transform.SetParent(GameObject.Find(RootName).transform);
            first.transform.position = Ground(road.GetPoint(.15f));
            route = new[] { first.transform }.Concat(old).ToArray();
        }
        var data = new SerializedObject(director); var stops = data.FindProperty("stops");
        var stations = Enumerable.Range(0,stops.arraySize).Select(i => ((Transform)stops.GetArrayElementAtIndex(i).objectReferenceValue).GetComponent<PassengerStation>()).ToArray();
        hud.Configure(director.Vehicle,route,director);
        int n = 0;
        foreach (var rival in Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None).OrderBy(x => x.name))
        {
            int index = n++ == 0 ? 1 : 0;
            Vector3 forward = route[index+1].position-route[index].position; forward.y=0f;
            rival.transform.SetPositionAndRotation(route[index].position+RivalKombiAI.LeftOffset(forward,1.55f)+Vector3.up*.12f,Quaternion.LookRotation(forward));
            rival.Configure(route,stations,rival.DepartureDelay,rival.Speed);
        }
        if (route != old) foreach (var traffic in Object.FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))
        {
            var serialized = new SerializedObject(traffic); var path = serialized.FindProperty("route"); path.arraySize=route.Length;
            for(int i=0;i<route.Length;i++)path.GetArrayElementAtIndex(i).objectReferenceValue=route[i];
            serialized.FindProperty("targetIndex").intValue++; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        RepairRouteTreeColliders(); KombiExpansionValidation.Audit();
        Debug.Log("KOMBI_RIVALS_POSITIONED_BEHIND_PLAYER");
    }
    [MenuItem("Kombi Shift/Repair Hidden Roadside Collisions")]
    public static void RepairRouteTreeColliders()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var route = hud.RoutePoints;
        int repaired = 0; var details = new List<string>();
        foreach (var collider in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
        {
            string name = collider.name.ToLowerInvariant();
            if (!collider.enabled || !name.Contains("tree") || !name.Contains("plane")) continue;
            Bounds bounds = collider.bounds;
            Bounds roadCorridor = bounds;
            roadCorridor.Expand(8f);
            bool crossesRoad = false;
            for (int i = 1; i < route.Length && !crossesRoad; i++)
                crossesRoad = SegmentIntersectsBounds(route[i - 1].position + Vector3.up, route[i].position + Vector3.up, roadCorridor);
            if (!crossesRoad) continue;
            // Alpha-cutout leaves are not solid walls. Preserve the art and collide with its trunk.
            collider.enabled = false;
            var trunkPosition = new Vector3(bounds.center.x, bounds.min.y + 1.5f, bounds.center.z);
            bool hasTrunk = Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None)
                .Any(x => x.name == "Route tree trunk collider" && HorizontalDistance(x.transform.position, trunkPosition) < 1f);
            if (!hasTrunk)
            {
                var trunk = new GameObject("Route tree trunk collider");
                trunk.transform.position = trunkPosition;
                var capsule = trunk.AddComponent<CapsuleCollider>(); capsule.height = 3f; capsule.radius = Mathf.Clamp(Mathf.Min(bounds.size.x,bounds.size.z) * .05f,.2f,.45f);
                // An independent root avoids nonuniform imported tree scales stretching the capsule across the road.
                trunk.transform.SetParent(GameObject.Find(RootName).transform,true);
                details.Add(collider.name + " trunk=" + trunk.transform.position.ToString("F2") + " radius=" + capsule.radius);
            }
            repaired++;
        }
        int duplicateTrunks = RemoveDuplicateTreeTrunks();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Docs/Validation/Expansion");
        File.AppendAllLines("Docs/Validation/Expansion/tree-collider-repair.txt",new[] { "Executed " + DateTime.UtcNow.ToString("o"), "Disabled oversized alpha-plane colliders near route: " + repaired, "Removed overlapping duplicate route tree trunk capsules: " + duplicateTrunks }.Concat(details));
        Debug.Log("KOMBI_ROUTE_TREE_COLLIDERS_REPAIRED " + repaired + " duplicate trunks removed " + duplicateTrunks);
    }
    static int RemoveDuplicateTreeTrunks()
    {
        var trunks = Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None)
            .Where(x => x.name == "Route tree trunk collider")
            .OrderBy(x => x.transform.position.x).ThenBy(x => x.transform.position.z).ToArray();
        var kept = new List<CapsuleCollider>();
        int removed = 0;
        foreach (var trunk in trunks)
        {
            var duplicate = kept.FirstOrDefault(x => HorizontalDistance(x.transform.position, trunk.transform.position) < 0.9f);
            if (duplicate == null) { kept.Add(trunk); continue; }
            duplicate.radius = Mathf.Max(duplicate.radius, trunk.radius);
            duplicate.height = Mathf.Max(duplicate.height, trunk.height);
            Object.DestroyImmediate(trunk.gameObject);
            removed++;
        }
        return removed;
    }
    [MenuItem("Kombi Shift/Repair Road Sign Placement")]
    public static void RepairRoadSignPlacement()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        int moved = 0;
        foreach (var zone in Object.FindObjectsByType<KombiRoadRuleZone>(FindObjectsSortMode.None))
        {
            var post = zone.transform.Find(zone.rule + " sign post");
            var label = zone.transform.Find("Readable road sign");
            if (post == null || label == null) continue;
            PlaceRoadSign(zone, post, label);
            moved++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("KOMBI_ROADSIDE_SIGNS_REPAIRED " + moved);
    }
    public static void RepairRoadsideSceneIssues()
    {
        RepairRoadSignPlacement();
        RepairRouteTreeColliders();
    }
    public static void Refine()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        Physics.SyncTransforms();
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var director = Object.FindFirstObjectByType<KombiShiftGameDirector>();
        var old = hud.RoutePoints; var points = new List<Transform>();
        for (int i = 0; i < old.Length; i++)
        {
            if (i > 0)
            {
                int divisions = Mathf.CeilToInt(Vector3.Distance(old[i-1].position,old[i].position) / 24f);
                for (int n = 1; n < divisions; n++)
                {
                    var point = new GameObject("Refined route midpoint").transform;
                    point.SetParent(GameObject.Find(RootName).transform); point.position = Ground(Vector3.Lerp(old[i-1].position,old[i].position,n/(float)divisions)); points.Add(point);
                }
            }
            points.Add(old[i]);
        }
        var route = points.ToArray();
        var data = new SerializedObject(director); var stops = data.FindProperty("stops");
        var stations = Enumerable.Range(0,stops.arraySize).Select(i => ((Transform)stops.GetArrayElementAtIndex(i).objectReferenceValue).GetComponent<PassengerStation>()).ToArray();
        hud.Configure(director.Vehicle, route,director);
        foreach (var rival in Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None)) rival.Configure(route,stations,rival.DepartureDelay,rival.Speed);
        foreach (var traffic in Object.FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))
        {
            var serialized = new SerializedObject(traffic); var path = serialized.FindProperty("route");
            int oldIndex = serialized.FindProperty("targetIndex").intValue;
            int index = Array.IndexOf(route,old[Mathf.Clamp(oldIndex,0,old.Length-1)]);
            path.arraySize = route.Length;
            for (int i = 0; i < route.Length; i++) path.GetArrayElementAtIndex(i).objectReferenceValue = route[i];
            serialized.FindProperty("targetIndex").intValue = index; serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        // Directional controls apply to their signed left lane, not legal opposing traffic.
        foreach (var zone in Object.FindObjectsByType<KombiRoadRuleZone>(FindObjectsSortMode.None))
        {
            if (zone.rule != RoadRule.Direction && zone.rule != RoadRule.Stop) continue;
            var box = zone.GetComponent<BoxCollider>();
            if (box.size.x > 4f)
            {
                box.center = new Vector3(-1.55f,0f,0f);
                box.size = new Vector3(3.1f,box.size.y,box.size.z);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        KombiExpansionValidation.Audit(); KombiExpansionValidation.Rules();
    }
    static int Nearest(Transform[] route, Vector3 point) => Enumerable.Range(0,route.Length).OrderBy(i => (route[i].position-point).sqrMagnitude).First();
    static bool SegmentIntersectsBounds(Vector3 start, Vector3 end, Bounds bounds)
    {
        Vector3 direction = end - start;
        float minimum = 0f, maximum = 1f;
        for (int axis = 0; axis < 3; axis++)
        {
            float origin = axis == 0 ? start.x : axis == 1 ? start.y : start.z;
            float delta = axis == 0 ? direction.x : axis == 1 ? direction.y : direction.z;
            float low = axis == 0 ? bounds.min.x : axis == 1 ? bounds.min.y : bounds.min.z;
            float high = axis == 0 ? bounds.max.x : axis == 1 ? bounds.max.y : bounds.max.z;
            if (Mathf.Abs(delta) < .0001f)
            {
                if (origin < low || origin > high) return false;
                continue;
            }
            float enter = (low - origin) / delta, exit = (high - origin) / delta;
            if (enter > exit) (enter, exit) = (exit, enter);
            minimum = Mathf.Max(minimum, enter);
            maximum = Mathf.Min(maximum, exit);
            if (minimum > maximum) return false;
        }
        return true;
    }
    static float HorizontalDistance(Vector3 a, Vector3 b)
        => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    static Vector3 Ground(Vector3 point)
    {
        var hits = Physics.RaycastAll(point + Vector3.up * 2f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
        var valid = hits.Where(x => x.rigidbody == null && x.normal.y > .7f).OrderByDescending(x => x.point.y).ToArray();
        if (valid.Length == 0) throw new Exception("No road ground at " + point);
        return new Vector3(point.x, valid[0].point.y + .025f, point.z);
    }
    static void MakeSign(Transform parent, Vector3 point, Vector3 forward, RoadRule rule, float limit, float length)
    {
        forward.y = 0; forward.Normalize();
        var zone = new GameObject(rule + " zone"); zone.transform.SetParent(parent); zone.transform.SetPositionAndRotation(point + Vector3.up * 1.5f, Quaternion.LookRotation(forward));
        var box = zone.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(rule == RoadRule.NoEntry ? 4f : 7f, 4f, length);
        var behaviour = zone.AddComponent<KombiRoadRuleZone>(); behaviour.rule = rule; behaviour.limit = limit; behaviour.travelDirection = forward;
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.name = rule + " sign post"; post.transform.SetParent(zone.transform);
        post.transform.localScale = new Vector3(.08f,1f,.08f);
        Object.DestroyImmediate(post.GetComponent<Collider>());
        var text = new GameObject("Readable road sign"); text.transform.SetParent(post.transform, false); text.transform.localScale = Vector3.one;
        text.transform.SetParent(zone.transform, true); text.transform.localScale = Vector3.one;
        var label = text.AddComponent<TextMesh>(); label.text = rule == RoadRule.SpeedLimit ? limit.ToString("0") + " km/h" : rule == RoadRule.Hump ? "HUMP 20" : rule.ToString().ToUpperInvariant();
        label.fontSize = 48; label.characterSize = .09f; label.anchor = TextAnchor.MiddleCenter; label.color = Color.yellow;
        PlaceRoadSign(behaviour, post.transform, text.transform);
    }
    static void PlaceRoadSign(KombiRoadRuleZone zone, Transform post, Transform label)
    {
        Vector3 forward = zone.travelDirection; forward.y = 0f; forward.Normalize();
        Vector3 ground = zone.transform.position - Vector3.up * 1.5f;
        Vector3 position = ground + RivalKombiAI.LeftOffset(forward, 6.5f) + Vector3.up;
        post.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        label.SetPositionAndRotation(position + Vector3.up * 1.3f, Quaternion.LookRotation(forward));
        label.localScale = Vector3.one;
    }
    static void MakeHump(Transform parent, Vector3 point, Vector3 forward)
    {
        // A shallow 4 m ramp profile: maximum height 12 cm, with no vertical step.
        var go = new GameObject("Physical speed hump"); go.transform.SetParent(parent); go.transform.SetPositionAndRotation(point, Quaternion.LookRotation(forward));
        float[] z = { -2f,-1f,0f,1f,2f }; float[] y = { 0f,.07f,.12f,.07f,0f };
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 0; i < z.Length; i++) { vertices.Add(new Vector3(-3.4f,y[i],z[i])); vertices.Add(new Vector3(3.4f,y[i],z[i])); }
        for (int i=0;i<4;i++) { int k=i*2; triangles.AddRange(new[] { k,k+2,k+1,k+1,k+2,k+3 }); }
        var mesh = new Mesh { name = "Shared shallow hump profile" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
        const string path = "Assets/KombiShift2/Art/Generated/ShallowHump.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh,path); saved = mesh; } else Object.DestroyImmediate(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = saved;
        var renderer = go.AddComponent<MeshRenderer>();
        const string matPath = "Assets/KombiShift2/Art/Generated/HumpWarning.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null) { material = new Material(Shader.Find("Standard")) { color = new Color(.8f,.55f,.08f) }; AssetDatabase.CreateAsset(material, matPath); }
        renderer.sharedMaterial = material; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<MeshCollider>().sharedMesh = saved;
    }
}
