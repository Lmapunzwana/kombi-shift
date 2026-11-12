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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class KombiPassengerSeatingLaneRepair
{
    public static void RunServiceScenario()
    {
        RunBatch();
        KombiPassengerVerification.Run();
    }
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch entry only.");
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        Apply();
        KombiSeatingLanePreview.Inspect();
    }
    [MenuItem("Kombi Shift/Repair Passenger Seats And Left Stops")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=KombiProjectWorkbench.Shift || scene.isDirty)
            throw new InvalidOperationException("Stop Play mode and save the driving scene before repairing it. Unity may stay open.");
        string backup="Review/BeforeSeatingLane-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);File.Copy(scene.path,backup+"/KombiShift_FirstShift.unity");
        var route=Object.FindFirstObjectByType<KombiNavigationHud>().RoutePoints;
        var report=new System.Text.StringBuilder("Passenger seating and left-lane scene repair\n");
        foreach(var station in Object.FindObjectsByType<PassengerStation>(FindObjectsSortMode.None))
        {
            var service=station.ServicePoint;var group=service.parent;
            var people=station.People ?? Array.Empty<StationPassenger>();
            int index=Enumerable.Range(0,route.Length).OrderBy(i=>(route[i].position-service.position).sqrMagnitude).First();
            Vector3 forward=route[Mathf.Min(index+1,route.Length-1)].position-route[Mathf.Max(index-1,0)].position;
            forward.y=0;forward.Normalize();Vector3 left=-Vector3.Cross(Vector3.up,forward);
            var wood=group.GetComponentsInChildren<Renderer>().FirstOrDefault(x=>x.name=="Bench seat")?.sharedMaterial;
            var metal=group.GetComponentsInChildren<Renderer>().FirstOrDefault(x=>x.name=="Shelter post")?.sharedMaterial;
            var roof=group.GetComponentsInChildren<Renderer>().FirstOrDefault(x=>x.name=="Shelter roof")?.sharedMaterial;
            string label=group.GetComponentInChildren<TextMesh>()?.text ?? station.name;
            foreach(var person in people)if(person!=null)person.transform.SetParent(group,true);
            // Only replace station-owned furniture and old anchors; preserve passengers and service references.
            foreach(Transform child in group.Cast<Transform>().ToArray())
                if(child!=service && child.GetComponent<StationPassenger>()==null &&
                    (child.name.StartsWith("Bench") || child.name.StartsWith("Shelter") || child.name=="Station name"))
                    Object.DestroyImmediate(child.gameObject);
            group.SetPositionAndRotation(Ground(route[index].position+left*5.5f),Quaternion.LookRotation(-left));
            service.SetPositionAndRotation(Ground(route[index].position+left*1.55f),Quaternion.LookRotation(forward));
            var seats=new Transform[12];
            for(int bench=0;bench<2;bench++)
            {
                float centre=bench==0?-2.6f:2.6f;
                Cube(group,"Bench seat",new Vector3(centre,.46f,0),new Vector3(4.5f,.09f,.5f),wood);
                Cube(group,"Bench back",new Vector3(centre,.8f,-.24f),new Vector3(4.5f,.55f,.07f),wood);
                foreach(float x in new[]{centre-1.8f,centre+1.8f})
                {
                    Cube(group,"Bench support",new Vector3(x,.23f,0),new Vector3(.12f,.46f,.42f),metal);
                    Cube(group,"Shelter post",new Vector3(x,1.17f,-.6f),new Vector3(.1f,2.34f,.1f),metal);
                }
                Cube(group,"Shelter roof",new Vector3(centre,2.35f,-.2f),new Vector3(5f,.12f,1.6f),roof);
                var sign=new GameObject("Station name",typeof(TextMesh));sign.transform.SetParent(group,false);
                sign.transform.localPosition=new Vector3(centre,1.9f,-.55f);sign.transform.localRotation=Quaternion.Euler(0,180,0);
                var text=sign.GetComponent<TextMesh>();text.text=label;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=42;text.characterSize=.055f;text.color=new Color(.91f,.89f,.8f);
                for(int n=0;n<6;n++)
                {
                    var seat=new GameObject("Bench Seat "+(bench*6+n+1)).transform;seat.SetParent(group,false);
                    seat.localPosition=new Vector3(centre+(n-2.5f)*.68f,0,0);seats[bench*6+n]=seat;
                }
            }
            for(int n=0;n<people.Length;n++)if(people[n]!=null)
            {
                people[n].transform.SetParent(seats[n],false);people[n].transform.localPosition=Vector3.zero;people[n].transform.localRotation=Quaternion.identity;
                people[n].SetWaitingSeat(seats[n]);people[n].PoseSeatedPreview();EditorUtility.SetDirty(people[n]);
            }
            station.Configure(people,service,seats,station.Terminal);EditorUtility.SetDirty(station);
            report.AppendLine(station.name+": 12 supported bench positions, left service offset "+Vector3.Dot(service.position-route[index].position,left).ToString("F2")+"m, waiting "+people.Length);
        }
        foreach(var cabin in Object.FindObjectsByType<KombiPassengerCabin>(FindObjectsSortMode.None))
        { RepairCabin(cabin.gameObject);report.AppendLine(cabin.name+": 12 cabin seats"); }
        foreach(var rival in Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None))
        {var data=new SerializedObject(rival);data.FindProperty("profile").FindPropertyRelative("allowOvertaking").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();}
        const string prefabPath="Assets/KombiShift2/Prefabs/Vehicles/Player/Hiace_Player.prefab";
        File.Copy(prefabPath,backup+"/Hiace_Player.prefab");
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try {RepairCabin(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
        finally {PrefabUtility.UnloadPrefabContents(prefab);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Docs/Validation/SeatingLane");File.WriteAllText("Docs/Validation/SeatingLane/repair-layout.txt",report.ToString());
        Debug.Log("KOMBI_SEATING_LANE_REPAIR_SAVED\n"+report);
    }
    public static void RepairCabin(GameObject vehicle)
    {
        var old=vehicle.transform.Find("Passenger Cabin");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Passenger Cabin").transform;root.SetParent(vehicle.transform,false);
        var door=new GameObject("Passenger Doorway").transform;door.SetParent(root,false);door.localPosition=new Vector3(-1.25f,.08f,.5f);
        var seats=new Transform[KombiPassengerCabin.SeatCount];
        for(int i=0;i<seats.Length;i++){seats[i]=new GameObject("Cabin Seat "+(i+1)).transform;seats[i].SetParent(root,false);seats[i].localPosition=KombiPassengerCabin.SeatLocalPosition(i);seats[i].localScale=Vector3.one*KombiPassengerCabin.PassengerScale;}
        var cabin=vehicle.GetComponent<KombiPassengerCabin>()??vehicle.AddComponent<KombiPassengerCabin>();cabin.Configure(door,seats);EditorUtility.SetDirty(cabin);
    }
    static Vector3 Ground(Vector3 point)
    {
        var hits=Physics.RaycastAll(point+Vector3.up*1.5f,Vector3.down,5f,~0,QueryTriggerInteraction.Ignore);
        var floors=hits.Where(x=>x.rigidbody==null && x.normal.y>.7f && x.collider.GetComponentInParent<PassengerStation>()==null && !x.collider.name.StartsWith("Shelter") && !x.collider.name.StartsWith("Bench")).OrderByDescending(x=>x.point.y).ToArray();
        if(floors.Length==0)throw new InvalidOperationException("No pavement or road at "+point);
        return new Vector3(point.x,floors[0].point.y+.025f,point.z);
    }
    static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.SetParent(parent,false);cube.transform.localPosition=position;cube.transform.localScale=scale;cube.GetComponent<Renderer>().sharedMaterial=material;}
}
