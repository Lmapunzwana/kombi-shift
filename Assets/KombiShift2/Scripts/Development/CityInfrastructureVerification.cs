#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using KombiShift2.Gameplay;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

public static class KombiCityVerification
{
    public static void Both(){SessionState.SetBool("City.VerifyBoth",true);Run();}
    public static void Rush(){KombiChallenge.Selected=KombiStage.RushHour;Begin();}
    public static void Run(){KombiChallenge.Selected=KombiStage.Morning;Begin();}
    static void Begin()
    {
        CityTripSelection.Selected=CityTripCatalog.Load().trips.First(t=>t.id=="mbare");
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        new GameObject("City integration verification",typeof(CityInfrastructureVerification));EditorApplication.isPlaying=true;
    }
}
public sealed class CityInfrastructureVerification : MonoBehaviour
{
    [Serializable] public class Report {public string utc,unity,gpu,stage;public List<string> passed=new(),failed=new();public List<Window> performance=new();}
    [Serializable] public class Window {public string mode;public int traffic,samples;public float meanFrameMs,p95FrameMs,meanDecisionMs,meanPhysicsMs;public int physicsSamples;public string physicsMarker;public long allocatedBytes;}
    static CityInfrastructureVerification owner;
    void Awake(){if(owner!=null&&owner!=this){Destroy(gameObject);return;}owner=this;}
    void OnDestroy(){if(owner==this)owner=null;}
    Report report=new();string output="Docs/Validation/CityInfrastructure";bool done;float started;
    void Start(){if(owner!=this)return;DontDestroyOnLoad(gameObject);started=Time.realtimeSinceStartup;report.utc=DateTime.UtcNow.ToString("o");report.unity=Application.unityVersion;report.gpu=SystemInfo.graphicsDeviceName;report.stage=KombiChallenge.Selected.ToString();StartCoroutine(Run());}
    void Update(){if(!done&&Time.realtimeSinceStartup-started>360){Check(false,"City scenario timed out");Finish();}}
    void Check(bool condition,string name){(condition?report.passed:report.failed).Add(name);Debug.Log("KOMBI_CITY_CHECK "+(condition?"PASS ":"FAIL ")+name);}
    IEnumerator VerifySignalReservations(Rigidbody playerBody)
    {
        var fixture=new GameObject("Traffic signal rule verification");fixture.transform.position=new Vector3(10000,1000,10000);
        var junction=fixture.AddComponent<CityIntersection>();junction.signalised=true;junction.radius=8f;
        float deadline=Time.time+16f;
        while((junction.Amber||junction.AllRed)&&Time.time<deadline)yield return null;
        bool phaseReady=!junction.Amber&&!junction.AllRed;
        Check(phaseReady,"Signal verification reaches a stable green phase");
        Vector3 green=junction.Phase==0?Vector3.right:Vector3.forward;
        Vector3 red=junction.Phase==0?Vector3.forward:Vector3.right;
        Check(junction.MustStop(red)&&!junction.MayEnter(playerBody,red,junction.transform.position+red*16f),"Traffic control rejects entry on red");
        Vector3 left=-Vector3.Cross(Vector3.up,green);
        var lead=new GameObject("Traffic platoon lead",typeof(BoxCollider),typeof(Rigidbody));lead.transform.SetParent(fixture.transform,false);
        lead.transform.position=junction.transform.position-green*12f+left*1.55f;lead.GetComponent<BoxCollider>().size=new Vector3(1.8f,1f,4f);
        var leadBody=lead.GetComponent<Rigidbody>();leadBody.isKinematic=true;leadBody.useGravity=false;
        var follower=new GameObject("Traffic platoon follower",typeof(BoxCollider),typeof(Rigidbody));follower.transform.SetParent(fixture.transform,false);
        follower.transform.position=junction.transform.position-green*21f+left*1.55f;follower.GetComponent<BoxCollider>().size=new Vector3(1.8f,1f,4f);
        var followerBody=follower.GetComponent<Rigidbody>();followerBody.isKinematic=true;followerBody.useGravity=false;
        Physics.SyncTransforms();
        Vector3 exit=junction.transform.position+green*16f;
        bool leadEntered=junction.MayEnter(leadBody,green,exit);
        bool followerEntered=junction.MayEnter(followerBody,green,exit);
        Check(leadEntered&&followerEntered,"Safe same-direction traffic platoons can clear a signal without serial deadlock");
        Destroy(fixture);
        yield return null;
    }
    IEnumerator Run()
    {
        yield return null;yield return null;
        int expectedQueue=KombiChallenge.Selected==KombiStage.RushHour?8:6;
        var graph=FindFirstObjectByType<CityRoadNetwork>();var director=FindFirstObjectByType<KombiShiftGameDirector>();var player=director.Vehicle;
        Check(graph.Nodes.Length>4000,"Existing city graph is loaded in actual gameplay");
        yield return VerifySignalReservations(player.GetComponent<Rigidbody>());
        Check(CityTripSession.Active&&director.Stations.Last().name.Contains("Mbare"),"Selected Trip Market destination controls actual stops");
        Check(director.Stations.Length==CityTripSelection.Selected.stopNodes.Length,"Selected route creates its actual intermediate stations");
        Check(director.Duration>=CityTripSelection.Distance/7f,"Selected trip timer accounts for route distance and stops");
        foreach(var trip in CityTripCatalog.Load().trips)Check(graph.FindPath(trip.originNode,trip.destinationNode).Count>1,"Reachable city trip: "+trip.destination);
        Check(CityTripCatalog.Load().trips.All(t=>t.Fare(1000,KombiStage.RushHour)>=t.Fare(1000,KombiStage.Morning)),"Peak fares and limits apply to configured routes");
        var fleet=FindFirstObjectByType<CityTrafficFleet>();
        yield return new WaitForSeconds(8);
        Check(fleet.ActiveCount>0,"Pooled civilian traffic spawns on the selected city network");
        Check(FindObjectsByType<CityTrafficAgent>().All(a=>a.GetComponentsInChildren<WheelCollider>().Length==4&&(Vector3.Distance(a.transform.position,player.transform.position)>=fleet.fullPhysicsDistance || !a.GetComponent<Rigidbody>().isKinematic)),"Nearby civilian traffic uses physical suspension and solid bodies");
        var camera=Camera.main;var follow=camera.GetComponent<KombiShift2.CameraSystem.KombiDriveCamera>();if(follow!=null)follow.enabled=false;
        camera.transform.position=player.transform.position-player.transform.forward*8+Vector3.up*5;camera.transform.LookAt(player.transform.position+player.transform.forward*10);
        yield return null;Capture(camera,output+"/selected-city-trip.png");
        var oldPosition=player.transform.position;var oldRotation=player.transform.rotation;
        var body=player.GetComponent<Rigidbody>();body.isKinematic=true;
        // Exercise real boarding at the selected origin. No passenger state is mocked.
        var origin=director.Stations[0];player.transform.SetPositionAndRotation(origin.ServicePoint.position,origin.ServicePoint.rotation);
        float deadline=Time.time+70;
        while((director.Passengers<expectedQueue||director.Servicing)&&Time.time<deadline)yield return null;
        Check(director.Passengers==expectedQueue,"Selected trip physically boards its stage-specific queue into unique cabin seats");
        Check(origin.People.Select(p=>p.transform.parent).Distinct().Count()==expectedQueue,"City trip boarding preserves unique seat ownership");
        Check(director.Trips.All(t=>t.destination>t.boardingStation&&t.destination<director.Stations.Length),"Individual destinations belong to selected reachable stops");
        Check(director.Earnings==0,"Expected route gross is not paid before actual delivery");
        var requested=director.Trips.First();var destination=director.Stations[requested.destination];
        player.transform.SetPositionAndRotation(destination.ServicePoint.position-destination.ServicePoint.forward*45,destination.ServicePoint.rotation);
        yield return new WaitForSeconds(.8f);Check(director.Trips.Any(t=>t.requested),"Approaching a selected destination raises an individual drop-off request");
        Check(FindFirstObjectByType<KombiDropoffMarker>()!=null,"Circular stopping marker is integrated into actual gameplay");
        player.transform.SetPositionAndRotation(destination.ServicePoint.position,destination.ServicePoint.rotation);
        deadline=Time.time+70;
        while((director.Delivered==0||director.Servicing)&&Time.time<deadline)yield return null;
        Check(director.Delivered>0&&director.GrossEarnings>0,"Walking through the door and delivering earns actual fractional fares");
        Check(FindObjectsByType<StationPassenger>().Where(p=>p.State==PassengerState.Delivered).All(p=>Mathf.Abs(p.transform.lossyScale.y-1)<.02f),"Passengers regain their original size after leaving the cabin");
        director.SetPaused(true);float clock=director.TimeRemaining;yield return new WaitForSecondsRealtime(.3f);Check(Mathf.Approximately(clock,director.TimeRemaining),"Pause preserves the selected trip timer");director.SetPaused(false);
        // Same loaded scene, fixed traffic pool: full-frequency and bounded-frequency decision windows.
        body.position=oldPosition;body.rotation=oldRotation;
        ProfilerRecorder physics=default;string physicsName="unavailable";
        var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
        foreach(var handle in handles)
        {
            var description=ProfilerRecorderHandle.GetDescription(handle);
            if(description.Name=="Physics.Simulate"||description.Name=="Physics.Processing")
            {physics=ProfilerRecorder.StartNew(description.Category,description.Name,1);physicsName=description.Name;break;}
        }
        foreach(bool optimized in new[]{false,true})
        {
            foreach(var agent in FindObjectsByType<CityTrafficAgent>())agent.decisionInterval=optimized?.12f:.02f;
            var culling=FindFirstObjectByType<CityDistanceCulling>();if(culling!=null)culling.optimize=optimized;
            var samples=new List<float>();double physicsTotal=0;int physicsSamples=0;float end=Time.realtimeSinceStartup+15;
            while(Time.realtimeSinceStartup<end){samples.Add(Time.unscaledDeltaTime*1000);if(physics.Valid&&physics.LastValue>0){physicsTotal+=physics.LastValue;physicsSamples++;}yield return null;}
            samples.Sort();var agents=FindObjectsByType<CityTrafficAgent>();
            report.performance.Add(new Window{mode=optimized?"AI 120ms + city distance culling":"comparison AI 20ms + full city visibility",traffic=agents.Length,samples=samples.Count,meanFrameMs=samples.Average(),p95FrameMs=samples[Math.Min(samples.Count-1,(int)(samples.Count*.95f))],meanDecisionMs=agents.Length>0?agents.Average(a=>a.DecisionMilliseconds):0,allocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),physicsSamples=physicsSamples,physicsMarker=physicsName,meanPhysicsMs=physicsSamples>0?(float)(physicsTotal/physicsSamples/1000000):-1});
        }
        Check(report.performance.All(p=>p.samples>20&&p.meanFrameMs>0),"Actual city traffic frame time, AI decision cost and memory were sampled");
        if(physics.Valid)physics.Dispose();
        var terminal=director.Stations.Last();player.transform.SetPositionAndRotation(terminal.ServicePoint.position,terminal.ServicePoint.rotation);
        deadline=Time.time+85;while(!director.Finished&&Time.time<deadline)yield return null;
        Check(director.Finished&&director.Success,"Selected "+KombiChallenge.Selected+" trip completes after actual passenger alighting");
        if(SessionState.GetBool("City.VerifyBoth",false)&&KombiChallenge.Selected==KombiStage.Morning)
        {
            WriteReport();KombiChallenge.Selected=KombiStage.RushHour;report=new Report{utc=DateTime.UtcNow.ToString("o"),unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,stage=KombiStage.RushHour.ToString()};
            SceneManager.LoadScene("KombiShift_FirstShift");yield return null;StartCoroutine(Run());yield break;
        }
        CityTripSelection.Selected=null;Finish();
    }
    void Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;target.Release();Destroy(target);Destroy(image);
    }
    void WriteReport(){Directory.CreateDirectory(output);File.WriteAllText(output+(report.stage=="RushHour"?"/city-service-rush.json":"/city-service-results.json"),JsonUtility.ToJson(report,true));}
    void Finish(){if(done)return;done=true;Directory.CreateDirectory(output);WriteReport();Debug.Log("KOMBI_CITY_VERIFICATION "+(report.failed.Count==0?"PASS":"FAIL"));EditorApplication.Exit(report.failed.Count==0?0:1);}
}
#endif
