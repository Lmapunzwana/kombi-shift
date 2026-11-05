#if UNITY_EDITOR
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEngine;

namespace KombiShift2.Editor
{
    public static class VehicleAssetPreparationTool
    {
        private const string OutputFolder = "Assets/KombiShift2/Art/Vehicles/Prepared";

        [MenuItem("Kombi Shift/Assets/Prepare Selected Vehicle Model", true)]
        private static bool ValidatePrepare() => Selection.activeObject is GameObject;

        [MenuItem("Kombi Shift/Assets/Prepare Selected Vehicle Model")]
        private static void Prepare()
        {
            GameObject source = Selection.activeObject as GameObject;
            if (source == null) return;
            EnsureOutputFolder();

            GameObject root = new(source.name + "_Gameplay");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            Bounds bounds = CalculateLocalBounds(root, visual);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 2300f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = bounds.center;
            bodyCollider.size = new Vector3(bounds.size.x * 0.92f, bounds.size.y * 0.88f, bounds.size.z * 0.9f);

            float radius = Mathf.Clamp(bounds.size.y * 0.17f, 0.28f, 0.52f);
            float halfTrack = bounds.extents.x * 0.82f;
            float frontZ = bounds.center.z + bounds.extents.z * 0.64f;
            float rearZ = bounds.center.z - bounds.extents.z * 0.64f;
            float wheelY = bounds.min.y + radius;

            WheelCollider frontLeft = CreateWheel(root.transform, "WheelCollider_FL", new Vector3(-halfTrack, wheelY, frontZ), radius);
            WheelCollider frontRight = CreateWheel(root.transform, "WheelCollider_FR", new Vector3(halfTrack, wheelY, frontZ), radius);
            WheelCollider rearLeft = CreateWheel(root.transform, "WheelCollider_RL", new Vector3(-halfTrack, wheelY, rearZ), radius);
            WheelCollider rearRight = CreateWheel(root.transform, "WheelCollider_RR", new Vector3(halfTrack, wheelY, rearZ), radius);

            CreateAnchor(root.transform, "Camera_Chase", new Vector3(0f, bounds.max.y + 2.5f, bounds.min.z - 7f));
            CreateAnchor(root.transform, "Camera_Close", new Vector3(0f, bounds.max.y + 1.1f, bounds.min.z - 3.8f));
            CreateAnchor(root.transform, "Camera_Cabin", new Vector3(-bounds.extents.x * 0.3f, bounds.center.y + bounds.extents.y * 0.35f, bounds.max.z - bounds.size.z * 0.18f));

            KombiVehicleController controller = root.AddComponent<KombiVehicleController>();
            SerializedObject serialized = new(controller);
            SerializedProperty axles = serialized.FindProperty("axles");
            axles.arraySize = 2;
            ConfigureAxle(axles.GetArrayElementAtIndex(0), frontLeft, frontRight, true, false, false);
            ConfigureAxle(axles.GetArrayElementAtIndex(1), rearLeft, rearRight, false, true, true);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{source.name}_Gameplay.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"KOMBI_ASSETS Prepared vehicle wrapper at {path}. Assign the imported wheel meshes to the axle visual fields after verifying their pivots.");
        }

        private static void ConfigureAxle(SerializedProperty axle, WheelCollider left, WheelCollider right, bool steering, bool powered, bool braking)
        {
            axle.FindPropertyRelative("leftCollider").objectReferenceValue = left;
            axle.FindPropertyRelative("rightCollider").objectReferenceValue = right;
            axle.FindPropertyRelative("steering").boolValue = steering;
            axle.FindPropertyRelative("powered").boolValue = powered;
            axle.FindPropertyRelative("braking").boolValue = braking;
        }

        private static WheelCollider CreateWheel(Transform parent, string name, Vector3 position, float radius)
        {
            GameObject wheel = new(name, typeof(WheelCollider));
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = position;
            WheelCollider collider = wheel.GetComponent<WheelCollider>();
            collider.radius = radius;
            collider.mass = 35f;
            collider.suspensionDistance = 0.22f;
            JointSpring spring = collider.suspensionSpring;
            spring.spring = 35000f;
            spring.damper = 4500f;
            spring.targetPosition = 0.5f;
            collider.suspensionSpring = spring;
            return collider;
        }

        private static void CreateAnchor(Transform parent, string name, Vector3 position)
        {
            GameObject anchor = new(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = position;
        }

        private static Bounds CalculateLocalBounds(GameObject root, GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.up, new Vector3(2f, 2f, 4f));
            Bounds local = new(root.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                Vector3 min = root.transform.InverseTransformPoint(renderer.bounds.min);
                Vector3 max = root.transform.InverseTransformPoint(renderer.bounds.max);
                local.Encapsulate(min);
                local.Encapsulate(max);
            }
            return local;
        }

        private static void EnsureOutputFolder()
        {
            EnsureFolder("Assets/KombiShift2/Art");
            EnsureFolder("Assets/KombiShift2/Art/Vehicles");
            EnsureFolder(OutputFolder);
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
