#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KombiShift2.Editor
{
    public static class TrafficVehiclePreparationTool
    {
        private const string SourcePath = "Assets/KombiShift2/ThirdParty/GenericPassengerCars/source/fab.fbx";
        private const string OutputFolder = "Assets/KombiShift2/Art/Vehicles/Traffic";

        private static readonly string[] BodyNames =
        {
            "Compact Body",
            "Coupe Body",
            "Hatchback Body",
            "minivan body",
            "Offroad Body",
            "Pickup Body",
            "Sedan Body",
            "Sport body",
            "SUV Body",
            "Wagon Body"
        };

        [MenuItem("Kombi Shift/Assets/Prepare Traffic Vehicle Pack")]
        public static void PrepareTrafficVehiclePack()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null)
            {
                Debug.LogError($"KOMBI_TRAFFIC Could not load {SourcePath}.");
                return;
            }

            EnsureFolder(OutputFolder);
            int prepared = 0;
            foreach (string bodyName in BodyNames)
            {
                if (PrepareVehicle(source, bodyName)) prepared++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidatePreparedPrefabs();
            Debug.Log($"KOMBI_TRAFFIC Prepared {prepared}/{BodyNames.Length} standalone traffic vehicle prefabs in {OutputFolder}.");
        }

        private static void ValidatePreparedPrefabs()
        {
            foreach (string bodyName in BodyNames)
            {
                string path = $"{OutputFolder}/{SanitizeName(bodyName)}_Traffic.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"KOMBI_TRAFFIC_VALIDATE Missing prefab {path}.");
                    continue;
                }

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                int texturedMaterials = renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Distinct()
                    .Count(material => material.mainTexture != null);
                int wheelCount = prefab.transform.Cast<Transform>()
                    .Count(child => child.name.StartsWith("Wheel_", StringComparison.OrdinalIgnoreCase));
                Bounds bounds = CalculateLocalBounds(prefab);
                bool valid = prefab.GetComponent<BoxCollider>() != null
                    && prefab.GetComponent<Rigidbody>() != null
                    && wheelCount == 4
                    && bounds.size.z >= bounds.size.x;

                string message = $"KOMBI_TRAFFIC_VALIDATE {prefab.name}: valid={valid}, renderers={renderers.Length}, wheels={wheelCount}, texturedMaterials={texturedMaterials}, size={bounds.size}.";
                if (valid) Debug.Log(message);
                else Debug.LogError(message);
            }
        }

        private static bool PrepareVehicle(GameObject source, string bodyName)
        {
            GameObject root = Object.Instantiate(source);
            root.name = SanitizeName(bodyName) + "_Traffic";
            root.hideFlags = HideFlags.None;

            Transform body = FindDirectChild(root.transform, bodyName);
            if (body == null)
            {
                Debug.LogError($"KOMBI_TRAFFIC Missing body object '{bodyName}'.");
                Object.DestroyImmediate(root);
                return false;
            }

            List<Transform> wheels = root.transform.Cast<Transform>()
                .Where(child => child.name.StartsWith("Wheel_", StringComparison.OrdinalIgnoreCase))
                .OrderBy(child => HorizontalDistanceSquared(child.localPosition, body.localPosition))
                .Take(4)
                .ToList();

            if (wheels.Count != 4)
            {
                Debug.LogError($"KOMBI_TRAFFIC Expected four wheels near '{bodyName}', found {wheels.Count}.");
                Object.DestroyImmediate(root);
                return false;
            }

            HashSet<Transform> keep = new(wheels) { body };
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
            {
                if (!keep.Contains(child)) Object.DestroyImmediate(child.gameObject);
            }

            Vector3 sourceCentre = body.localPosition;
            float sourceYaw = body.localEulerAngles.y;
            Quaternion normalization = Quaternion.Euler(0f, -sourceYaw, 0f);
            foreach (Transform child in root.transform)
            {
                child.localPosition = normalization * (child.localPosition - sourceCentre);
                child.localRotation = normalization * child.localRotation;
            }

            Bounds bounds = CalculateLocalBounds(root);
            if (bounds.size.x > bounds.size.z * 1.2f)
            {
                Quaternion axisCorrection = Quaternion.Euler(0f, 90f, 0f);
                foreach (Transform child in root.transform)
                {
                    child.localPosition = axisCorrection * child.localPosition;
                    child.localRotation = axisCorrection * child.localRotation;
                }
                bounds = CalculateLocalBounds(root);
            }

            float groundOffset = -bounds.min.y;
            foreach (Transform child in root.transform)
            {
                child.localPosition += Vector3.up * groundOffset;
            }
            bounds.center += Vector3.up * groundOffset;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(bounds.center.x, bounds.center.y + bounds.size.y * 0.04f, bounds.center.z);
            collider.size = new Vector3(bounds.size.x * 0.92f, bounds.size.y * 0.84f, bounds.size.z * 0.92f);

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 1450f;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            string prefabPath = $"{OutputFolder}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            string wheelList = string.Join(", ", wheels.Select(wheel => wheel.name));
            Debug.Log($"KOMBI_TRAFFIC Prepared {prefabPath}; size={bounds.size}; wheels=[{wheelList}].");
            Object.DestroyImmediate(root);
            return true;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
            }
            return null;
        }

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        private static Bounds CalculateLocalBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.up * 0.5f, Vector3.one);

            Bounds bounds = new(root.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Vector3 min = root.transform.InverseTransformPoint(renderer.bounds.min);
                Vector3 max = root.transform.InverseTransformPoint(renderer.bounds.max);
                bounds.Encapsulate(min);
                bounds.Encapsulate(max);
            }
            return bounds;
        }

        private static string SanitizeName(string bodyName)
        {
            return bodyName.Replace(" Body", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path[..split];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(split + 1)..]);
        }
    }
}
#endif
