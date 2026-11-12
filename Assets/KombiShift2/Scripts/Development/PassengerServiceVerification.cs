#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class KombiPassengerVerification
{
    public static void Visual() { Run();SessionState.SetBool("Kombi.VerifyVisualOnly",true); }
    public static void Run()
    {
        SessionState.SetBool("Kombi.VerifyVisualOnly",false);
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        var bootstrap=UnityEngine.Object.FindFirstObjectByType<RccHiaceTestBootstrap>();bootstrap.enabled=false;
        foreach(var rival in UnityEngine.Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None))
        {
            rival.enabled=false;
            var data=new SerializedObject(rival);data.FindProperty("departureDelay").floatValue=0f;data.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var traffic in UnityEngine.Object.FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None)) traffic.gameObject.SetActive(false);
        new GameObject("Passenger service verification",typeof(PassengerServiceVerificationRunner));
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.isPlaying=true;
        if(!Application.isBatchMode) EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
}

public sealed class PassengerServiceVerificationRunner : MonoBehaviour
{
    [Serializable] class Report { public List<string> passed=new();public List<string> failed=new(); }
    readonly Report report=new();
    float started;
    string output;
    bool done;
    void Start()
    {
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Builds/Expansion2026/PassengerServiceEvidence"));
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(output);started=Time.realtimeSinceStartup;StartCoroutine(Run());
    }
    void Update()
    {
        if(!done&&Time.realtimeSinceStartup-started>240f){report.failed.Add("Verification timed out");Complete(false);}
    }
    void Check(bool condition,string name)
    {
        if(condition){report.passed.Add(name);Debug.Log("KOMBI_SERVICE_CHECK PASS "+name);}
        else{report.failed.Add(name);Debug.LogError("KOMBI_SERVICE_CHECK FAIL "+name);}
    }
    IEnumerator Run()
    {
        yield return null;yield return null;
        var director=FindFirstObjectByType<KombiShiftGameDirector>();
        var car=FindFirstObjectByType<RCCP_CarController>();
        var body=car.GetComponent<Rigidbody>();body.isKinematic=true;
        car.Inputs.OverrideInputs(new RCCP_Inputs { handbrakeInput = 1f });
        var stations=director.Stations;var rivals=director.Rivals;
        Check(stations.Length==7,"Seven expanded route stations configured");
        Check(stations[0].WaitingCount==6&&stations[1].WaitingCount==6&&stations[2].WaitingCount==6,"Expanded route begins with three six-person queues");
        Check(rivals.Length==2,"Two rival kombis present");
        foreach(var item in rivals)item.GetComponent<Rigidbody>().isKinematic=true;
        for(int i=1;i<rivals.Length;i++)rivals[i].gameObject.SetActive(false);
        bool seated=true,rigged=true;
        foreach(var stop in stations)foreach(var person in stop.People){seated&=person.State==PassengerState.Seated;rigged&=person.HasHumanRig;}
        Check(seated&&rigged,"All waiting passengers seated with valid humanoid rigs");
        var first=stations[0];var originalPosition=car.transform.position;
        car.transform.position=first.ServicePoint.position;car.transform.rotation=first.ServicePoint.rotation;
        Check(first.TryReserve(car.transform),"Player can reserve a nearby waiting stop");
        var rival=rivals[0];Vector3 originalRivalPosition=rival.transform.position;rival.transform.position=first.ServicePoint.position+Vector3.forward*3f;
        Check(!first.TryReserve(rival.transform),"Rival cannot reserve passengers already claimed by player");first.Release(car.transform);rival.transform.position=originalRivalPosition;
        // Frame the first bench, player and door for an actual rendered inspection.
        var camera=Camera.main??FindFirstObjectByType<Camera>();
        var follow=camera.GetComponent<KombiShift2.CameraSystem.KombiDriveCamera>();if(follow!=null)follow.enabled=false;
        var seat=first.ArrivalSeats[2];
        camera.transform.position=seat.position+seat.forward*7f+seat.right*4f+Vector3.up*3f;
        camera.transform.LookAt(seat.position+Vector3.up*.8f);
        yield return null;
        CaptureCamera(camera,"01-seated-station.png");
        Vector3 cameraPosition=camera.transform.position;Quaternion cameraRotation=camera.transform.rotation;
        camera.transform.position=seat.position+seat.forward*2.1f+seat.right*2.8f+Vector3.up*1.5f;
        camera.transform.LookAt(seat.position+Vector3.up*.8f);CaptureCamera(camera,"00-bench-closeup.png");
        camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
        float deadline=Time.time+4f;bool walking=false;
        while(Time.time<deadline){foreach(var person in first.People)walking|=person.State==PassengerState.Walking;if(walking)break;yield return null;}
        Check(walking,"Passenger stands and walks toward the doorway");
        Check(first.People.Count(x=>x.State==PassengerState.Walking)==6,"All six passengers walk toward the kombi together");
        yield return new WaitForSeconds(.7f);CaptureCamera(camera,"02-walking-to-kombi.png");
        Time.timeScale=3f;deadline=Time.time+75f;
        while((first.WaitingCount>0||director.Servicing||director.Passengers<6)&&Time.time<deadline)yield return null;
        yield return null;yield return null;
        Check(first.WaitingCount==0&&director.Passengers==6,"Six passengers physically board before fares are credited");
        Check(first.People[0].Boarded&&first.People[0].transform.IsChildOf(car.transform),"Boarded passenger occupies a player cabin seat");
        Check(first.People.Select(x=>x.transform.parent).Distinct().Count()==6,"Concurrent boarding assigns six different cabin seats");
        Check(director.CurrentStop==1,"Route advances after boarding completes");
        if(!Application.isBatchMode)
        {
            if(follow!=null)follow.ResetView();
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-compact-hud.png"));
            yield return new WaitForSeconds(.4f);
            camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
        }
        if(SessionState.GetBool("Kombi.VerifyVisualOnly",false))
        {
            Time.timeScale=1f;
            if(!Application.isBatchMode)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("KombiMainMenu");
                yield return new WaitForSeconds(.8f);
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-menu-cover.png"));
                yield return new WaitForSeconds(.5f);
            }
            Complete(report.failed.Count==0);yield break;
        }
        // Let a real AI drive along waypoints to the next station and collect its queue.
        var next=stations[1];
        var hud=FindFirstObjectByType<KombiNavigationHud>();var route=hud.RoutePoints;
        int nearest=0;float best=float.MaxValue;
        for(int i=0;i<route.Length;i++){float d=(route[i].position-next.ServicePoint.position).sqrMagnitude;if(d<best){best=d;nearest=i;}}
        int begin=Mathf.Max(0,nearest-2);rival.transform.position=route[begin].position+Vector3.up*.08f;
        rival.transform.rotation=Quaternion.LookRotation(route[Mathf.Min(begin+1,route.Length-1)].position-route[begin].position);
        Vector3 rivalStart=rival.transform.position;rival.GetComponent<Rigidbody>().isKinematic=false;rival.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;Physics.SyncTransforms();rival.GetComponent<Rigidbody>().WakeUp();rival.enabled=true;
        deadline=Time.time+110f;
        while(rival.PassengersTaken<6&&Time.time<deadline)yield return null;
        yield return null;yield return null;
        Check(Vector3.Distance(rivalStart,rival.transform.position)>3f,"Rival travels along the route to a waiting stop");
        Debug.Log("KOMBI_RIVAL_DIAGNOSTIC state="+rival.State+" pax="+rival.PassengersTaken+" waiting="+next.WaitingCount+" position="+rival.transform.position+" target="+(rival.Target!=null?rival.Target.name:"none")+" stop="+next.ServicePoint.position);
        var rivalBody=rival.GetComponent<Rigidbody>();
        Debug.Log("KOMBI_RIVAL_PHYSICS sleeping="+rivalBody.IsSleeping()+" velocity="+rivalBody.linearVelocity+" forward="+rival.transform.forward+" blocked="+typeof(RivalKombiAI).GetField("blockedTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(rival));
        foreach(var wheel in rival.GetComponentsInChildren<WheelCollider>()){bool grounded=wheel.GetGroundHit(out var contact);Debug.Log("KOMBI_RIVAL_WHEEL "+wheel.name+" grounded="+grounded+" road="+(grounded?contact.collider.name:"none")+" motor="+wheel.motorTorque+" brake="+wheel.brakeTorque+" steer="+wheel.steerAngle);}
        var query=new object[]{rival.transform.position+Vector3.up,rival.transform.forward,7f,default(RaycastHit)};
        bool clear=(bool)typeof(RivalKombiAI).GetMethod("Clear",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rival,query);
        var obstacle=(RaycastHit)query[3];Debug.Log("KOMBI_RIVAL_SENSOR clear="+clear+" collider="+(obstacle.collider!=null?obstacle.collider.name:"none")+" distance="+obstacle.distance+" normal="+obstacle.normal);
        camera.transform.position=rival.transform.position-rival.transform.forward*8f+Vector3.up*4f;camera.transform.LookAt(rival.transform.position+rival.transform.forward*4f);CaptureCamera(camera,"rival-approach-diagnostic.png");
        foreach(var person in next.People) Debug.Log("KOMBI_PERSON_DIAGNOSTIC "+person.State+" position="+person.transform.position);
        Check(rival.PassengersTaken==6&&next.WaitingCount==0,"Rival boards and removes six waiting passengers");
        Check(director.Passengers==6,"Rival passengers are not credited to player");
        Check(director.CurrentStop==1,"A pending passenger destination keeps an exhausted pickup as a valid drop-off");
        CaptureCamera(camera,"03-rival-service.png");rival.enabled=false;
        // Verify leaving during boarding returns an uncollected person to the bench.
        typeof(KombiShiftGameDirector).GetField("stopIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(director,2);
        var third=stations[2];car.transform.SetPositionAndRotation(third.ServicePoint.position,third.ServicePoint.rotation);
        deadline=Time.time+4f;walking=false;
        while(Time.time<deadline){foreach(var person in third.People)walking|=person.State==PassengerState.Walking;if(walking)break;yield return null;}
        Check(walking,"Next station starts boarding when player stops");
        car.transform.position=originalPosition+Vector3.up*.1f;
        deadline=Time.time+18f;
        while((third.Reserved||third.People[0].State!=PassengerState.Seated)&&Time.time<deadline)yield return null;
        Check(third.WaitingCount==6&&!third.Reserved&&third.People[0].State==PassengerState.Seated,"Driving away cancels boarding and returns passengers to bench");
        foreach(var dimensions in new[]{new Vector2(1600,900),new Vector2(1280,720),new Vector2(900,600)})
        {
            var layout=KombiNavigationHud.GetLayout(dimensions.x,dimensions.y);
            Check(!layout.Map.Overlaps(layout.Speed)&&!layout.Objective.Overlaps(layout.Stats)&&layout.Map.y>layout.Objective.yMax&&layout.Speed.xMax<=dimensions.x,"HUD corners stay separate at "+dimensions);
        }
        var texture=(Texture2D)typeof(KombiNavigationHud).GetField("map",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(hud);
        Check(texture.GetPixel(0,0).a==0&&texture.GetPixel(texture.width/2,texture.height/2).a>0,"Minimap has transparent corners and a circular visible area");
        File.WriteAllBytes(Path.Combine(output,"circular-minimap.png"),texture.EncodeToPNG());
        Check(QualitySettings.shadows==ShadowQuality.Disable&&QualitySettings.shadowDistance==0&&FindObjectsByType<Light>(FindObjectsSortMode.None).All(x=>x.shadows==LightShadows.None),"Game lighting has no cast shadows");
        car.transform.SetPositionAndRotation(third.ServicePoint.position,third.ServicePoint.rotation);
        deadline=Time.time+85f;
        while((third.WaitingCount>0||director.Servicing)&&Time.time<deadline)yield return null;
        yield return null;yield return null;
        Check(director.Passengers==12&&director.CurrentStop==stations.Length-1,"Player can collect the remaining passengers after rival theft");
        typeof(KombiShiftGameDirector).GetField("stopIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(director,stations.Length-1);
        var terminal=stations[stations.Length-1];
        car.transform.SetPositionAndRotation(terminal.ServicePoint.position,terminal.ServicePoint.rotation);
        deadline=Time.time+45f;
        while(!director.Finished&&Time.time<deadline)yield return null;
        Check(director.Finished,"Final terminal completes the shift and starts alighting");
        yield return new WaitForSecondsRealtime(.2f);
        Check(first.WaitingCount==0&&next.WaitingCount==0&&third.WaitingCount==0,"Delivered passengers never reappear in earlier waiting queues");
        Time.timeScale=1f;
        if(!Application.isBatchMode)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("KombiMainMenu");
            yield return new WaitForSeconds(.8f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-menu-cover.png"));
            yield return new WaitForSeconds(.5f);
        }
        Complete(report.failed.Count==0);
    }
    void CaptureCamera(Camera camera,string name)
    {
        var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;target.Release();Destroy(target);Destroy(texture);
    }
    void Complete(bool passed)
    {
        if(done)return;done=true;File.WriteAllText(Path.Combine(output,SessionState.GetBool("Kombi.VerifyVisualOnly",false)?"visual-results.json":"service-results.json"),JsonUtility.ToJson(report,true));
        Debug.Log("KOMBI_PASSENGER_VERIFICATION "+(passed?"PASS":"FAIL"));EditorApplication.Exit(passed?0:1);
    }
}
#endif
