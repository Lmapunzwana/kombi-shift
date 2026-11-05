#if UNITY_EDITOR
using System.Linq;
using BezierSolution;
using KombiShift2.Vehicle;
using KombiShift2.CameraSystem;
using KombiShift2.Roadworks;
using KombiShift2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace KombiShift2.Editor
{
    public static class RccHiaceTestSceneBuilder
    {
        private const string TemplateScene = "Assets/Realistic Car Controller Pro/Scenes/RCCP_Scene_Blank_Prototype.unity";
        private const string HiacePrefab = "Assets/KombiShift2/Art/Vehicles/Experimental/Hiace_RCC_Experimental.prefab";
        private const string CameraPrefab = "Assets/Realistic Car Controller Pro/Prefabs/Cameras/RCCP_Camera.prefab";
        private const string OutputScene = "Assets/KombiShift2/Scenes/RCC_Hiace_DrivingTest.unity";
        private const string CitySourceScene = "Assets/NewYork.unity";
        private const string CityOutputScene = "Assets/KombiShift2/Scenes/RCC_Hiace_CityTest.unity";
        private const string EasyRoadsSourceScene = "Assets/EasyRoads3D scenes/Demo Scene.unity";
        private const string EasyRoadsOutputScene = "Assets/KombiShift2/Scenes/RCC_Hiace_EasyRoadsTest.unity";

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

        [MenuItem("Kombi Shift/Vehicles/Build RCC Hiace City Test Scene")]
        public static void BuildCityDrivingTestScene()
        {
            Scene scene = EditorSceneManager.OpenScene(CitySourceScene, OpenSceneMode.Single);
            int disabledFoliageColliders = 0;
            foreach (MeshCollider collider in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!collider.name.StartsWith("tree0Plane", System.StringComparison.OrdinalIgnoreCase)) continue;
                collider.enabled = false;
                disabledFoliageColliders++;
            }

            ConfigureCityLighting(scene);
            GameObject inheritedBus = GameObject.Find("autobus");
            if (inheritedBus == null)
            {
                throw new System.InvalidOperationException("The city source scene no longer contains the autonomous bus spawn reference.");
            }

            Vector3 spawnPosition = inheritedBus.transform.position + Vector3.up * 0.65f;
            Quaternion spawnRotation = inheritedBus.transform.rotation;
            Object.DestroyImmediate(inheritedBus);

            foreach (Camera oldCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                oldCamera.gameObject.SetActive(false);
            }

            if (Object.FindFirstObjectByType<RCCP_SceneManager>() == null)
            {
                new GameObject("RCCP_SceneManager", typeof(RCCP_SceneManager));
            }
            if (Object.FindFirstObjectByType<RCCP_SkidmarksManager>() == null)
            {
                new GameObject("RCCP_SkidmarksManager", typeof(RCCP_SkidmarksManager));
            }

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HiacePrefab);
            GameObject hiace = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            hiace.name = "Hiace_RCC_Player";
            hiace.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            RCCP_CarController controller = hiace.GetComponent<RCCP_CarController>();

            GameObject cameraSource = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab);
            GameObject camera = (GameObject)PrefabUtility.InstantiatePrefab(cameraSource, scene);
            camera.name = "RCCP_Camera";

            GameObject bootstrapObject = new("Kombi_RCC_TestBootstrap");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            RccHiaceTestBootstrap bootstrap = bootstrapObject.AddComponent<RccHiaceTestBootstrap>();
            SerializedObject serialized = new(bootstrap);
            serialized.FindProperty("vehicle").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            KombiNavigationHud navigation = bootstrapObject.AddComponent<KombiNavigationHud>();
            navigation.SetVehicle(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityOutputScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"KOMBI_RCC_CITY Built {CityOutputScene} at inherited bus spawn {spawnPosition}; disabled {disabledFoliageColliders} decorative foliage colliders.");
        }

        public static void RunAutomatedCitySmokeTest()
        {
            EditorSceneManager.OpenScene(CityOutputScene, OpenSceneMode.Single);
            Debug.Log("KOMBI_RCC_CITY Entering play mode to probe the inherited city road collision.");
            EditorApplication.isPlaying = true;
        }

        public static void AuditCitySpawn()
        {
            Scene scene = EditorSceneManager.OpenScene(CityOutputScene, OpenSceneMode.Single);
            GameObject vehicle = GameObject.Find("Hiace_RCC_Player");
            if (vehicle == null) throw new System.InvalidOperationException("City test vehicle was not found.");

            Vector3 origin = vehicle.transform.position;
            foreach (BezierSpline spline in Object.FindObjectsByType<BezierSpline>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                float nearestDistance = float.MaxValue;
                float nearestT = 0f;
                for (int i = 0; i <= 100; i++)
                {
                    float t = i / 100f;
                    float distance = Vector3.Distance(origin, spline.GetPoint(t));
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestT = t;
                }
                Debug.Log($"KOMBI_CITY_SPLINE name={spline.name} points={spline.Count} length={spline.length:F1} nearestDistance={nearestDistance:F1} nearestT={nearestT:F2}.");
            }
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Debug.Log($"KOMBI_CITY_LIGHT name={light.name} active={light.gameObject.activeInHierarchy} type={light.type} intensity={light.intensity:F2} color={light.color} rotation={light.transform.eulerAngles}.");
            }

            foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (collider.transform.IsChildOf(vehicle.transform)) continue;
                float distance = Vector3.Distance(collider.bounds.ClosestPoint(origin), origin);
                if (distance <= 18f)
                {
                    Debug.Log($"KOMBI_CITY_COLLIDER name={collider.name} path={GetPath(collider.transform)} type={collider.GetType().Name} distance={distance:F2} center={collider.bounds.center} size={collider.bounds.size}.");
                }
            }
        }

        private static string GetPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        private static void ConfigureCityLighting(Scene scene)
        {
            Light sun = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(light => light.type == LightType.Directional);
            if (sun == null)
            {
                sun = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
                SceneManager.MoveGameObjectToScene(sun.gameObject, scene);
                sun.type = LightType.Directional;
            }
            sun.gameObject.SetActive(true);
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            GameObject fillObject = new("City Sky Fill", typeof(Light));
            SceneManager.MoveGameObjectToScene(fillObject, scene);
            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.38f;
            fill.color = new Color(0.68f, 0.79f, 1f);
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(35f, 145f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.66f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.17f, 0.14f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.sun = sun;
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

        [MenuItem("Kombi Shift/Vehicles/Build Linux RCC Hiace City Test")]
        public static void BuildLinuxCityDrivingTest()
        {
            const string executable = "Builds/RCC_Hiace_CityTest/KombiShift_RCC_CityTest.x86_64";
            System.IO.Directory.CreateDirectory("Builds/RCC_Hiace_CityTest");
            BuildPlayerOptions options = new()
            {
                scenes = new[] { CityOutputScene },
                locationPathName = executable,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"RCC Hiace city build failed: {report.summary.result}, errors={report.summary.totalErrors}.");
            }
            Debug.Log($"KOMBI_RCC_CITY_BUILD Built {executable} ({report.summary.totalSize} bytes).");
        }

        [MenuItem("Kombi Shift/Vehicles/Build RCC Hiace EasyRoads Test Scene")]
        public static void BuildEasyRoadsDrivingTestScene()
        {
            Scene scene = EditorSceneManager.OpenScene(EasyRoadsSourceScene, OpenSceneMode.Single);
            ConfigureCityLighting(scene);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.66f, 0.73f, 0.78f);
            RenderSettings.fogStartDistance = 280f;
            RenderSettings.fogEndDistance = 900f;
            GameObject road = GameObject.Find("road_0001");
            MeshCollider roadCollider = road != null ? road.GetComponent<MeshCollider>() : null;
            if (roadCollider == null || roadCollider.sharedMesh == null)
                throw new System.InvalidOperationException("The EasyRoads demo road mesh was not found.");

            ConformTerrainToEasyRoad(roadCollider);
            ConfigureEasyRoadsSurfaceVariety(roadCollider);

            (Vector3 spawn, Quaternion rotation) = FindRoadSpawn(roadCollider);
            foreach (Camera oldCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                oldCamera.gameObject.SetActive(false);

            if (Object.FindFirstObjectByType<RCCP_SceneManager>() == null)
                new GameObject("RCCP_SceneManager", typeof(RCCP_SceneManager));
            if (Object.FindFirstObjectByType<RCCP_SkidmarksManager>() == null)
                new GameObject("RCCP_SkidmarksManager", typeof(RCCP_SkidmarksManager));

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HiacePrefab);
            GameObject hiace = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            hiace.name = "Hiace_RCC_Player";
            // The generated RCC Hiace prefab is authored with its wheel contact height
            // relative to the root already.  The old 0.7 m lift made the whole vehicle
            // visibly hover until the suspension eventually settled.
            hiace.transform.SetPositionAndRotation(spawn + Vector3.up * 0.08f, rotation);
            RCCP_CarController controller = hiace.GetComponent<RCCP_CarController>();

            GameObject cameraSource = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab);
            GameObject camera = (GameObject)PrefabUtility.InstantiatePrefab(cameraSource, scene);
            camera.name = "RCCP_Camera";

            GameObject bootstrapObject = new("Kombi_RCC_TestBootstrap");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            RccHiaceTestBootstrap bootstrap = bootstrapObject.AddComponent<RccHiaceTestBootstrap>();
            SerializedObject serialized = new(bootstrap);
            serialized.FindProperty("vehicle").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, EasyRoadsOutputScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"KOMBI_RCC_EASYROADS Built {EasyRoadsOutputScene} at road position {spawn}.");
        }

        [MenuItem("Kombi Shift/Vehicles/Build Linux RCC Hiace EasyRoads Test")]
        public static void BuildLinuxEasyRoadsDrivingTest()
        {
            const string executable = "Builds/RCC_Hiace_EasyRoadsTest/KombiShift_RCC_EasyRoadsTest.x86_64";
            System.IO.Directory.CreateDirectory("Builds/RCC_Hiace_EasyRoadsTest");
            BuildPlayerOptions options = new()
            {
                scenes = new[] { EasyRoadsOutputScene },
                locationPathName = executable,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException($"EasyRoads test build failed: {report.summary.result}.");
            Debug.Log($"KOMBI_RCC_EASYROADS_BUILD Built {executable} ({report.summary.totalSize} bytes).");
        }

        private static void ConfigureEasyRoadsSurfaceVariety(MeshCollider collider)
        {
            MeshRenderer renderer = collider.GetComponent<MeshRenderer>();
            MeshFilter filter = collider.GetComponent<MeshFilter>();
            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/EasyRoads3D/Resources/Materials/roads/road material.mat");
            Material dirt = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/EasyRoads3D/Resources/Materials/roads/dirt material.mat");
            if (renderer == null || filter == null || asphalt == null || dirt == null)
                return;

            Mesh source = collider.sharedMesh;
            Mesh varied = Object.Instantiate(source);
            varied.name = source.name + " Asphalt and Dirt Sections";
            int[] sourceTriangles = source.triangles;
            var asphaltTriangles = new System.Collections.Generic.List<int>(sourceTriangles.Length);
            var dirtTriangles = new System.Collections.Generic.List<int>(sourceTriangles.Length / 3);

            // EasyRoads writes triangles in road-segment order.  Keep the beginning
            // asphalt for spawning, change the middle quarter to the bundled dirt
            // surface, and return to asphalt near the far end.
            int triangleCount = sourceTriangles.Length / 3;
            for (int triangle = 0; triangle < triangleCount; triangle++)
            {
                float progress = triangle / (float)Mathf.Max(1, triangleCount - 1);
                var destination = progress >= 0.38f && progress <= 0.68f
                    ? dirtTriangles
                    : asphaltTriangles;
                int offset = triangle * 3;
                destination.Add(sourceTriangles[offset]);
                destination.Add(sourceTriangles[offset + 1]);
                destination.Add(sourceTriangles[offset + 2]);
            }

            varied.subMeshCount = 2;
            varied.SetTriangles(asphaltTriangles, 0, true);
            varied.SetTriangles(dirtTriangles, 1, true);
            filter.sharedMesh = varied;
            collider.sharedMesh = varied;
            renderer.sharedMaterials = new[] { asphalt, dirt };
        }

        private static void ConformTerrainToEasyRoad(MeshCollider roadCollider)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null || terrain.terrainData == null)
                return;

            const string generatedFolder = "Assets/KombiShift2/Generated";
            const string terrainAssetPath = generatedFolder + "/EasyRoadsTestTerrain.asset";
            if (!AssetDatabase.IsValidFolder(generatedFolder))
                AssetDatabase.CreateFolder("Assets/KombiShift2", "Generated");

            TerrainData workingData = Object.Instantiate(terrain.terrainData);
            workingData.name = "EasyRoads Test Terrain";
            int resolution = workingData.heightmapResolution;
            float[,] heights = workingData.GetHeights(0, 0, resolution, resolution);
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = workingData.size;
            Vector3[] roadVertices = roadCollider.sharedMesh.vertices;

            const float innerShoulderMetres = 3.5f;
            const float outerBlendMetres = 8f;
            int radiusX = Mathf.CeilToInt(outerBlendMetres * (resolution - 1) / terrainSize.x);
            int radiusZ = Mathf.CeilToInt(outerBlendMetres * (resolution - 1) / terrainSize.z);
            foreach (Vector3 localVertex in roadVertices)
            {
                Vector3 world = roadCollider.transform.TransformPoint(localVertex);
                float normalizedX = (world.x - terrainPosition.x) / terrainSize.x;
                float normalizedZ = (world.z - terrainPosition.z) / terrainSize.z;
                if (normalizedX < 0f || normalizedX > 1f || normalizedZ < 0f || normalizedZ > 1f)
                    continue;

                int centerX = Mathf.RoundToInt(normalizedX * (resolution - 1));
                int centerZ = Mathf.RoundToInt(normalizedZ * (resolution - 1));
                float roadHeight = Mathf.Clamp01((world.y - terrainPosition.y - 0.06f) / terrainSize.y);
                for (int z = Mathf.Max(0, centerZ - radiusZ); z <= Mathf.Min(resolution - 1, centerZ + radiusZ); z++)
                {
                    for (int x = Mathf.Max(0, centerX - radiusX); x <= Mathf.Min(resolution - 1, centerX + radiusX); x++)
                    {
                        float dx = (x - centerX) * terrainSize.x / (resolution - 1);
                        float dz = (z - centerZ) * terrainSize.z / (resolution - 1);
                        float distance = Mathf.Sqrt(dx * dx + dz * dz);
                        if (distance > outerBlendMetres) continue;
                        float blend = distance <= innerShoulderMetres
                            ? 1f
                            : 1f - Mathf.SmoothStep(0f, 1f, (distance - innerShoulderMetres) / (outerBlendMetres - innerShoulderMetres));
                        heights[z, x] = Mathf.Lerp(heights[z, x], roadHeight, blend);
                    }
                }
            }

            workingData.SetHeights(0, 0, heights);
            TerrainData storedData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainAssetPath);
            if (storedData == null)
            {
                AssetDatabase.CreateAsset(workingData, terrainAssetPath);
                storedData = workingData;
            }
            else
            {
                EditorUtility.CopySerialized(workingData, storedData);
                Object.DestroyImmediate(workingData);
            }

            terrain.terrainData = storedData;
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider != null)
                terrainCollider.terrainData = storedData;
            terrain.Flush();
        }

        private static (Vector3 position, Quaternion rotation) FindRoadSpawn(MeshCollider collider)
        {
            Mesh mesh = collider.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int index = (triangles.Length / 6) * 3; index < triangles.Length - 3; index += 3)
            {
                Vector3 a = collider.transform.TransformPoint(vertices[triangles[index]]);
                Vector3 b = collider.transform.TransformPoint(vertices[triangles[index + 1]]);
                Vector3 c = collider.transform.TransformPoint(vertices[triangles[index + 2]]);
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Mathf.Abs(normal.y) < 0.75f) continue;
                Vector3 edge = (b - a).sqrMagnitude > (c - a).sqrMagnitude ? b - a : c - a;
                edge.y = 0f;
                Vector3 forward = Vector3.Cross(Vector3.up, edge.normalized);
                return ((a + b + c) / 3f, Quaternion.LookRotation(forward, Vector3.up));
            }
            return (collider.bounds.center, Quaternion.identity);
        }
    }
}
#endif
