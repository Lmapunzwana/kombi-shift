using System;
using System.IO;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class KombiPassengerSceneUpgrade
{
    const string Generated = "Assets/KombiShift2/Generated/PassengerService";
    const string Prefabs = "Assets/KombiShift2/Prefabs/Passengers";
    static Material wood, metal, roof;

    [MenuItem("Kombi Shift/Upgrade Passenger Stations And Rivals")]
    public static void Bake()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        Directory.CreateDirectory(Generated); Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
        wood = Material("Bench wood",new Color(.32f,.23f,.14f));
        metal = Material("Station metal",new Color(.24f,.28f,.27f));
        roof = Material("Station roof",new Color(.47f,.48f,.43f));
        var director = Object.FindFirstObjectByType<KombiShiftGameDirector>();
        var hud = Object.FindFirstObjectByType<KombiNavigationHud>();
        var data = new SerializedObject(director);
        var stopArray = data.FindProperty("stops");
        Transform[] stops = Enumerable.Range(0,stopArray.arraySize).Select(i => (Transform)stopArray.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Transform[] route = hud.RoutePoints;
        var player = Object.FindFirstObjectByType<RCCP_CarController>();
        foreach (string name in new[] {"Passenger Stations","Competing Kombis"})
        { var old=GameObject.Find(name);if (old!=null) Object.DestroyImmediate(old); }
        var stationsRoot = new GameObject("Passenger Stations").transform;
        var stations = new PassengerStation[stops.Length];
        var actorPrefabs = CreatePassengerPrefabs();
        for (int i=0;i<stops.Length;i++)
        {
            var stop=stops[i];
            foreach (Transform child in stop.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var marker=stop.GetComponent<Renderer>();if (marker!=null) marker.enabled=false;
            int nearest=Nearest(route,stop.position);
            Vector3 forward=(route[Mathf.Min(nearest+1,route.Length-1)].position-route[Mathf.Max(0,nearest-1)].position).normalized;
            forward.y=0;forward.Normalize();Vector3 right=Vector3.Cross(Vector3.up,forward);
            var group=new GameObject(i==stops.Length-1 ? "Mbare Terminal Station" : $"Passenger Station {i+1}").transform;
            group.SetParent(stationsRoot);group.position=stop.position;group.position=new Vector3(group.position.x,route[nearest].position.y+.025f,group.position.z);
            group.rotation=Quaternion.LookRotation(-right);
            int count=i==0?6:3;
            CreateShelter(group,i==stops.Length-1?"MBARE TERMINAL":i==0?"CBD RANK":$"BUS STOP {i+1}");
            var seats=new Transform[i==stops.Length-1?14:Mathf.Max(6,count)];
            for (int n=0;n<seats.Length;n++)
            {
                seats[n]=new GameObject($"Bench Seat {n+1}").transform;seats[n].SetParent(group,false);
                seats[n].localPosition=i==stops.Length-1 ? new Vector3((n%7-3)*.6f,0,-(n/7)*2.1f) : new Vector3((n-(seats.Length-1)*.5f)*.68f,0,0);
            }
            var service=new GameObject("Kombi Service Point").transform;service.SetParent(group);
            service.position=route[nearest].position+right*3.1f;service.rotation=Quaternion.LookRotation(forward);
            var people=i==stops.Length-1?Array.Empty<StationPassenger>():new StationPassenger[count];
            for(int n=0;n<people.Length;n++)
            {
                var person=(GameObject)PrefabUtility.InstantiatePrefab(actorPrefabs[(i*2+n)%actorPrefabs.Length]);
                person.name=$"Waiting Passenger {i+1}-{n+1}";person.transform.SetParent(group,true);
                person.transform.SetPositionAndRotation(seats[n].position,seats[n].rotation);
                var actor=person.GetComponent<StationPassenger>();
                var serialized=new SerializedObject(actor);serialized.FindProperty("waitingSeat").objectReferenceValue=seats[n];serialized.ApplyModifiedPropertiesWithoutUndo();
                actor.PoseSeatedPreview();people[n]=actor;
            }
            var station=stop.GetComponent<PassengerStation>()??stop.gameObject.AddComponent<PassengerStation>();
            station.Configure(people,service,seats,i==stops.Length-1);stations[i]=station;
        }
        AddCabin(player.gameObject);
        var rivalsRoot=new GameObject("Competing Kombis").transform;
        for (int i=0;i<2;i++)
        {
            var rival=CreateRival(player.gameObject,i);rival.transform.SetParent(rivalsRoot,true);
            int nearest=Nearest(route,player.transform.position);
            int index=Mathf.Clamp(nearest-(i+1)*4,0,route.Length-2);
            rival.transform.SetPositionAndRotation(route[index].position+Vector3.up*.08f,Quaternion.LookRotation(route[index+1].position-route[index].position));
            // Separate rivals even when the start of the route has no earlier waypoints.
            rival.transform.position-=rival.transform.forward*(i+1)*9f;
            rival.GetComponent<RivalKombiAI>().Configure(route,stations,18f+i*18f,7.5f+i*.7f);
        }
        foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) renderer.shadowCastingMode=ShadowCastingMode.Off;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        KombiPassengerSeatingLaneRepair.Apply();
        Directory.CreateDirectory("Docs");
        File.WriteAllText("Docs/PASSENGER_UPGRADE.txt",$"Passenger stations: {stations.Length}\nSeated waiting passengers: {stations.Sum(x=>x.WaitingCount)}\nCompeting kombis: 2\nPassenger prefab variants: {actorPrefabs.Length}\nCircular map and compact corner HUD enabled.\n");
        Debug.Log("KOMBI_PASSENGER_SCENE_UPGRADED: seated stations, cabin seats and two rivals saved.");
    }

    static GameObject[] CreatePassengerPrefabs()
    {
        string[] names={"Worker","Executive","Caretaker","teacher","Teenager","saleswoman"};
        string[] models={"builder","executive","caretaker","teacher","teenager","civilian"};
        var result=new GameObject[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var persona=AssetDatabase.LoadAssetAtPath<PassengerPersona>($"Assets/KombiShift2/Data/PassengerPersonas/{names[i]}.asset");
            string modelPath=$"Assets/KombiShift2/Art/Characters/{models[i]}.fbx";
            if(models[i]=="teacher"||models[i]=="civilian")
            {
                string textures=$"Assets/KombiShift2/Art/Characters/Extracted/{models[i]}";
                if(!Directory.Exists(textures))
                {
                    Directory.CreateDirectory(textures);AssetDatabase.Refresh();
                    var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
                    importer.ExtractTextures(textures);AssetDatabase.Refresh();importer.SaveAndReimport();
                }
            }
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var actor=new GameObject(names[i]+" Passenger");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.transform.SetParent(actor.transform,false);
            var animator=visual.GetComponentInChildren<Animator>();
            if(animator==null) animator=visual.AddComponent<Animator>();
            if(animator.avatar==null) animator.avatar=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model)).OfType<Avatar>().FirstOrDefault();
            FixCharacterMaterials(visual,models[i]);
            var renderers=visual.GetComponentsInChildren<Renderer>();
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float factor=1.7f/Mathf.Max(.1f,bounds.size.y);visual.transform.localScale*=factor;
            bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            visual.transform.localPosition+=Vector3.up*(-bounds.min.y);
            var component=actor.AddComponent<StationPassenger>();component.Configure(persona,animator,null);
            if(!component.HasHumanRig)throw new Exception("Humanoid rig unavailable: "+models[i]);
            result[i]=PrefabUtility.SaveAsPrefabAsset(actor,$"{Prefabs}/{names[i]} Passenger.prefab");
            persona.visualPrefab=result[i];if(names[i]=="Executive")persona.personaName="Executive";
            EditorUtility.SetDirty(persona);Object.DestroyImmediate(actor);
        }
        return result;
    }

    static void FixCharacterMaterials(GameObject visual,string model)
    {
        foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(source==null||source.mainTexture!=null)continue;
                string texturePath=null;
                if(model=="builder")texturePath=renderer.name.ToLowerInvariant().Contains("hair")?"Assets/KombiShift2/Prefabs/Gameplay/Ch17_1002_Diffuse.png":"Assets/KombiShift2/Prefabs/Gameplay/Ch17_1001_Diffuse.png";
                if(model=="caretaker")texturePath="Assets/KombiShift2/Art/Characters/Girl01_diffuse.jpg";
                if(model=="civilian")texturePath="Assets/KombiShift2/Art/Characters/Extracted/civilian/Boy01_diffuse.jpg";
                if(model=="teacher")texturePath=renderer.name.Contains("Hair")?"Assets/KombiShift2/Art/Characters/Extracted/teacher/Ch22_1002_Diffuse.png":"Assets/KombiShift2/Art/Characters/Extracted/teacher/Ch22_1001_Diffuse.png";
                if(texturePath==null)continue;
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(texture==null)continue;
                string path=$"{Generated}/{model} {source.name.Replace('/','_')}.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
                material.color=Color.white;material.mainTexture=texture;material.SetFloat("_Glossiness",.18f);materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
    }

    static void AddCabin(GameObject vehicle)
    {
        var old=vehicle.transform.Find("Passenger Cabin");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Passenger Cabin").transform;root.SetParent(vehicle.transform,false);
        var entry=new GameObject("Passenger Doorway").transform;entry.SetParent(root,false);entry.localPosition=new Vector3(-1.25f,.08f,.5f);
        var seats=new Transform[KombiPassengerCabin.SeatCount];
        for(int i=0;i<seats.Length;i++)
        {
            seats[i]=new GameObject($"Cabin Seat {i+1}").transform;seats[i].SetParent(root,false);
            seats[i].localPosition=KombiPassengerCabin.SeatLocalPosition(i);seats[i].localScale=Vector3.one*KombiPassengerCabin.PassengerScale;
        }
        var cabin=vehicle.GetComponent<KombiPassengerCabin>()??vehicle.AddComponent<KombiPassengerCabin>();cabin.Configure(entry,seats);
    }

    static GameObject CreateRival(GameObject player,int index)
    {
        var rival=Object.Instantiate(player);rival.name=index==0?"Rival Kombi — Mbare Express":"Rival Kombi — City Shuttle";
        foreach(var script in rival.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
        foreach(var collider in rival.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach(var audio in rival.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(audio);
        foreach(var light in rival.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);
        foreach(var body in rival.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
        foreach(var particles in rival.GetComponentsInChildren<ParticleSystem>(true)) particles.gameObject.SetActive(false);
        var physics=rival.AddComponent<Rigidbody>();physics.isKinematic=true;physics.useGravity=false;
        var box=rival.AddComponent<BoxCollider>();box.center=new Vector3(0,.85f,0);box.size=new Vector3(1.9f,1.6f,4.5f);
        AddCabin(rival);rival.AddComponent<RivalKombiAI>();
        Color tint=index==0?new Color(.49f,.62f,.50f):new Color(.72f,.61f,.44f);
        foreach(var renderer in rival.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(source==null||!source.HasProperty("_Color"))continue;
                string name=source.name.ToLowerInvariant();
                if(new[]{"glass","window","light","lamp","chrome","wheel","tire","rubber","mirror"}.Any(name.Contains))continue;
                Color color=source.color;if((color.r+color.g+color.b)/3f<.72f)continue;
                string path=$"{Generated}/Rival{index} {source.name.Replace('/','_')}.mat";
                var paint=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(paint==null){paint=new Material(source);AssetDatabase.CreateAsset(paint,path);}
                paint.color=tint;materials[i]=paint;
            }
            renderer.sharedMaterials=materials;
        }
        return rival;
    }
    static int Nearest(Transform[] route,Vector3 point)
    { int best=0;float distance=float.MaxValue;for(int i=0;i<route.Length;i++){float d=(route[i].position-point).sqrMagnitude;if(d<distance){distance=d;best=i;}}return best; }
    static Material Material(string name,Color color)
    {
        string path=$"{Generated}/{name}.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
        material.color=color;return material;
    }
    static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collider=false)
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.SetParent(parent,false);cube.transform.localPosition=position;cube.transform.localScale=scale;
        cube.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.DestroyImmediate(cube.GetComponent<Collider>());
    }
    static void CreateShelter(Transform parent,string name)
    {
        Cube(parent,"Bench seat",new Vector3(0,.46f,0),new Vector3(4.5f,.09f,.5f),wood);
        Cube(parent,"Bench back",new Vector3(0,.8f,-.24f),new Vector3(4.5f,.55f,.07f),wood);
        foreach(float x in new[]{-1.8f,1.8f})
        {Cube(parent,"Bench support",new Vector3(x,.23f,0),new Vector3(.12f,.46f,.42f),metal);Cube(parent,"Shelter post",new Vector3(x,1.17f,-.6f),new Vector3(.1f,2.34f,.1f),metal);}
        Cube(parent,"Shelter roof",new Vector3(0,2.35f,-.2f),new Vector3(5f,.12f,1.6f),roof);
        var sign=new GameObject("Station name",typeof(TextMesh));sign.transform.SetParent(parent,false);sign.transform.localPosition=new Vector3(0,1.9f,-.55f);
        var text=sign.GetComponent<TextMesh>();text.text=name;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=42;text.characterSize=.055f;text.color=new Color(.91f,.89f,.8f);sign.transform.localRotation=Quaternion.Euler(0,180,0);
    }
}
