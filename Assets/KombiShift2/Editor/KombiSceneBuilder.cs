#if UNITY_EDITOR
using BezierSolution;
using KombiShift2.CameraSystem;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace KombiShift2.Editor
{
    public static class KombiSceneBuilder
    {
        [MenuItem("Kombi Shift/Build Startup And Driving Scenes")]
        public static void Build()
        {
            BuildMenu();
            BuildDrive();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/KombiShift2/Scenes/KombiMainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/KombiShift2/Scenes/KombiDrive.unity", true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void BuildLinuxPlayer()
        {
            Build();
            XRGeneralSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (xrSettings != null && xrSettings.Manager != null)
            {
                xrSettings.InitManagerOnStart = false;
                xrSettings.Manager.automaticLoading = false;
                xrSettings.Manager.automaticRunning = false;
                EditorUtility.SetDirty(xrSettings);
                EditorUtility.SetDirty(xrSettings.Manager);
                AssetDatabase.SaveAssets();
            }
            const string output = "Builds/Linux/KombiShift2.x86_64";
            System.IO.Directory.CreateDirectory("Builds/Linux");
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/KombiShift2/Scenes/KombiMainMenu.unity",
                    "Assets/KombiShift2/Scenes/KombiDrive.unity"
                },
                locationPathName = output,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            });
        }

        public static void DiagnoseDriveScene()
        {
            EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiDrive.unity", OpenSceneMode.Single);
            GameObject bus = GameObject.Find("autobus");
            Bounds bounds = default;
            bool initialized = false;
            foreach (Renderer renderer in bus.GetComponentsInChildren<Renderer>())
            {
                if (!initialized) { bounds = renderer.bounds; initialized = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            BoxCollider box = bus.GetComponent<BoxCollider>();
            Debug.Log($"KOMBI_DIAGNOSTIC root={bus.transform.position} forward={bus.transform.forward} right={bus.transform.right} visualCenter={bounds.center} visualSize={bounds.size} localVisualCenter={bus.transform.InverseTransformPoint(bounds.center)} colliderCenter={box.center} colliderSize={box.size}");
        }

        private static void BuildMenu()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.54f);

            GameObject light = new("Sun", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Menu Road";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("MenuRoad", new Color(0.08f, 0.09f, 0.09f));

            GameObject busPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BusModel/AutonomousBus_01/Prefabs/AutonomousBus_01_Closed.prefab");
            GameObject bus = (GameObject)PrefabUtility.InstantiatePrefab(busPrefab);
            bus.name = "Kombi Shift Hero Bus";
            bus.transform.SetPositionAndRotation(new Vector3(1.5f, 0f, 4f), Quaternion.Euler(0f, -28f, 0f));

            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(-11f, 5.3f, -10.5f);
            camera.transform.LookAt(bus.transform.position + Vector3.up * 1.8f);
            camera.fieldOfView = 48f;
            camera.backgroundColor = new Color(0.25f, 0.38f, 0.4f);

            new GameObject("Kombi Main Menu", typeof(KombiMainMenu));
            EditorSceneManager.SaveScene(scene, "Assets/KombiShift2/Scenes/KombiMainMenu.unity");
        }

        private static void BuildDrive()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/NewYork.unity", OpenSceneMode.Single);
            GameObject bus = GameObject.Find("autobus");
            if (bus == null) throw new System.InvalidOperationException("The NewYork scene no longer contains the autobus object.");

            foreach (BezierWalkerWithSpeed walker in Object.FindObjectsByType<BezierWalkerWithSpeed>(FindObjectsSortMode.None))
                Object.DestroyImmediate(walker);

            foreach (Camera oldCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                oldCamera.gameObject.SetActive(false);

            Rigidbody body = bus.GetComponent<Rigidbody>();
            if (body == null) body = bus.AddComponent<Rigidbody>();
            BoxCollider collider = bus.GetComponent<BoxCollider>();
            if (collider == null) collider = bus.AddComponent<BoxCollider>();
            // Match the imported vehicle (roughly 2.1 m wide x 4.2 m long).
            // The old 9.5 m box penetrated the sloped road and prevented translation.
            collider.center = new Vector3(0f, 1.38f, 0f);
            collider.size = new Vector3(2.05f, 2.65f, 4.15f);
            bus.transform.position += Vector3.up * .14f;
            bus.AddComponent<PlayerBusController>();

            Camera camera = new GameObject("Player Camera", typeof(Camera), typeof(AudioListener), typeof(PlayerBusCamera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = bus.transform.TransformPoint(new Vector3(0f, 5.2f, -12f));
            camera.GetComponent<PlayerBusCamera>().SetTarget(bus.transform);
            camera.farClipPlane = 2500f;

            EditorSceneManager.SaveScene(scene, "Assets/KombiShift2/Scenes/KombiDrive.unity");
        }

        private static Material MakeMaterial(string name, Color colour)
        {
            string path = $"Assets/KombiShift2/Generated/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = colour };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
