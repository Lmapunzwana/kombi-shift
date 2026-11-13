using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BezierSolution;
using KombiShift2.City;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class KombiShiftExpansionUpgrade
{
    const string RootName = "Shift Expansion 2026";
    const string RoadMarkingRootName = "Road carriage markings";
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
            int index = rivalIndex == 0 ? 34 : 75;
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
        BuildOrderedRoadRules(root, route, stations);
        MakeRoadCarriageMarkings(root, route);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        KombiPassengerSeatingLaneRepair.Apply();
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
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before repairing road signs.");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the current scene before repairing road signs.");
        if (scene.path != KombiProjectWorkbench.Shift) scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        int moved = RepositionRoadSignsInOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("KOMBI_ROADSIDE_SIGNS_REPAIRED " + moved);
    }
    public static int RepositionRoadSignsInOpenScene()
    {
        int moved = 0;
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        if (hud == null) throw new InvalidOperationException("The saved driving scene is missing its navigation route.");
        var network = Object.FindFirstObjectByType<CityRoadNetwork>();
        if (network == null) throw new InvalidOperationException("The saved driving scene is missing its city road network.");
        network.Load(true);
        Physics.SyncTransforms();
        foreach (var zone in Object.FindObjectsByType<KombiRoadRuleZone>(FindObjectsSortMode.None)
                     .OrderBy(x => Nearest(hud.RoutePoints, x.transform.position)))
        {
            var post = zone.transform.Find(zone.rule + " sign post");
            var label = zone.transform.Find("Readable road sign");
            if (post == null || label == null) continue;
            PlaceRoadSign(zone, post, label, network);
            moved++;
        }
        return moved;
    }
    public static void RepairRoadsideSceneIssues()
    {
        RebuildOrderedRoadRules();
        RepairRouteTreeColliders();
        RefreshRoadCarriageMarkings();
        RemovePhysicalRouteCenterLines();
    }
    public static void RebuildOrderedRoadRules()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var root = GameObject.Find(RootName);
        if (root == null) throw new Exception("Shift expansion root is missing; cannot rebuild road signs.");
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var director = Object.FindFirstObjectByType<KombiShiftGameDirector>();
        var stops = new SerializedObject(director).FindProperty("stops");
        var stations = Enumerable.Range(0, stops.arraySize)
            .Select(i => stops.GetArrayElementAtIndex(i).objectReferenceValue as Transform)
            .Where(x => x != null)
            .Select(x => x.GetComponent<PassengerStation>())
            .Where(x => x != null)
            .ToArray();
        BuildOrderedRoadRules(root.transform, hud.RoutePoints, stations);
        foreach (var zone in Object.FindObjectsByType<KombiRoadRuleZone>(FindObjectsSortMode.None))
            if ((int)zone.rule == 2) Object.DestroyImmediate(zone.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        int humps = Object.FindObjectsByType<KombiRoadRuleZone>(FindObjectsSortMode.None).Count(x => x.rule == RoadRule.Hump);
        Debug.Log("KOMBI_ORDERED_ROAD_RULES_REBUILT stations=" + stations.Length + " humps=" + humps + " giveWay=0");
    }
    static void BuildOrderedRoadRules(Transform root, Transform[] route, PassengerStation[] stations)
    {
        if (route == null || route.Length < 4) throw new Exception("A complete route is required to place roadside rules.");
        foreach (var zone in root.GetComponentsInChildren<KombiRoadRuleZone>(true)) Object.DestroyImmediate(zone.gameObject);
        foreach (var child in root.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Physical speed hump").ToArray())
            Object.DestroyImmediate(child.gameObject);

        var orderedStations = stations
            .Where(x => x != null && x.ServicePoint != null)
            .Select(x => (station: x, index: Nearest(route, x.ServicePoint.position)))
            .OrderBy(x => x.index)
            .ToArray();
        var routeDistances = RouteDistances(route);
        var crossingIndices = orderedStations.Where(x => !x.station.Terminal)
            .Select(x => RouteIndexBehind(route, x.index, 18f)).ToArray();
        var usedHumps = new List<int>();
        var signs = new List<(int routeIndex, Action create)>();

        int[] speedSignStations = orderedStations.Length > 2
            ? new[] { 0, orderedStations.Length / 2, orderedStations.Length - 1 }
            : Enumerable.Range(0, orderedStations.Length).ToArray();
        foreach (int stationIndex in speedSignStations.Distinct())
        {
            int index = orderedStations[stationIndex].index;
            Vector3 forward = RouteForward(route, index);
            float limit = speedSignStations.Length > 1 && stationIndex == speedSignStations[1] ? 50f : 30f;
            signs.Add((index, () => MakeSign(root, route[index].position, forward, RoadRule.SpeedLimit, limit, 35f)));
        }

        foreach (var entry in orderedStations)
        {
            int index = entry.index;
            if (entry.station.Terminal) continue;

            int crossing = RouteIndexBehind(route, index, 18f);
            Vector3 crossingForward = RouteForward(route, crossing);
            signs.Add((crossing, () => MakeSign(root, route[crossing].position, crossingForward, RoadRule.Crossing, 20f, 12f, false)));

            int hump = FindStraightHumpIndex(route, index, crossingIndices, usedHumps, routeDistances);
            if (hump >= 0)
            {
                usedHumps.Add(hump);
                Vector3 humpForward = RouteForward(route, hump);
                signs.Add((hump, () =>
                {
                    Vector3 point = route[hump].position;
                    MakeSign(root, point, humpForward, RoadRule.Hump, 20f, 4f);
                    MakeHump(root, point, humpForward);
                }));
            }

            int noOvertaking = RouteIndexBehind(route, index, 25f);
            Vector3 noOvertakingForward = RouteForward(route, noOvertaking);
            signs.Add((noOvertaking, () => MakeSign(root, route[noOvertaking].position, noOvertakingForward, RoadRule.NoOvertaking, 30f, 30f, false)));
        }

        foreach (var sign in signs.OrderBy(x => x.routeIndex)) sign.create();
    }
    static float[] RouteDistances(Transform[] route)
    {
        var distances = new float[route.Length];
        for (int i = 1; i < route.Length; i++)
            distances[i] = distances[i - 1] + HorizontalDistance(route[i - 1].position, route[i].position);
        return distances;
    }
    static int RouteIndexBehind(Transform[] route, int index, float distance)
    {
        float remaining = distance;
        while (index > 0 && remaining > 0f)
        {
            remaining -= HorizontalDistance(route[index - 1].position, route[index].position);
            index--;
        }
        return index;
    }
    static Vector3 RouteForward(Transform[] route, int index)
    {
        Vector3 forward = route[Mathf.Min(route.Length - 1, index + 1)].position - route[Mathf.Max(0, index - 1)].position;
        forward.y = 0f;
        return forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
    }
    static int FindStraightHumpIndex(Transform[] route, int stationIndex, int[] crossingIndices, List<int> usedHumps, float[] routeDistances)
    {
        float stationDistance = routeDistances[stationIndex];
        var candidates = Enumerable.Range(2, route.Length - 4)
            .Where(i =>
            {
                float distance = stationDistance - routeDistances[i];
                if (distance < 45f || distance > 100f) return false;
                Vector3 incoming = route[i].position - route[i - 1].position;
                Vector3 outgoing = route[i + 1].position - route[i].position;
                incoming.y = 0f; outgoing.y = 0f;
                if (incoming.sqrMagnitude < .01f || outgoing.sqrMagnitude < .01f || Vector3.Angle(incoming, outgoing) > 12f) return false;
                if (crossingIndices.Any(c => Mathf.Abs(routeDistances[c] - routeDistances[i]) < 22f)) return false;
                if (usedHumps.Any(h => Mathf.Abs(routeDistances[h] - routeDistances[i]) < 40f)) return false;
                return true;
            })
            .OrderBy(i => Mathf.Abs((stationDistance - routeDistances[i]) - 70f))
            .ToArray();
        return candidates.Length == 0 ? -1 : candidates[0];
    }
    [MenuItem("Kombi Shift/Refresh Road Carriage Markings")]
    public static void RefreshRoadCarriageMarkings()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var root = GameObject.Find(RootName);
        if (root == null) throw new Exception("Shift expansion root is missing; cannot place road markings.");
        MakeRoadCarriageMarkings(root.transform, hud.RoutePoints);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("KOMBI_ROAD_MARKINGS_REFRESHED " + hud.RoutePoints.Length);
    }
    static void RemovePhysicalRouteCenterLines()
    {
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        int removed = 0;
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!renderer.gameObject.name.StartsWith("Centre Line bz", StringComparison.Ordinal)) continue;
            renderer.enabled = false;
            removed++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("KOMBI_PHYSICAL_ROUTE_LINES_REMOVED " + removed);
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
    static void MakeRoadCarriageMarkings(Transform parent, Transform[] route)
    {
        var previous = parent.Find(RoadMarkingRootName);
        if (previous != null)
        {
            foreach (var filter in previous.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null && !AssetDatabase.Contains(filter.sharedMesh)) Object.DestroyImmediate(filter.sharedMesh);
            Object.DestroyImmediate(previous.gameObject);
        }
        var group = new GameObject(RoadMarkingRootName);
        group.transform.SetParent(parent, false);
        var restricted = Object.FindObjectsByType<KombiRoadRuleZone>(FindObjectsSortMode.None)
            .Where(x => x.rule == RoadRule.NoOvertaking).ToArray();
        CreateMarking(group.transform, "Road edge lines", BuildEdgeMarkings(route), RoadMarkingMaterial("RoadMarkingWhite", Color.white));
        CreateMarking(group.transform, "Dashed centre line", BuildDashedCentreLine(route, restricted), RoadMarkingMaterial("RoadMarkingWhite", Color.white));
        CreateMarking(group.transform, "No-overtaking double line", BuildRestrictedCentreLine(route, restricted), RoadMarkingMaterial("RoadMarkingWhite", Color.white));
    }
    static Mesh BuildEdgeMarkings(Transform[] route)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 1; i < route.Length; i++)
        {
            AddRoadStrip(vertices, triangles, route[i - 1].position, route[i].position, -3.05f, .12f);
            AddRoadStrip(vertices, triangles, route[i - 1].position, route[i].position, 3.05f, .12f);
        }
        return RoadMarkingMesh("Road edge markings mesh", vertices, triangles);
    }
    static Mesh BuildDashedCentreLine(Transform[] route, KombiRoadRuleZone[] restricted)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        const float dash = 4f, cycle = 8f;
        float distanceAlong = 0f;
        for (int i = 1; i < route.Length; i++)
        {
            Vector3 start = route[i - 1].position, end = route[i].position;
            float length = Vector3.Distance(new Vector3(start.x,0f,start.z), new Vector3(end.x,0f,end.z));
            if (length < .01f) continue;
            float cursor = 0f;
            while (cursor < length)
            {
                float phase = (distanceAlong + cursor) % cycle;
                bool draw = phase < dash;
                float step = Mathf.Min(length - cursor, draw ? dash - phase : cycle - phase);
                if (step < .001f) break;
                if (draw)
                {
                    Vector3 a = Vector3.Lerp(start, end, cursor / length);
                    Vector3 b = Vector3.Lerp(start, end, (cursor + step) / length);
                    if (!IsRestricted(a + (b - a) * .5f, restricted)) AddRoadStrip(vertices, triangles, a, b, 0f, .12f);
                }
                cursor += step;
            }
            distanceAlong += length;
        }
        return RoadMarkingMesh("Dashed road centre line mesh", vertices, triangles);
    }
    static Mesh BuildRestrictedCentreLine(Transform[] route, KombiRoadRuleZone[] restricted)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 1; i < route.Length; i++)
        {
            Vector3 start = route[i - 1].position, end = route[i].position;
            if (!IsRestricted((start + end) * .5f, restricted)) continue;
            AddRoadStrip(vertices, triangles, start, end, -.16f, .1f);
            AddRoadStrip(vertices, triangles, start, end, .16f, .1f);
        }
        return RoadMarkingMesh("Solid no-overtaking centre line mesh", vertices, triangles);
    }
    static bool IsRestricted(Vector3 point, KombiRoadRuleZone[] zones)
    {
        foreach (var zone in zones)
        {
            var box = zone.GetComponent<BoxCollider>();
            if (box == null) continue;
            Vector3 local = zone.transform.InverseTransformPoint(point) - box.center;
            if (Mathf.Abs(local.x) <= box.size.x * .5f && Mathf.Abs(local.y) <= box.size.y * .5f &&
                Mathf.Abs(local.z) <= box.size.z * .5f) return true;
        }
        return false;
    }
    static void AddRoadStrip(List<Vector3> vertices, List<int> triangles, Vector3 start, Vector3 end, float offset, float width)
    {
        Vector3 direction = end - start; direction.y = 0f;
        if (direction.sqrMagnitude < .0001f) return;
        Vector3 lateral = Vector3.Cross(Vector3.up, direction.normalized);
        Vector3 lift = Vector3.up * .04f;
        Vector3 a = start + lateral * (offset - width * .5f) + lift;
        Vector3 b = end + lateral * (offset - width * .5f) + lift;
        Vector3 c = start + lateral * (offset + width * .5f) + lift;
        Vector3 d = end + lateral * (offset + width * .5f) + lift;
        int first = vertices.Count;
        vertices.AddRange(new[] { a, b, c, d });
        triangles.AddRange(new[] { first, first + 1, first + 2, first + 2, first + 1, first + 3 });
    }
    static Mesh RoadMarkingMesh(string name, List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
    static Material RoadMarkingMaterial(string name, Color color)
    {
        string path = "Assets/KombiShift2/Art/Generated/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) throw new Exception("No supported shader found for road carriage markings.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }
    static void CreateMarking(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
    static Vector3 Ground(Vector3 point)
    {
        var hits = Physics.RaycastAll(point + Vector3.up * 2f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
        var valid = hits.Where(x => x.rigidbody == null && x.normal.y > .7f).OrderByDescending(x => x.point.y).ToArray();
        if (valid.Length == 0) throw new Exception("No road ground at " + point);
        return new Vector3(point.x, valid[0].point.y + .025f, point.z);
    }
    static void MakeSign(Transform parent, Vector3 point, Vector3 forward, RoadRule rule, float limit, float length, bool showSign = true)
    {
        forward.y = 0; forward.Normalize();
        var zone = new GameObject(rule + " zone"); zone.transform.SetParent(parent); zone.transform.SetPositionAndRotation(point + Vector3.up * 1.5f, Quaternion.LookRotation(forward));
        var box = zone.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(rule == RoadRule.NoEntry ? 4f : 7f, 4f, length);
        var behaviour = zone.AddComponent<KombiRoadRuleZone>(); behaviour.rule = rule; behaviour.limit = limit; behaviour.travelDirection = forward;
        if (!showSign) return;
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.name = rule + " sign post"; post.transform.SetParent(zone.transform);
        post.transform.localScale = new Vector3(.08f,1f,.08f);
        Object.DestroyImmediate(post.GetComponent<Collider>());
        var text = new GameObject("Readable road sign"); text.transform.SetParent(post.transform, false); text.transform.localScale = Vector3.one;
        text.transform.SetParent(zone.transform, true); text.transform.localScale = Vector3.one;
        var label = text.AddComponent<TextMesh>(); label.text = RoadSignText(rule, limit);
        label.fontSize = 48; label.characterSize = .09f; label.anchor = TextAnchor.MiddleCenter; label.color = Color.yellow;
        var network = Object.FindFirstObjectByType<CityRoadNetwork>();
        if (network == null) throw new InvalidOperationException("The city road network is required to place roadside signs.");
        network.Load();
        PlaceRoadSign(behaviour, post.transform, text.transform, network);
    }
    static string RoadSignText(RoadRule rule, float limit)
    {
        return rule switch
        {
            RoadRule.SpeedLimit => limit.ToString("0") + " km/h",
            RoadRule.Stop => "STOP",
            RoadRule.NoEntry => "NO ENTRY",
            RoadRule.NoOvertaking => "NO OVERTAKING",
            RoadRule.Crossing => "CROSSING",
            RoadRule.Roadworks => "ROADWORKS",
            RoadRule.Hump => "HUMP 20",
            RoadRule.Direction => "KEEP LEFT",
            _ => rule.ToString().ToUpperInvariant()
        };
    }
    static void PlaceRoadSign(KombiRoadRuleZone zone, Transform post, Transform label, CityRoadNetwork network)
    {
        Vector3 forward = zone.travelDirection; forward.y = 0f; forward.Normalize();
        Vector3 ground = zone.transform.position - Vector3.up * 1.5f;
        float approach = zone.rule switch
        {
            RoadRule.Hump => 15f,
            RoadRule.Crossing => 20f,
            RoadRule.NoOvertaking => 30f,
            RoadRule.SpeedLimit => 12f,
            RoadRule.Stop => 15f,
            RoadRule.Roadworks => 20f,
            _ => 0f
        };
        int roadIndex = network.Nearest(ground, 0);
        if (roadIndex < 0) throw new InvalidOperationException("No city road node was found for the " + zone.rule + " sign.");
        var road = network.Nodes[roadIndex];
        Vector3 lanePoint = ground - forward * approach;
        if (!CityRoadsidePlacement.TryFind(lanePoint, -forward, road.width, 1, out var roadside, out bool needsFooting))
            throw new InvalidOperationException("No supported roadside surface was found for the " + zone.rule + " sign.");
        if (needsFooting) CityRoadsidePlacement.CreateFooting(post.parent, roadside);
        Quaternion facing = Quaternion.LookRotation(-forward);
        post.SetPositionAndRotation(roadside + Vector3.up, facing);
        label.SetPositionAndRotation(roadside + Vector3.up * 2.3f, facing);
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
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/KombiShift2/Generated/City/City Asphalt.mat");
        if (material == null) throw new Exception("City asphalt material is missing; cannot match the speed hump to the road.");
        renderer.sharedMaterial = material; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<MeshCollider>().sharedMesh = saved;
    }
}
