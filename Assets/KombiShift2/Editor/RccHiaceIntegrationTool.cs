#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KombiShift2.Editor
{
    public static class RccHiaceIntegrationTool
    {
        private const string PrototypePath = "Assets/Realistic Car Controller Pro/Prefabs/Prototype/Model_Skyline by BUMSTRUM(3DMaesen) (Prototype).prefab";
        private const string HiacePath = "Assets/KombiShift2/ThirdParty/Hiace2005/source/Hiace.glb";
        private const string OutputFolder = "Assets/KombiShift2/Art/Vehicles/Experimental";
        private const string OutputPath = OutputFolder + "/Hiace_RCC_Experimental.prefab";
        private const string WheelMeshFolder = "Assets/KombiShift2/Generated/HiaceWheels";

        [MenuItem("Kombi Shift/Vehicles/Build Experimental RCC Hiace")]
        public static void BuildExperimentalRccHiace()
        {
            GameObject prototype = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePath);
            GameObject hiaceSource = AssetDatabase.LoadAssetAtPath<GameObject>(HiacePath);
            if (prototype == null || hiaceSource == null)
            {
                Debug.LogError($"KOMBI_RCC Missing source asset. RCC={prototype != null}, Hiace={hiaceSource != null}.");
                return;
            }

            EnsureFolder(OutputFolder);
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prototype);
            root.name = "Hiace_RCC_Experimental";

            // Preserve RCC's fully wired drivetrain objects while hiding the
            // prototype sports-car mesh. Several RCC systems retain references to
            // these transforms, so deleting them would break the Lite template.
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider is not WheelCollider) Object.DestroyImmediate(collider);
            }

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(hiaceSource);
            visual.name = "Hiace_Visual";
            visual.transform.SetParent(root.transform, false);
            // The source GLB's nose points along local -Z after its import-axis
            // conversion. RCC expects the vehicle nose along root +Z.
            visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }

            Bounds bounds = CalculateLocalBounds(root.transform, visual);
            visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            bounds = CalculateLocalBounds(root.transform, visual);

            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body == null) body = root.AddComponent<Rigidbody>();
            body.mass = 2300f;
            body.centerOfMass = new Vector3(0f, 0.62f, 0.08f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            RCCP_Engine engine = root.GetComponentInChildren<RCCP_Engine>(true);
            if (engine != null)
            {
                engine.maximumSpeed = 95f;
                engine.maximumTorqueAsNM = 220f;
                engine.minEngineRPM = 750f;
                // RCC's automatic gearbox needs its original RPM headroom to
                // complete shifts reliably. Road speed remains capped below.
                engine.maxEngineRPM = 8000f;
                engine.CheckAndCreateNMCurve();
            }

            RCCP_Gearbox gearbox = root.GetComponentInChildren<RCCP_Gearbox>(true);
            if (gearbox != null)
            {
                gearbox.shiftUpRPM = 6500f;
                gearbox.shiftDownRPM = 3750f;
            }

            BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(bounds.center.x, bounds.center.y + bounds.size.y * 0.03f, bounds.center.z);
            bodyCollider.size = new Vector3(bounds.size.x * 0.9f, bounds.size.y * 0.84f, bounds.size.z * 0.9f);

            Transform[] wheelVisuals = CreateIndependentWheelVisuals(root.transform, visual);
            PositionPhysicalWheels(root.transform, wheelVisuals);

            PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"KOMBI_RCC Built {OutputPath} with normalized bounds {bounds.size}, mass 2300 kg, and four repositioned RCC wheels.");
            ValidateExperimentalPrefab();
        }

        private static void ValidateExperimentalPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
            Rigidbody body = prefab != null ? prefab.GetComponent<Rigidbody>() : null;
            int wheels = prefab != null ? prefab.GetComponentsInChildren<WheelCollider>(true).Length : 0;
            int visibleRenderers = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true).Count(renderer => renderer.enabled) : 0;
            int enabledBodyColliders = prefab != null ? prefab.GetComponentsInChildren<Collider>(true).Count(collider => collider.enabled && collider is not WheelCollider) : 0;
            bool hasController = prefab != null && prefab.GetComponents<MonoBehaviour>().Any(component => component != null && component.GetType().Name == "RCCP_CarController");
            bool valid = prefab != null
                && hasController
                && body != null
                && Mathf.Approximately(body.mass, 2300f)
                && wheels == 4
                && visibleRenderers > 0
                && enabledBodyColliders == 1;

            string message = $"KOMBI_RCC_VALIDATE valid={valid}, controller={hasController}, mass={(body != null ? body.mass : 0f)}, wheels={wheels}, visibleRenderers={visibleRenderers}, bodyColliders={enabledBodyColliders}.";
            if (valid) Debug.Log(message);
            else Debug.LogError(message);
        }

        private static Transform[] CreateIndependentWheelVisuals(Transform root, GameObject visual)
        {
            EnsureFolder(WheelMeshFolder);
            GameObject wheelRoot = new("Hiace_WheelVisuals");
            wheelRoot.transform.SetParent(root, false);
            List<Transform> result = new();

            SkinnedMeshRenderer[] sources = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.name.StartsWith("hiace_wheel.smd"))
                .ToArray();
            foreach (SkinnedMeshRenderer source in sources)
            {
                Mesh baked = new();
                source.BakeMesh(baked);
                Vector3 meshCentre = baked.bounds.center;
                Vector3 worldCentre = source.transform.TransformPoint(meshCentre);
                Vector3[] vertices = baked.vertices;
                for (int i = 0; i < vertices.Length; i++) vertices[i] -= meshCentre;
                baked.vertices = vertices;
                baked.RecalculateBounds();
                baked.name = source.name.Replace('.', '_') + "_Centered";

                string meshPath = $"{WheelMeshFolder}/{baked.name}.asset";
                Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (savedMesh == null)
                {
                    AssetDatabase.CreateAsset(baked, meshPath);
                    savedMesh = baked;
                }
                else
                {
                    Object.DestroyImmediate(baked);
                }

                GameObject wheel = new(source.name + "_Visual", typeof(MeshFilter), typeof(MeshRenderer));
                wheel.transform.SetParent(wheelRoot.transform, false);
                wheel.transform.position = worldCentre;
                wheel.transform.rotation = source.transform.rotation;
                wheel.GetComponent<MeshFilter>().sharedMesh = savedMesh;
                wheel.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
                result.Add(wheel.transform);
                source.enabled = false;
            }

            return result.ToArray();
        }

        private static void PositionPhysicalWheels(Transform root, Transform[] wheelVisuals)
        {
            WheelCollider[] wheels = root.GetComponentsInChildren<WheelCollider>(true);
            if (wheels.Length != 4 || wheelVisuals.Length != 4)
            {
                Debug.LogError($"KOMBI_RCC Expected four physics and visual wheels but found physics={wheels.Length}, visuals={wheelVisuals.Length}.");
                return;
            }

            HashSet<Transform> available = new(wheelVisuals);
            foreach (WheelCollider wheel in wheels)
            {
                Vector3 current = root.InverseTransformPoint(wheel.transform.position);
                Transform match = available.OrderBy(candidate =>
                {
                    Vector3 candidatePosition = root.InverseTransformPoint(candidate.position);
                    bool sameSide = Mathf.Sign(candidatePosition.x) == Mathf.Sign(current.x);
                    bool sameAxle = Mathf.Sign(candidatePosition.z) == Mathf.Sign(current.z);
                    return Vector3.SqrMagnitude(candidatePosition - current) + (sameSide ? 0f : 100f) + (sameAxle ? 0f : 100f);
                }).First();
                available.Remove(match);
                wheel.transform.position = match.position;
                wheel.transform.rotation = root.rotation;
                wheel.radius = 0.265f;
                wheel.suspensionDistance = 0.24f;
                wheel.mass = 40f;
            }

            foreach (RCCP_Axle axle in root.GetComponentsInChildren<RCCP_Axle>(true))
            {
                if (axle.leftWheelCollider != null)
                {
                    axle.leftWheelModel = wheelVisuals.OrderBy(candidate => Vector3.SqrMagnitude(candidate.position - axle.leftWheelCollider.transform.position)).First();
                    axle.leftWheelCollider.wheelModel = axle.leftWheelModel;
                }
                if (axle.rightWheelCollider != null)
                {
                    axle.rightWheelModel = wheelVisuals.OrderBy(candidate => Vector3.SqrMagnitude(candidate.position - axle.rightWheelCollider.transform.position)).First();
                    axle.rightWheelCollider.wheelModel = axle.rightWheelModel;
                }
            }
        }

        private static Bounds CalculateLocalBounds(Transform root, GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0) return new Bounds(Vector3.up, new Vector3(1.8f, 2f, 4f));

            Bounds bounds = new(root.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(root.InverseTransformPoint(renderer.bounds.min));
                bounds.Encapsulate(root.InverseTransformPoint(renderer.bounds.max));
            }
            return bounds;
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
