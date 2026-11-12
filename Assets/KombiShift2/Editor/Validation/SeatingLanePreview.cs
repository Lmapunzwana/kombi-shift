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
using Object=UnityEngine.Object;

// Full authored-scene inspection and disposable visual staging; never saves staging into the game.
public static class KombiSeatingLanePreview
{
    public static void RunServiceScenario()
    {
        RunBatch();
        KombiPassengerVerification.Run();
    }
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        Inspect();
    }
    [MenuItem("Kombi Shift/Inspect Passenger Seats And Left Stops")]
    public static void Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before inspecting the saved layout.");
        const string folder="Docs/Validation/SeatingLane";Directory.CreateDirectory(folder);
        Physics.SyncTransforms();
        var route=Object.FindFirstObjectByType<KombiNavigationHud>().RoutePoints;
        var stations=Object.FindObjectsByType<PassengerStation>(FindObjectsSortMode.None);
        var actors=Object.FindObjectsByType<StationPassenger>(FindObjectsSortMode.None).GroupBy(x=>x.Persona).Select(x=>x.First()).ToArray();
        var report=new System.Text.StringBuilder();
        foreach(var station in stations)
        {
            int i=Enumerable.Range(0,route.Length).OrderBy(n=>(route[n].position-station.ServicePoint.position).sqrMagnitude).First();
            var f=route[Math.Min(i+1,route.Length-1)].position-route[Math.Max(i-1,0)].position;f.y=0;
            float left=Vector3.Dot(station.ServicePoint.position-route[i].position,RivalKombiAI.LeftOffset(f,1));
            if(left<1f || left>2.5f)throw new InvalidOperationException("Stop is outside left lane: "+station.name);
            int supported=0;
            foreach(var seat in station.ArrivalSeats)
            {
                bool bench=Physics.RaycastAll(seat.position+Vector3.up*.53f,Vector3.down,.1f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider.name=="Bench seat");
                if(!bench)throw new InvalidOperationException("Unsupported bench position: "+station.name+"/"+seat.name);
                supported++;
            }
            report.AppendLine(station.name+": left lane "+left.ToString("F2")+"m; supported positions "+supported);
        }
        foreach(var cabin in Object.FindObjectsByType<KombiPassengerCabin>(FindObjectsSortMode.None))
        {
            if(cabin.Seats.Length!=12 || cabin.Seats.Distinct().Count()!=12)throw new InvalidOperationException("Cabin must have 12 unique seats.");
            report.AppendLine(cabin.name+": twelve unique cabin positions");
        }
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var light=new GameObject("Preview light",typeof(Light));SceneManager.MoveGameObjectToScene(light,preview);
            light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);
            var cameraObject=new GameObject("Preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,preview);
            var camera=cameraObject.GetComponent<Camera>();camera.scene=preview;camera.backgroundColor=new Color(.2f,.25f,.3f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=45;camera.nearClipPlane=.03f;camera.farClipPlane=100;
            // Clone station furniture and its actual waiting avatars into an isolated preview scene.
            var terminal=stations.First(x=>x.Terminal);
            var group=Object.Instantiate(terminal.ServicePoint.parent.gameObject);SceneManager.MoveGameObjectToScene(group,preview);group.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            for(int i=0;i<terminal.ArrivalSeats.Length;i++)
            {
                var actor=Object.Instantiate(actors[i%actors.Length].gameObject);SceneManager.MoveGameObjectToScene(actor,preview);
                actor.transform.SetParent(group.transform,false);actor.transform.localPosition=terminal.ArrivalSeats[i].localPosition;actor.transform.localRotation=Quaternion.identity;
                actor.GetComponent<StationPassenger>().PoseSeatedPreview();
            }
            Shot(camera,new Vector3(8,4,9),new Vector3(0,.8f,0),folder+"/terminal-supported-benches.png");
            Object.DestroyImmediate(group);
            var player=Object.FindFirstObjectByType<RCCP_CarController>();
            var bus=Object.Instantiate(player.gameObject);SceneManager.MoveGameObjectToScene(bus,preview);bus.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            foreach(var behaviour in bus.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
            var seats=bus.GetComponent<KombiPassengerCabin>().Seats;
            var passengerBounds=new Bounds[12];
            for(int i=0;i<12;i++)
            {
                var actor=Object.Instantiate(actors[i%actors.Length].gameObject);SceneManager.MoveGameObjectToScene(actor,preview);
                actor.transform.SetParent(seats[i],false);actor.transform.localPosition=Vector3.zero;actor.transform.localRotation=Quaternion.identity;
                var person=actor.GetComponent<StationPassenger>();
                typeof(StationPassenger).GetProperty("State").SetValue(person,PassengerState.Onboard);
                person.PoseSeatedPreview();
                bool first=true;Bounds bounds=default;
                foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=new Mesh();skin.BakeMesh(mesh);
                    foreach(var vertex in mesh.vertices)
                    {var p=bus.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                    Object.DestroyImmediate(mesh);
                }
                passengerBounds[i]=bounds;report.AppendLine("Cabin actor "+i+" "+person.Persona.name+": "+bounds.ToString("F3"));
                if(bounds.min.x<-.6f || bounds.max.x>.6f || bounds.min.z< -1.55f || bounds.max.z>1f || bounds.min.y<.3f || bounds.max.y>1.52f)
                    throw new InvalidOperationException("Actor extends beyond cabin envelope: "+i+" "+bounds);
            }
            for(int i=0;i<12;i++)for(int j=i+1;j<12;j++)
                if(passengerBounds[i].Intersects(passengerBounds[j]))throw new InvalidOperationException("Passenger mesh bounds overlap: "+i+" and "+j);
            Shot(camera,new Vector3(4,2,-6),new Vector3(0,.8f,-.5f),folder+"/full-cabin-exterior.png");
            foreach(var renderer in bus.GetComponentsInChildren<Renderer>())if(renderer.GetComponentInParent<StationPassenger>()==null)renderer.enabled=false;
            Shot(camera,new Vector3(3,4,-5),new Vector3(0,.7f,-.5f),folder+"/full-cabin-seat-spacing.png");
            Object.DestroyImmediate(bus);
            report.AppendLine("All 12 baked avatar bounds are disjoint and inside the cabin envelope. Preview staging was discarded.");
        }
        finally {EditorSceneManager.ClosePreviewScene(preview);File.WriteAllText(folder+"/scene-inspection.txt",report.ToString());}
        File.WriteAllText(folder+"/scene-inspection.txt",report.ToString());Debug.Log("KOMBI_SEATING_LANE_INSPECTION_PASSED\n"+report);
    }
    static void Shot(Camera camera,Vector3 position,Vector3 look,string path)
    {
        camera.transform.position=position;camera.transform.LookAt(look);
        var target=new RenderTexture(1280,800,24);var active=RenderTexture.active;var previous=camera.targetTexture;
        var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);
        try {camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally {camera.targetTexture=previous;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);}
    }
}
