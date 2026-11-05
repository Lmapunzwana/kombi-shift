#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KombiShift2.Editor
{
    public static class ImportedVehicleAudit
    {
        private static readonly string[] VehiclePaths =
        {
            "Assets/KombiShift2/ThirdParty/Hiace2005/source/Hiace.glb",
            "Assets/KombiShift2/ThirdParty/GenericPassengerCars/source/fab.fbx"
        };

        [MenuItem("Kombi Shift/Assets/Audit Imported Vehicles")]
        public static void AuditImportedVehicles()
        {
            foreach (string path in VehiclePaths)
            {
                Audit(path);
            }
        }

        private static void Audit(string path)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
            {
                Debug.LogError($"KOMBI_VEHICLE_AUDIT Could not load {path}");
                return;
            }

            GameObject instance = Object.Instantiate(source);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                MeshFilter[] filters = instance.GetComponentsInChildren<MeshFilter>(true);
                Bounds bounds = CalculateBounds(renderers);
                long vertices = 0;
                long triangles = 0;
                foreach (MeshFilter filter in filters)
                {
                    if (filter.sharedMesh == null) continue;
                    vertices += filter.sharedMesh.vertexCount;
                    triangles += filter.sharedMesh.triangles.Length / 3;
                }

                StringBuilder report = new();
                report.AppendLine($"KOMBI_VEHICLE_AUDIT {path}");
                report.AppendLine($"Root={source.name} Renderers={renderers.Length} Meshes={filters.Length} Vertices={vertices} Triangles={triangles}");
                report.AppendLine($"Bounds center={Format(bounds.center)} size={Format(bounds.size)} min={Format(bounds.min)} max={Format(bounds.max)}");
                AppendHierarchy(instance.transform, instance.transform, report);
                Debug.Log(report.ToString());
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AppendHierarchy(Transform root, Transform current, StringBuilder report)
        {
            MeshFilter filter = current.GetComponent<MeshFilter>();
            Renderer renderer = current.GetComponent<Renderer>();
            string relativePath = AnimationUtility.CalculateTransformPath(current, root);
            if (string.IsNullOrEmpty(relativePath)) relativePath = "<root>";

            if (filter != null || renderer != null || ContainsVehiclePartName(current.name))
            {
                string mesh = filter != null && filter.sharedMesh != null
                    ? $" mesh={filter.sharedMesh.name} vertices={filter.sharedMesh.vertexCount}"
                    : string.Empty;
                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    string rootBone = skinned.rootBone != null ? skinned.rootBone.name : "<null>";
                    string bones = string.Join(", ", System.Array.ConvertAll(skinned.bones, bone => bone != null ? bone.name : "<null>"));
                    mesh = $" skinnedMesh={skinned.sharedMesh.name} vertices={skinned.sharedMesh.vertexCount} localBoundsCenter={Format(skinned.localBounds.center)} localBoundsSize={Format(skinned.localBounds.size)} rootBone={rootBone} bones=[{bones}]";
                }
                string materialNames = string.Empty;
                if (renderer != null)
                {
                    string[] names = new string[renderer.sharedMaterials.Length];
                    for (int i = 0; i < names.Length; i++)
                    {
                        names[i] = renderer.sharedMaterials[i] != null ? renderer.sharedMaterials[i].name : "<null>";
                    }
                    materialNames = $" materials=[{string.Join(", ", names)}]";
                }
                report.AppendLine($"  {relativePath} pos={Format(current.localPosition)} rot={Format(current.localEulerAngles)} scale={Format(current.localScale)}{mesh}{materialNames}");
            }

            foreach (Transform child in current)
            {
                AppendHierarchy(root, child, report);
            }
        }

        private static bool ContainsVehiclePartName(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("wheel") || lower.Contains("tire") || lower.Contains("body") || lower.Contains("door");
        }

        private static Bounds CalculateBounds(Renderer[] renderers)
        {
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static string Format(Vector3 value) => $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
    }
}
#endif
