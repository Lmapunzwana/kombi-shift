using System.Collections.Generic;
using KombiShift2.City;
using UnityEngine;

public static class CityRoadsidePlacement
{
    static MeshFilter[] visualGroundMeshes;
    static Terrain[] visualGroundTerrains;
    static int visualGroundMeshCandidates, visualGroundMeshHits, visualGroundTerrainHits;

    public static void RefreshVisualGroundCache()
    {
        visualGroundMeshes = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        visualGroundTerrains = Terrain.activeTerrains;
    }

    public static bool TryFind(Vector3 lanePoint, Vector3 approachDirection, float roadWidth, int side, out Vector3 position,
        out bool needsFooting, float maxAlong = 8f, float extraLateral = 4f)
    {
        position = default;
        needsFooting = false;
        approachDirection.y = 0f;
        if (approachDirection.sqrMagnitude < .01f || roadWidth <= 0f || side == 0) return false;
        approachDirection.Normalize();
        Vector3 roadside = Vector3.Cross(Vector3.up, approachDirection) * Mathf.Sign(side);
        const float firstOffset = .8f;
        float lastOffset = Mathf.Max(firstOffset + 4f, roadWidth * .5f + extraLateral);
        int nonRoadHits = 0, roadHits = 0;
        float nearestGroundHeightDelta = float.MaxValue;
        string nearestGroundCollider = "none";
        visualGroundMeshCandidates = visualGroundMeshHits = visualGroundTerrainHits = 0;
        var visualGroundSamples = new List<Vector3>();
        var footingSamples = new List<Vector3>();

        for (float along = 0f; along <= maxAlong; along += 2f)
        {
            for (float offset = firstOffset; offset <= lastOffset; offset += .5f)
            {
                Vector3 sample = lanePoint + approachDirection * along + roadside * offset;
                var hits = Physics.RaycastAll(sample + Vector3.up * 2f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
                float roadHeight = float.NegativeInfinity;
                RaycastHit ground = default;
                float groundDistance = float.MaxValue;
                foreach (var hit in hits)
                {
                    if (hit.rigidbody != null || hit.normal.y < .65f || IsExistingRoadsideFixture(hit.collider)) continue;
                    if (IsRoad(hit.collider))
                    {
                        roadHits++;
                        roadHeight = Mathf.Max(roadHeight, hit.point.y);
                        continue;
                    }

                    nonRoadHits++;
                    float distance = Mathf.Abs(hit.point.y - lanePoint.y);
                    if (distance < nearestGroundHeightDelta)
                    {
                        nearestGroundHeightDelta = distance;
                        nearestGroundCollider = hit.collider.name;
                    }
                    if (distance > 1.25f) continue;
                    if (distance < groundDistance)
                    {
                        ground = hit;
                        groundDistance = distance;
                    }
                }

                if (ground.collider == null && float.IsNegativeInfinity(roadHeight))
                {
                    visualGroundSamples.Add(sample);
                    if (offset >= roadWidth * .5f + .5f) footingSamples.Add(sample);
                }
                if (ground.collider == null || !float.IsNegativeInfinity(roadHeight) && Mathf.Abs(roadHeight - ground.point.y) < .6f) continue;
                position = new Vector3(sample.x, ground.point.y, sample.z);
                return true;
            }
        }

        foreach (var sample in visualGroundSamples)
        {
            if (!TryFindVisualGround(sample, lanePoint.y, 1.25f, out position)) continue;
            return true;
        }

        if (roadHits > 0 && footingSamples.Count > 0)
        {
            var sample = footingSamples[0];
            position = new Vector3(sample.x, lanePoint.y, sample.z);
            needsFooting = true;
            return true;
        }

        Debug.LogWarning("No roadside support near "+lanePoint+"; road hits="+roadHits+", ground hits="+nonRoadHits+
            ", nearest ground height delta="+nearestGroundHeightDelta+" on "+nearestGroundCollider+
            ", visual mesh candidates/hits="+visualGroundMeshCandidates+"/"+visualGroundMeshHits+
            ", terrain hits="+visualGroundTerrainHits+".");
        return false;
    }

    public static void CreateFooting(Transform parent, Vector3 position)
    {
        var footing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        footing.name = "Roadside post footing";
        footing.transform.SetParent(parent, false);
        footing.transform.position = position + Vector3.up * .06f;
        footing.transform.localScale = new Vector3(.9f, .12f, .9f);
    }

    public static bool HasVisualGround(Vector3 position, float verticalTolerance)
    {
        return TryFindVisualGround(position, position.y, verticalTolerance, out _);
    }

    static bool TryFindVisualGround(Vector3 sample, float referenceHeight, float verticalTolerance, out Vector3 ground)
    {
        ground = default;
        if (visualGroundMeshes == null || visualGroundTerrains == null) RefreshVisualGroundCache();

        foreach (var terrain in visualGroundTerrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (sample.x < origin.x || sample.x > origin.x + size.x || sample.z < origin.z || sample.z > origin.z + size.z) continue;
            float height = terrain.SampleHeight(sample) + origin.y;
            if (Mathf.Abs(height - referenceHeight) > verticalTolerance) continue;
            ground = new Vector3(sample.x, height, sample.z);
            visualGroundTerrainHits++;
            return true;
        }
        foreach (var filter in visualGroundMeshes)
        {
            if (filter == null || filter.sharedMesh == null || !filter.gameObject.activeInHierarchy ||
                filter.GetComponent<Collider>() != null || filter.GetComponentInParent<Rigidbody>() != null) continue;
            var renderer = filter.GetComponent<Renderer>();
            if (renderer == null || !renderer.enabled) continue;
            var bounds = renderer.bounds;
            if (sample.x < bounds.min.x || sample.x > bounds.max.x || sample.z < bounds.min.z || sample.z > bounds.max.z ||
                bounds.max.y < referenceHeight - verticalTolerance || bounds.min.y > referenceHeight + verticalTolerance) continue;
            visualGroundMeshCandidates++;

            var meshCollider = filter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = filter.sharedMesh;
            Physics.SyncTransforms();
            try
            {
                foreach (var hit in Physics.RaycastAll(sample + Vector3.up * 2f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != meshCollider || hit.normal.y < .65f || IsRoad(hit.collider) ||
                        Mathf.Abs(hit.point.y - referenceHeight) > verticalTolerance) continue;
                    ground = hit.point;
                    visualGroundMeshHits++;
                    return true;
                }
            }
            finally
            {
                Object.DestroyImmediate(meshCollider);
                Physics.SyncTransforms();
            }
        }

        return false;
    }

    static bool IsRoad(Collider collider)
    {
        if (collider.GetComponentInParent<CityRoadSurface>() != null) return true;
        var renderer = collider.GetComponent<Renderer>() ?? collider.GetComponentInParent<Renderer>();
        return renderer != null && renderer.sharedMaterials.Length > 0 &&
            System.Array.Exists(renderer.sharedMaterials, material =>
                material != null && (material.name.Contains("Asphalt") || material.shader != null && material.shader.name == "Kombi Shift/Road Surface"));
    }

    static bool IsExistingRoadsideFixture(Collider collider)
    {
        return collider.GetComponentInParent<KombiShift2.City.CityIntersection>() != null ||
            collider.GetComponentInParent<KombiShift2.Gameplay.KombiRoadRuleZone>() != null ||
            collider.name == "Roadside post footing";
    }
}
