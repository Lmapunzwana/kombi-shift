#if UNITY_EDITOR
using System.Linq;
using BezierSolution;
using KombiShift2.Vehicle;
using KombiShift2.CameraSystem;
using KombiShift2.Roadworks;
using KombiShift2.UI;
using KombiShift2.Weather;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
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
        private const string FirstShiftScene = "Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity";

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
            KombiWeatherController weather = bootstrapObject.AddComponent<KombiWeatherController>();
            weather.SetPlayerVehicle(hiace);

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
            KombiWeatherController weather = bootstrapObject.AddComponent<KombiWeatherController>();
            weather.SetPlayerVehicle(hiace);

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

        [MenuItem("Kombi Shift/Archive/Build Legacy EasyRoads First Shift")]
        public static void BuildLegacyEasyRoadsFirstShiftScene()
        {
            Scene scene = EditorSceneManager.OpenScene(EasyRoadsSourceScene, OpenSceneMode.Single);
            ConfigureCityLighting(scene);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.66f, .73f, .78f);
            RenderSettings.fogStartDistance = 280f;
            RenderSettings.fogEndDistance = 900f;

            GameObject road = GameObject.Find("road_0001");
            MeshCollider roadCollider = road != null ? road.GetComponent<MeshCollider>() : null;
            if (roadCollider == null || roadCollider.sharedMesh == null)
                throw new System.InvalidOperationException("EasyRoads road mesh is missing.");
            ConformTerrainToEasyRoad(roadCollider);
            ConfigureEasyRoadsSurfaceVariety(roadCollider);
            Transform[] route = CreateRoadRoute(scene, roadCollider, 34);

            foreach (Camera oldCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                oldCamera.gameObject.SetActive(false);
            if (Object.FindFirstObjectByType<RCCP_SceneManager>() == null) new GameObject("RCCP_SceneManager", typeof(RCCP_SceneManager));
            if (Object.FindFirstObjectByType<RCCP_SkidmarksManager>() == null) new GameObject("RCCP_SkidmarksManager", typeof(RCCP_SkidmarksManager));

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HiacePrefab);
            GameObject hiace = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            hiace.name = "Hiace_RCC_Player";
            Vector3 forward = route[2].position - route[1].position;
            forward.y = 0f;
            hiace.transform.SetPositionAndRotation(route[1].position + Vector3.up * .08f, Quaternion.LookRotation(forward));
            RCCP_CarController controller = hiace.GetComponent<RCCP_CarController>();
            RCCP_Engine engine = hiace.GetComponentInChildren<RCCP_Engine>(true);
            if (engine != null) engine.maximumSpeed = 180f;
            controller.steerAngle = 34f;

            GameObject camera = new("Kombi Drive Camera", typeof(Camera), typeof(AudioListener), typeof(KombiDriveCamera));
            SceneManager.MoveGameObjectToScene(camera, scene);
            camera.GetComponent<KombiDriveCamera>().SetVehicle(hiace.transform);

            GameObject systems = new("KombiShift_FirstShift_Systems");
            SceneManager.MoveGameObjectToScene(systems, scene);
            RccHiaceTestBootstrap bootstrap = systems.AddComponent<RccHiaceTestBootstrap>();
            SerializedObject bootstrapData = new(bootstrap);
            bootstrapData.FindProperty("vehicle").objectReferenceValue = controller;
            bootstrapData.ApplyModifiedPropertiesWithoutUndo();
            KombiWeatherController weather = systems.AddComponent<KombiWeatherController>();
            weather.SetPlayerVehicle(hiace);

            Transform[] stops = CreatePassengerStops(scene, route);
            KombiShiftGameDirector director = systems.AddComponent<KombiShiftGameDirector>();
            director.Configure(controller, stops);
            KombiNavigationHud hud = systems.AddComponent<KombiNavigationHud>();
            hud.Configure(controller, route, director);
            ConfigureVehicleAccessories(hiace, weather);
            CreateTraffic(scene, route);
            CreateWideRoadAndIntersections(scene, road, route);
            CreateRoadHazards(scene, route);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FirstShiftScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"KOMBI_FIRST_SHIFT Built {FirstShiftScene} with routePoints={route.Length}, stops={stops.Length}.");
        }

        [MenuItem("Kombi Shift/Build Playable First Shift Scene")]
        public static void BuildPlayableFirstShiftScene()
        {
            Scene scene = EditorSceneManager.OpenScene(CitySourceScene, OpenSceneMode.Single);
            ConfigureCityLighting(scene);
            foreach (Camera oldCamera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) oldCamera.gameObject.SetActive(false);
            foreach (MeshCollider collider in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (collider.name.StartsWith("tree0Plane", System.StringComparison.OrdinalIgnoreCase)) collider.enabled = false;
            GameObject oldBus = GameObject.Find("autobus"); if (oldBus != null) Object.DestroyImmediate(oldBus);

            BezierSpline[] splines = Object.FindObjectsByType<BezierSpline>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (splines.Length == 0) throw new System.InvalidOperationException("The city has no route splines.");
            BezierSpline mainSpline = splines.OrderByDescending(spline => spline.length).First();
            Transform[] route = CreateSplineRoute(scene, mainSpline, 64, "CBD to Mbare Route");
            Material asphalt = GetOrCreateCityMaterial("City Asphalt", new Color(.105f, .115f, .12f), .18f);
            Material marking = GetOrCreateCityMaterial("City Road Marking", new Color(.95f, .78f, .12f), .1f);
            var streetRoutes = new System.Collections.Generic.List<Transform[]>();
            foreach (BezierSpline spline in splines)
            {
                Transform[] streetRoute = spline == mainSpline ? route : CreateSplineRoute(scene, spline, Mathf.Max(20, Mathf.CeilToInt(spline.length / 10f)), $"City Street {spline.name}");
                streetRoutes.Add(streetRoute);
                CreateRoadRibbon(scene, streetRoute, 10f, asphalt, $"Asphalt Street {spline.name}");
                CreateRoadRibbon(scene, streetRoute, .16f, marking, $"Centre Line {spline.name}", .10f);
            }
            CreateCityIntersections(scene, streetRoutes, asphalt);

            if (Object.FindFirstObjectByType<RCCP_SceneManager>() == null) new GameObject("RCCP_SceneManager", typeof(RCCP_SceneManager));
            if (Object.FindFirstObjectByType<RCCP_SkidmarksManager>() == null) new GameObject("RCCP_SkidmarksManager", typeof(RCCP_SkidmarksManager));
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HiacePrefab);
            GameObject hiace = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
            hiace.name = "Hiace_RCC_Player";
            Vector3 heading = route[2].position - route[1].position; heading.y = 0f;
            hiace.transform.SetPositionAndRotation(route[1].position + Vector3.up * .16f, Quaternion.LookRotation(heading));
            RCCP_CarController controller = hiace.GetComponent<RCCP_CarController>();
            RCCP_Engine engine = hiace.GetComponentInChildren<RCCP_Engine>(true); if (engine != null) engine.maximumSpeed = 180f;
            controller.steerAngle = 31f;
            Rigidbody body = hiace.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.mass = 2050f;
                body.centerOfMass = new Vector3(0f, .58f, .06f);
                body.linearDamping = .02f;
                body.angularDamping = .72f;
                body.maxAngularVelocity = 5.5f;
            }

            GameObject camera = new("Kombi Drive Camera", typeof(Camera), typeof(AudioListener), typeof(KombiDriveCamera));
            SceneManager.MoveGameObjectToScene(camera, scene); camera.GetComponent<KombiDriveCamera>().SetVehicle(hiace.transform);
            GameObject systems = new("KombiShift_CityGame_Systems"); SceneManager.MoveGameObjectToScene(systems, scene);
            RccHiaceTestBootstrap bootstrap = systems.AddComponent<RccHiaceTestBootstrap>();
            SerializedObject bootstrapData = new(bootstrap); bootstrapData.FindProperty("vehicle").objectReferenceValue = controller; bootstrapData.ApplyModifiedPropertiesWithoutUndo();
            KombiWeatherController weather = systems.AddComponent<KombiWeatherController>(); weather.SetPlayerVehicle(hiace);
            Transform[] stops = CreatePassengerStops(scene, route);
            KombiShiftGameDirector director = systems.AddComponent<KombiShiftGameDirector>(); director.Configure(controller, stops);
            KombiNavigationHud hud = systems.AddComponent<KombiNavigationHud>(); hud.Configure(controller, route, director);
            ConfigureVehicleAccessories(hiace, weather);
            CreateTraffic(scene, route);
            // Roadworks and potholes are intentionally omitted until proper art and an
            // environmentally sensible placement pass are available.

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FirstShiftScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"KOMBI_CITY_FIRST_SHIFT Built {FirstShiftScene} using {splines.Length} city streets, routePoints={route.Length}, stops={stops.Length}.");
        }

        [MenuItem("Kombi Shift/Build Linux Playable First Shift")]
        public static void BuildLinuxPlayableFirstShift()
        {
            const string executable = "Builds/KombiShift_FirstShift/KombiShift.x86_64";
            System.IO.Directory.CreateDirectory("Builds/KombiShift_FirstShift");
            BuildPlayerOptions options = new()
            {
                scenes = new[] { "Assets/KombiShift2/Scenes/KombiMainMenu.unity", FirstShiftScene },
                locationPathName = executable,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException($"First Shift build failed: {report.summary.result}.");
            Debug.Log($"KOMBI_FIRST_SHIFT_BUILD Built {executable} ({report.summary.totalSize} bytes).");
        }

        public static void RunAutomatedFirstShiftSmokeTest()
        {
            EditorSceneManager.OpenScene(FirstShiftScene, OpenSceneMode.Single);
            Debug.Log("KOMBI_FIRST_SHIFT_SMOKE Entering play mode for combined-game vehicle validation.");
            EditorApplication.isPlaying = true;
        }

        private static Transform[] CreateSplineRoute(Scene scene, BezierSpline spline, int count, string name)
        {
            GameObject root = new(name); SceneManager.MoveGameObjectToScene(root, scene);
            Transform[] route = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                GameObject point = new($"Route {i:00}"); point.transform.SetParent(root.transform);
                Vector3 splinePoint = spline.GetPoint(i / (float)(count - 1));
                point.transform.position = FindRoadSurface(splinePoint) + Vector3.up * .025f;
                route[i] = point.transform;
            }
            return route;
        }

        private static Material GetOrCreateCityMaterial(string name, Color color, float smoothness)
        {
            const string folder = "Assets/KombiShift2/Generated/City";
            if (!AssetDatabase.IsValidFolder("Assets/KombiShift2/Generated/City")) AssetDatabase.CreateFolder("Assets/KombiShift2/Generated", "City");
            string path = $"{folder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color; material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateRoadRibbon(Scene scene, Transform[] route, float width, Material material, string name, float verticalOffset = .06f)
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();
            float distance = 0f;
            for (int i = 0; i < route.Length; i++)
            {
                Vector3 previous = route[Mathf.Max(0, i - 1)].position;
                Vector3 next = route[Mathf.Min(route.Length - 1, i + 1)].position;
                Vector3 forward = next - previous; forward.y = 0f; forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                if (i > 0) distance += Vector3.Distance(route[i - 1].position, route[i].position);
                vertices.Add(route[i].position - right * width * .5f + Vector3.up * verticalOffset);
                vertices.Add(route[i].position + right * width * .5f + Vector3.up * verticalOffset);
                uvs.Add(new Vector2(0f, distance / 8f)); uvs.Add(new Vector2(1f, distance / 8f));
                if (i == route.Length - 1) continue;
                int v = i * 2; triangles.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
            }
            Mesh mesh = new() { name = name + " Mesh" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject road = new(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)); SceneManager.MoveGameObjectToScene(road, scene);
            road.GetComponent<MeshFilter>().sharedMesh = mesh; road.GetComponent<MeshRenderer>().sharedMaterial = material; road.GetComponent<MeshCollider>().sharedMesh = mesh;
            return road;
        }

        private static void CreateCityIntersections(Scene scene, System.Collections.Generic.IReadOnlyList<Transform[]> streets, Material asphalt)
        {
            var junctions = new System.Collections.Generic.List<Vector3>();
            for (int a = 0; a < streets.Count; a++)
            {
                for (int b = a + 1; b < streets.Count; b++)
                {
                    Transform[] first = streets[a]; Transform[] second = streets[b];
                    float nearestDistance = float.MaxValue;
                    float nearestAngle = 0f;
                    for (int i = 1; i < first.Length - 1; i++) for (int j = 1; j < second.Length - 1; j++)
                    {
                        Vector3 delta = first[i].position - second[j].position; delta.y = 0f;
                        if (delta.magnitude < nearestDistance)
                        {
                            nearestDistance = delta.magnitude;
                            Vector3 auditFirst = first[i + 1].position - first[i - 1].position; auditFirst.y = 0f;
                            Vector3 auditSecond = second[j + 1].position - second[j - 1].position; auditSecond.y = 0f;
                            nearestAngle = Vector3.Angle(auditFirst, auditSecond);
                            nearestAngle = Mathf.Min(nearestAngle, 180f - nearestAngle);
                        }
                        // Ten-metre streets whose centrelines approach within fourteen
                        // metres have touching/near-touching carriageway edges.
                        if (delta.sqrMagnitude > 196f) continue;
                        Vector3 firstDirection = first[i + 1].position - first[i - 1].position; firstDirection.y = 0f;
                        Vector3 secondDirection = second[j + 1].position - second[j - 1].position; secondDirection.y = 0f;
                        float angle = Vector3.Angle(firstDirection, secondDirection);
                        angle = Mathf.Min(angle, 180f - angle);
                        if (angle < 28f) continue;
                        Vector3 center = FindRoadSurface((first[i].position + second[j].position) * .5f);
                        if (junctions.Any(existing => Vector3.Distance(existing, center) < 28f)) continue;
                        junctions.Add(center);
                        GameObject crossing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        crossing.name = "City Asphalt Junction";
                        crossing.transform.position = center + Vector3.up * .035f;
                        crossing.transform.localScale = new Vector3(7.2f, .025f, 7.2f);
                        crossing.GetComponent<Renderer>().sharedMaterial = asphalt;
                        Object.DestroyImmediate(crossing.GetComponent<Collider>());
                        SceneManager.MoveGameObjectToScene(crossing, scene);
                        Vector3 forward = firstDirection.normalized;
                        Vector3 right = Vector3.Cross(Vector3.up, forward);
                        CreateTrafficSignal(scene, center + forward * 6.5f + right * 5.4f, -forward);
                        CreateTrafficSignal(scene, center - forward * 6.5f - right * 5.4f, forward);
                    }
                    Debug.Log($"KOMBI_CITY_STREET_DISTANCE {a}-{b}: {nearestDistance:0.0}m angle={nearestAngle:0.0}");
                }
            }
            Debug.Log($"KOMBI_CITY_JUNCTIONS Created {junctions.Count} geometry-derived junctions.");
        }

        private static Vector3 FindRoadSurface(Vector3 point)
        {
            RaycastHit[] hits = Physics.RaycastAll(point + Vector3.up * 120f, Vector3.down, 240f, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits.OrderByDescending(candidate => candidate.point.y))
            {
                if (hit.collider == null || hit.collider.name.StartsWith("tree0Plane", System.StringComparison.OrdinalIgnoreCase)) continue;
                return hit.point;
            }
            return point;
        }

        private static Transform[] CreateRoadRoute(Scene scene, MeshCollider collider, int count)
        {
            Mesh mesh = collider.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            GameObject root = new("First Shift Route");
            SceneManager.MoveGameObjectToScene(root, scene);
            var points = new System.Collections.Generic.List<Transform>();
            int triangleCount = triangles.Length / 3;
            for (int sample = 0; sample < count; sample++)
            {
                int triangle = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5, triangleCount - 6, sample / (float)(count - 1))), 0, triangleCount - 1);
                for (int search = 0; search < 80 && triangle < triangleCount; search++, triangle++)
                {
                    int offset = triangle * 3;
                    Vector3 a = collider.transform.TransformPoint(vertices[triangles[offset]]);
                    Vector3 b = collider.transform.TransformPoint(vertices[triangles[offset + 1]]);
                    Vector3 c = collider.transform.TransformPoint(vertices[triangles[offset + 2]]);
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    if (Mathf.Abs(normal.y) < .75f) continue;
                    GameObject point = new($"Route {sample:00}");
                    point.transform.SetParent(root.transform);
                    point.transform.position = (a + b + c) / 3f;
                    points.Add(point.transform);
                    break;
                }
            }
            return points.ToArray();
        }

        private static Transform[] CreatePassengerStops(Scene scene, Transform[] route)
        {
            int[] indices = {
                Mathf.Clamp(Mathf.RoundToInt((route.Length - 1) * .06f), 1, route.Length - 2),
                Mathf.Clamp(Mathf.RoundToInt((route.Length - 1) * .34f), 1, route.Length - 2),
                Mathf.Clamp(Mathf.RoundToInt((route.Length - 1) * .66f), 1, route.Length - 2),
                Mathf.Clamp(Mathf.RoundToInt((route.Length - 1) * .94f), 1, route.Length - 2)
            };
            GameObject passengerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Homeless_people/Free_Scavenger/Prefab/Scavenger Variant.prefab");
            var stops = new System.Collections.Generic.List<Transform>();
            foreach (int routeIndex in indices)
            {
                int index = Mathf.Min(routeIndex, route.Length - 2);
                Vector3 forward = (route[index + 1].position - route[index].position).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                GameObject stop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stop.name = index == indices[^1] ? "Mbare Drop-off" : "Roadside Passenger Stop";
                stop.transform.position = route[index].position + right * 5.8f + Vector3.up * .15f;
                stop.transform.localScale = new Vector3(2.2f, .12f, 2.2f);
                stop.GetComponent<Collider>().enabled = false;
                SceneManager.MoveGameObjectToScene(stop, scene);
                stops.Add(stop.transform);
                if (passengerPrefab == null || index == indices[^1]) continue;
                for (int person = 0; person < 3; person++)
                {
                    GameObject passenger = (GameObject)PrefabUtility.InstantiatePrefab(passengerPrefab, scene);
                    passenger.name = $"Waiting Passenger {index}-{person}";
                    passenger.transform.position = stop.transform.position + right * (person * 1.1f - 1.1f) + forward * 2.2f;
                    passenger.transform.rotation = Quaternion.LookRotation(-right);
                    passenger.transform.SetParent(stop.transform, true);
                }
            }
            return stops.ToArray();
        }

        private static void CreateTraffic(Scene scene, Transform[] route)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/KombiShift2/Art/Vehicles/Traffic" });
            string[] prefabs = guids.Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith("_Traffic.prefab")).ToArray();
            for (int i = 0; i < Mathf.Min(10, prefabs.Length); i++)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
                GameObject traffic = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                traffic.name = $"Traffic {i + 1:00}";
                if (traffic.GetComponentInChildren<Collider>() == null)
                {
                    BoxCollider collider = traffic.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0f, .75f, 0f);
                    collider.size = new Vector3(1.8f, 1.5f, 4.2f);
                }
                KombiTrafficVehicle driver = traffic.AddComponent<KombiTrafficVehicle>();
                int start = Mathf.Clamp(Mathf.RoundToInt((i + 1) * (route.Length - 2) / 11f), 1, route.Length - 2);
                driver.Configure(route, start, 7f + (i % 3) * 1.4f, i % 3 == 0);
            }
        }

        private static void CreateWideRoadAndIntersections(Scene scene, GameObject originalRoad, Transform[] route)
        {
            const float halfWidth = 4f;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();
            float distance = 0f;
            for (int i = 0; i < route.Length; i++)
            {
                Vector3 previous = route[Mathf.Max(0, i - 1)].position;
                Vector3 next = route[Mathf.Min(route.Length - 1, i + 1)].position;
                Vector3 forward = next - previous; forward.y = 0f; forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                if (i > 0) distance += Vector3.Distance(route[i - 1].position, route[i].position);
                vertices.Add(route[i].position - right * halfWidth + Vector3.up * .07f);
                vertices.Add(route[i].position + right * halfWidth + Vector3.up * .07f);
                uvs.Add(new Vector2(0f, distance / 8f)); uvs.Add(new Vector2(1f, distance / 8f));
                if (i == route.Length - 1) continue;
                int v = i * 2;
                triangles.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3 });
            }
            Mesh mesh = new() { name = "First Shift 8m Road" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject wideRoad = new("First Shift Wide Road", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            SceneManager.MoveGameObjectToScene(wideRoad, scene);
            wideRoad.GetComponent<MeshFilter>().sharedMesh = mesh;
            wideRoad.GetComponent<MeshCollider>().sharedMesh = mesh;
            wideRoad.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/EasyRoads3D/Resources/Materials/roads/road material.mat");
            if (originalRoad.TryGetComponent(out MeshRenderer oldRenderer)) oldRenderer.enabled = false;
            if (originalRoad.TryGetComponent(out MeshCollider oldCollider)) oldCollider.enabled = false;

            foreach (int index in new[] { 10, 24 })
            {
                if (index >= route.Length - 1) continue;
                Vector3 forward = route[index + 1].position - route[index - 1].position; forward.y = 0f; forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                GameObject crossing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossing.name = "Visible Road Intersection";
                crossing.transform.position = route[index].position + Vector3.up * .04f;
                crossing.transform.rotation = Quaternion.LookRotation(right);
                crossing.transform.localScale = new Vector3(8f, .08f, 30f);
                crossing.GetComponent<Renderer>().sharedMaterial = wideRoad.GetComponent<MeshRenderer>().sharedMaterial;
                SceneManager.MoveGameObjectToScene(crossing, scene);
                CreateTrafficSignal(scene, route[index].position + forward * 6f + right * 6f, -forward);
                CreateTrafficSignal(scene, route[index].position - forward * 6f - right * 6f, forward);
            }
        }

        private static void CreateTrafficSignal(Scene scene, Vector3 position, Vector3 facing)
        {
            GameObject root = new("Working Traffic Light");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing));
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root.transform, false); pole.transform.localPosition = new Vector3(0f, 2.4f, 0f); pole.transform.localScale = new Vector3(.12f, 2.4f, .12f);
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.transform.SetParent(root.transform, false); housing.transform.localPosition = new Vector3(0f, 4.8f, 0f); housing.transform.localScale = new Vector3(.7f, 1.8f, .55f);
            Renderer[] lamps = new Renderer[3];
            Color[] colors = { Color.red, new Color(1f, .45f, 0f), Color.green };
            for (int i = 0; i < 3; i++)
            {
                GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lamp.name = i == 0 ? "Red" : i == 1 ? "Amber" : "Green";
                lamp.transform.SetParent(root.transform, false); lamp.transform.localPosition = new Vector3(0f, 5.35f - i * .55f, -.32f); lamp.transform.localScale = Vector3.one * .35f;
                Material material = new(Shader.Find("Standard")); material.color = colors[i] * .25f; material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black);
                lamps[i] = lamp.GetComponent<Renderer>(); lamps[i].sharedMaterial = material;
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
            }
            TrafficSignalController signal = root.AddComponent<TrafficSignalController>();
            SerializedObject data = new(signal);
            data.FindProperty("redLamp").objectReferenceValue = lamps[0]; data.FindProperty("amberLamp").objectReferenceValue = lamps[1]; data.FindProperty("greenLamp").objectReferenceValue = lamps[2];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateRoadHazards(Scene scene, Transform[] route)
        {
            foreach (int index in new[] { 13, 15, 17, 23 })
            {
                if (index >= route.Length) continue;
                GameObject hazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hazard.name = "Visible Pothole Hazard";
                hazard.transform.position = route[index].position + Vector3.up * .09f;
                hazard.transform.localScale = new Vector3(1.25f, .035f, .9f);
                Material dark = new(Shader.Find("Standard")); dark.color = new Color(.045f, .04f, .035f); hazard.GetComponent<Renderer>().sharedMaterial = dark;
                Collider collider = hazard.GetComponent<Collider>(); collider.isTrigger = true;
                hazard.AddComponent<PotholeHazard>();
                SceneManager.MoveGameObjectToScene(hazard, scene);
            }
            for (int i = 18; i <= 24 && i < route.Length; i++)
            {
                Vector3 forward = i < route.Length - 1 ? route[i + 1].position - route[i].position : route[i].position - route[i - 1].position;
                forward.y = 0f; forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barrier.name = "Roadworks Orange Barrier";
                barrier.transform.position = route[i].position + Vector3.up * .55f + right * 2.4f;
                barrier.transform.rotation = Quaternion.LookRotation(forward);
                barrier.transform.localScale = new Vector3(.45f, 1.1f, .45f);
                Material orange = new(Shader.Find("Standard")); orange.color = new Color(1f, .22f, .015f); barrier.GetComponent<Renderer>().sharedMaterial = orange;
                SceneManager.MoveGameObjectToScene(barrier, scene);
            }
        }

        private static void ConfigureCabCamera(Scene scene, GameObject vehicle)
        {
            RCCP_HoodCamera hood = vehicle.GetComponentInChildren<RCCP_HoodCamera>(true);
            if (hood == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Realistic Car Controller Pro/Prefabs/Cameras/RCCP_HoodCamera.prefab");
                GameObject camera = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                camera.transform.SetParent(vehicle.transform, false);
                hood = camera.GetComponent<RCCP_HoodCamera>();
            }
            hood.transform.localPosition = new Vector3(-.38f, 1.72f, .68f);
            hood.transform.localRotation = Quaternion.identity;
        }

        private static void ConfigureVehicleAccessories(GameObject vehicle, KombiWeatherController weather)
        {
            Light[] lights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject lightObject = new(i == 0 ? "Headlight Left" : "Headlight Right", typeof(Light));
                lightObject.transform.SetParent(vehicle.transform, false);
                lightObject.transform.localPosition = new Vector3(i == 0 ? -.62f : .62f, .78f, 2.05f);
                lightObject.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Spot; light.range = 65f; light.spotAngle = 55f; light.intensity = 2.3f; light.color = new Color(1f, .93f, .78f); light.enabled = false;
                lights[i] = light;
            }
            KombiVehicleAccessories accessories = vehicle.AddComponent<KombiVehicleAccessories>();
            // The source model has no authored wiper mesh or windscreen pivots.
            // Do not display primitive placeholders; proper wipers will be added
            // when suitable geometry is available.
            accessories.Configure(lights, null, null, weather);
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
