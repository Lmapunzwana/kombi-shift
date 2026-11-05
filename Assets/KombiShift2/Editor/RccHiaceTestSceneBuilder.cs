#if UNITY_EDITOR
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KombiShift2.Editor
{
    public static class RccHiaceTestSceneBuilder
    {
        private const string TemplateScene = "Assets/Realistic Car Controller Pro/Scenes/RCCP_Scene_Blank_Prototype.unity";
        private const string HiacePrefab = "Assets/KombiShift2/Art/Vehicles/Experimental/Hiace_RCC_Experimental.prefab";
        private const string CameraPrefab = "Assets/Realistic Car Controller Pro/Prefabs/Cameras/RCCP_Camera.prefab";
        private const string OutputScene = "Assets/KombiShift2/Scenes/RCC_Hiace_DrivingTest.unity";

        [MenuItem("Kombi Shift/Vehicles/Build RCC Hiace Driving Test Scene")]
        public static void BuildDrivingTestScene()
        {
            if (!AssetDatabase.CopyAsset(TemplateScene, OutputScene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScene) == null)
            {
                Debug.LogError($"KOMBI_RCC_TEST Could not copy template scene to {OutputScene}.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            GameObject rccCanvas = GameObject.Find("RCCP_Canvas");
            if (rccCanvas != null)
            {
                Object.DestroyImmediate(rccCanvas);
            }

            GameObject plane = GameObject.Find("Plane");
            if (plane != null)
            {
                Vector3 position = plane.transform.position;
                plane.transform.position = new Vector3(position.x, 0f, position.z);
            }

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HiacePrefab);
            GameObject hiace = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            hiace.name = "Hiace_RCC_Player";
            hiace.transform.SetPositionAndRotation(new Vector3(0f, 0.4f, 0f), Quaternion.identity);

            RCCP_CarController controller = hiace.GetComponent<RCCP_CarController>();
            if (controller == null)
            {
                Debug.LogError("KOMBI_RCC_TEST Experimental Hiace has no RCCP_CarController.");
                return;
            }

            if (Object.FindFirstObjectByType<RCCP_Camera>() == null)
            {
                GameObject cameraSource = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab);
                GameObject camera = (GameObject)PrefabUtility.InstantiatePrefab(cameraSource, scene);
                camera.name = "RCCP_Camera";
            }

            GameObject bootstrapObject = new("Kombi_RCC_TestBootstrap");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            RccHiaceTestBootstrap bootstrap = bootstrapObject.AddComponent<RccHiaceTestBootstrap>();
            SerializedObject serialized = new(bootstrap);
            serialized.FindProperty("vehicle").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OutputScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"KOMBI_RCC_TEST Built {OutputScene} with the RCC Hiace, player registration bootstrap, RCC camera, managers, light, and test ground.");
        }

        public static void RunAutomatedSmokeTest()
        {
            EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            Debug.Log("KOMBI_RCC_TEST Entering play mode for the automated Hiace movement test.");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Kombi Shift/Vehicles/Build Linux RCC Hiace Driving Test")]
        public static void BuildLinuxDrivingTest()
        {
            const string executable = "Builds/RCC_Hiace_Test/KombiShift_RCC_Test.x86_64";
            System.IO.Directory.CreateDirectory("Builds/RCC_Hiace_Test");
            BuildPlayerOptions options = new()
            {
                scenes = new[] { OutputScene },
                locationPathName = executable,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"RCC Hiace test build failed: {report.summary.result}, errors={report.summary.totalErrors}.");
            }
            Debug.Log($"KOMBI_RCC_TEST_BUILD Built {executable} ({report.summary.totalSize} bytes).");
        }
    }
}
#endif
