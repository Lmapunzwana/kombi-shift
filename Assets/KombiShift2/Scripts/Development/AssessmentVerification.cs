using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Opt-in automation: ordinary game launches never create this runner.
public sealed class KombiAssessmentVerification : MonoBehaviour
{
    [Serializable] class Report { public string unity,platform,utc,stage,scope; public List<string> passed=new(),failed=new(); }
    Report report;
    static KombiStage stage;
    float began;
    bool complete;
    public static string LastReport { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CommandLine()
    {
        bool requested = Environment.GetCommandLineArgs().Contains("--kombi-verify") || Application.absoluteURL.Contains("verify=1");
        if (!requested) return;
        stage = Environment.GetCommandLineArgs().Contains("--rush-hour") || Application.absoluteURL.Contains("stage=rush") ? KombiStage.RushHour : KombiStage.Morning;
        KombiChallenge.Selected = stage;
        new GameObject("Assessment automated verification",typeof(KombiAssessmentVerification));
    }
#if UNITY_EDITOR
    public static void Morning() => EditorRun(KombiStage.Morning);
    public static void RushHour() => EditorRun(KombiStage.RushHour);
    static void EditorRun(KombiStage value)
    {
        stage=value; KombiChallenge.Selected=value;
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorApplication.playModeStateChanged += BeginEditorCheck;
        EditorSettings.enterPlayModeOptionsEnabled=true;
        EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.isPlaying=true;
    }
    static void BeginEditorCheck(PlayModeStateChange state)
    {
        if(state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= BeginEditorCheck;
        new GameObject("Assessment automated verification",typeof(KombiAssessmentVerification));
    }
#endif
    void Awake() { DontDestroyOnLoad(gameObject); Application.logMessageReceived += CaptureException; }
    void CaptureException(string message,string trace,LogType type) { if(type==LogType.Exception && report!=null) report.failed.Add("Runtime exception: "+message); }
    void OnDestroy() { Application.logMessageReceived -= CaptureException; }
    void Update() { if(!complete && Time.realtimeSinceStartup-began>180f) { Check(false,"Wall-clock timeout"); Complete(); } }
    IEnumerator Start()
    {
        began=Time.realtimeSinceStartup;
        report=new Report {unity=Application.unityVersion,platform=Application.platform.ToString(),utc=DateTime.UtcNow.ToString("o"),stage=stage.ToString(),scope="Staged automated integration; vehicle positioned at stops; rivals isolated during boarding; not human driving/playtesting"};
        KombiChallenge.Selected=stage;
        if(SceneManager.GetActiveScene().name!="KombiShift_FirstShift") { yield return SceneManager.LoadSceneAsync("KombiShift_FirstShift"); }
        yield return null;yield return null;yield return new WaitForSeconds(.5f);
        var director=FindFirstObjectByType<KombiShiftGameDirector>();
        var car=director != null ? director.Vehicle : null;
        Check(director!=null&&car!=null,"Gameplay scene has director and vehicle");
        if(director==null||car==null){Complete();yield break;}
        Check(director.Stage==stage&&director.Duration==KombiChallenge.Seconds(stage),"Selected stage applies its duration");
        Check(director.MinimumPassengers==(stage==KombiStage.Morning?1:6),"Distinct stage delivery target");
        Check(director.Rivals.Length==2&&director.Rivals.All(x=>Mathf.Approximately(x.Speed,KombiChallenge.RivalSpeed(stage))),"Both rivals use stage-specific speed");
        Check(director.Rivals.All(x=>x.DepartureDelay>=KombiChallenge.RivalDelay(stage)),"Rival head start follows stage rules");
        int pauseEvents=0,outcomes=0;float fares=0;bool outcome=false;
        director.OnPauseChanged+=_=>pauseEvents++;director.OnFareEarned+=amount=>fares+=amount;director.OnShiftFinished+=won=>{outcomes++;outcome=won;};
        director.SetPaused(true);float timer=director.TimeRemaining;
        yield return new WaitForSecondsRealtime(.15f);
        Check(director.TimeRemaining==timer&&Time.timeScale==0,"Pause freezes shift clock");
        director.SetPaused(false);Check(pauseEvents==2&&Time.timeScale==1,"Resume and pause events");
        // Isolate service/outcomes; rival movement is covered by the separate service suite.
        foreach(var rival in director.Rivals) rival.enabled=false;
        foreach(var traffic in FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))traffic.enabled=false;
        var input=FindFirstObjectByType<RccHiaceTestBootstrap>();if(input!=null)input.enabled=false;
        car.Inputs.OverrideInputs(new RCCP_Inputs { handbrakeInput = 1f });
        var stability=car.GetComponent<KombiVehicleStability>();if(stability!=null)stability.enabled=false;
        var body=car.GetComponent<Rigidbody>();body.isKinematic=true;
        var first=director.Stations[0];car.transform.SetPositionAndRotation(first.ServicePoint.position,first.ServicePoint.rotation);
        Time.timeScale=4;
        float deadline=Time.realtimeSinceStartup+60;
        while((director.Passengers<6||director.Servicing)&&!director.Finished&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(director.Passengers==6&&fares==0&&director.Earnings==0,"Six actual seat arrivals create trips; fares deferred until delivery");
        Check(first.People.Select(x=>x.transform.parent).Distinct().Count()==6,"Concurrent passengers occupy distinct seats");
        typeof(KombiShiftGameDirector).GetField("stopIndex",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(director,director.StopCount-1);
        var terminal=director.Stations.Last();car.transform.SetPositionAndRotation(terminal.ServicePoint.position,terminal.ServicePoint.rotation);
        deadline=Time.realtimeSinceStartup+60;
        while(!director.Finished&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(director.Success&&outcome&&outcomes==1,"Target delivery produces one successful outcome event");
        Check(fares==(stage==KombiStage.Morning?12:18),"Completed trips emit stage-configured fare events once");
        Check(first.People.All(x=>x.State==PassengerState.Delivered),"Ending waits until passengers reach terminal seats");
        Check(Time.timeScale==0,"Results freeze world");
        Check(KombiChallenge.Best(stage)>=director.Earnings,"Successful stage score persists");
        // A fresh scene must clear manifests and fail an empty delivery.
        Time.timeScale=1;yield return SceneManager.LoadSceneAsync("KombiShift_FirstShift");yield return null;yield return null;
        director=FindFirstObjectByType<KombiShiftGameDirector>();car=director.Vehicle;
        Check(director.Passengers==0&&director.Earnings==0&&!director.Finished,"Replay starts with clean passenger and fare state");
        foreach(var rival in director.Rivals)rival.enabled=false;
        input=FindFirstObjectByType<RccHiaceTestBootstrap>();if(input!=null)input.enabled=false;
        stability=car.GetComponent<KombiVehicleStability>();if(stability!=null)stability.enabled=false;
        car.GetComponent<Rigidbody>().isKinematic=true;
        // Stage at terminal and point route progress there without pretending it was driven.
        typeof(KombiShiftGameDirector).GetField("stopIndex",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(director,director.StopCount-1);
        var last=director.Stations.Last();car.transform.SetPositionAndRotation(last.ServicePoint.position,last.ServicePoint.rotation);
        deadline=Time.realtimeSinceStartup+5;
        while(!director.Finished&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(director.Finished&&!director.Success,"Empty terminal delivery fails");
        Time.timeScale=1;yield return SceneManager.LoadSceneAsync("KombiShift_FirstShift");yield return null;yield return null;
        director=FindFirstObjectByType<KombiShiftGameDirector>();
        typeof(KombiShiftGameDirector).GetField("timeRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(director,.01f);
        yield return new WaitForSecondsRealtime(.1f);
        Check(director.Finished&&!director.Success,"Timer expiry fails the shift");
        Time.timeScale=1;yield return SceneManager.LoadSceneAsync("KombiMainMenu");yield return null;
        Check(FindFirstObjectByType<KombiMainMenu>()!=null&&Time.timeScale==1,"Return to menu restores running time");
        Complete();
    }
    void Check(bool ok,string label) { if(report==null)return;(ok?report.passed:report.failed).Add(label);Debug.Log("KOMBI_ASSESSMENT_CHECK "+(ok?"PASS ":"FAIL ")+label); }
    void Complete()
    {
        if(complete)return;complete=true;Time.timeScale=1;
        LastReport=JsonUtility.ToJson(report,true);
        string folder=Application.isEditor?Path.GetFullPath("Docs/Validation/Expansion/Regression"):Application.persistentDataPath;
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"integration-"+stage+".json"),LastReport);
        Debug.Log("KOMBI_ASSESSMENT_JSON "+LastReport);
        Debug.Log("KOMBI_ASSESSMENT_RESULT "+(report.failed.Count==0?"PASS":"FAIL"));
#if UNITY_EDITOR
        EditorApplication.Exit(report.failed.Count==0?0:1);
#else
        if(Application.platform!=RuntimePlatform.WebGLPlayer)Application.Quit(report.failed.Count==0?0:1);
#endif
    }
}
