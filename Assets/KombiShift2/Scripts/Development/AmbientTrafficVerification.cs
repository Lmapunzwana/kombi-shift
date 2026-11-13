#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using KombiShift2.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KombiAmbientTrafficVerification
{
    public static void Run()=>Begin(KombiStage.Morning);
    public static void Rush()=>Begin(KombiStage.RushHour);
    static void Begin(KombiStage stage)
    {
        KombiChallenge.Selected=stage;CityTripSelection.Selected=CityTripCatalog.Load().trips.First(t=>t.id=="mbare");
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
        var verifier=new GameObject("Ambient traffic soak verification").AddComponent<AmbientTrafficVerification>();verifier.stage=stage;EditorApplication.isPlaying=true;
    }
}
public sealed class AmbientTrafficVerification:MonoBehaviour
{
    public KombiStage stage;
    [Serializable] sealed class WaitRecord {public string vehicle,junction,reason;public float seconds;public int node;public Vector3 position;public bool kinematic;}
    [Serializable] sealed class Report
    {
        public string utc,stage;public int requestedFleet,peakActive,junctionEntries,redLightEntries,stopSignViolations,overThirtySecondJunctionWaits,replans;
        public float simulatedSeconds,realSeconds,meanDecisionMilliseconds,meanFrameMilliseconds,throughputEntriesPerMinute;
        public bool baselineAvailable;public string baselineNote;public List<WaitRecord>worstWaits=new();public List<string>failed=new();
    }
    readonly Dictionary<CityTrafficAgent,float> stoppedSince=new();readonly Dictionary<CityTrafficAgent,float>worst=new();Report report=new();float realStart,frameTotal;int frames;const float Duration=600f;
    void Start(){DontDestroyOnLoad(gameObject);StartCoroutine(Run());}
    IEnumerator Run()
    {
        KombiChallenge.Selected=stage;CityTripSelection.Selected=CityTripCatalog.Load().trips.First(t=>t.id=="mbare");
        AudioListener.pause=true;foreach(var source in FindObjectsByType<AudioSource>())source.enabled=false;foreach(var animator in FindObjectsByType<Animator>())animator.enabled=false;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
        realStart=Time.realtimeSinceStartup;report.utc=DateTime.UtcNow.ToString("o");report.stage=stage.ToString();report.baselineAvailable=false;report.baselineNote="No pre-Phase-3 executable baseline was retained; current throughput is reported without fabricating a comparison.";
        Time.timeScale=1f;Time.fixedDeltaTime=.02f;yield return null;var fleet=FindFirstObjectByType<CityTrafficFleet>();report.requestedFleet=stage==KombiStage.RushHour?fleet.rushVehicles:fleet.morningVehicles;
        float warmupDeadline=Time.realtimeSinceStartup+30f;while(fleet.ActiveCount<report.requestedFleet&&Time.realtimeSinceStartup<warmupDeadline)yield return null;
        var director=FindFirstObjectByType<KombiShiftGameDirector>();if(director!=null)director.enabled=false;
        Time.timeScale=50f;Time.fixedDeltaTime=.25f;Time.maximumDeltaTime=.5f;
        float start=Time.time,nextCheckpoint=60f;
        while(Time.time-start<Duration)
        {
            var agents=FindObjectsByType<CityTrafficAgent>();report.peakActive=Mathf.Max(report.peakActive,fleet.ActiveCount);
            foreach(var agent in agents)
            {
                if(!agent.gameObject.activeInHierarchy){stoppedSince.Remove(agent);continue;}
                bool atJunction=agent.AtJunction;
                if(atJunction&&agent.JunctionWaitSeconds>0){float wait=agent.JunctionWaitSeconds;if(!worst.TryGetValue(agent,out float old)||wait>old)worst[agent]=wait;}
                else stoppedSince.Remove(agent);
            }
            if(Time.time-start>=nextCheckpoint){Debug.Log($"KOMBI_AMBIENT_TRAFFIC_PROGRESS stage={stage} simulated={Time.time-start:F1} active={fleet.ActiveCount}");nextCheckpoint+=60f;}
            yield return null;
        }
        Time.timeScale=1f;Time.fixedDeltaTime=.02f;Time.maximumDeltaTime=.3333333f;float frameEnd=Time.realtimeSinceStartup+5f;while(Time.realtimeSinceStartup<frameEnd){frameTotal+=Time.unscaledDeltaTime*1000f;frames++;yield return null;}
        var all=FindObjectsByType<CityTrafficAgent>();report.simulatedSeconds=Time.time-start;report.realSeconds=Time.realtimeSinceStartup-realStart;report.meanFrameMilliseconds=frames>0?frameTotal/frames:0;report.meanDecisionMilliseconds=fleet.MeanDecisionMilliseconds;
        report.junctionEntries=all.Sum(x=>x.JunctionEntries);report.redLightEntries=all.Sum(x=>x.RedLightEntries);report.stopSignViolations=all.Sum(x=>x.StopSignViolations);report.replans=all.Sum(x=>x.Replans);report.throughputEntriesPerMinute=report.junctionEntries/(Duration/60f);
        report.overThirtySecondJunctionWaits=worst.Count(x=>x.Value>30f);report.worstWaits=worst.OrderByDescending(x=>x.Value).Take(5).Select(x=>new WaitRecord{vehicle=x.Key.name,junction=x.Key.JunctionId,reason=x.Key.DenialReason,seconds=x.Value,node=x.Key.CurrentNode,position=x.Key.transform.position,kinematic=x.Key.GetComponent<Rigidbody>().isKinematic}).ToList();
        if(report.peakActive<report.requestedFleet)report.failed.Add("Fleet never reached the requested stage count.");if(report.overThirtySecondJunctionWaits>0)report.failed.Add("At least one vehicle waited over 30 seconds at a junction.");if(report.redLightEntries>0)report.failed.Add("Ambient traffic entered on red.");if(report.stopSignViolations>0)report.failed.Add("Ambient traffic failed a full STOP.");
        Directory.CreateDirectory("Docs/Validation/CityInfrastructure");string suffix=report.stage=="RushHour"?"rush":"morning";File.WriteAllText("Docs/Validation/CityInfrastructure/ambient-traffic-"+suffix+".json",JsonUtility.ToJson(report,true)+"\n");
        Debug.Log("KOMBI_AMBIENT_TRAFFIC_SOAK "+JsonUtility.ToJson(report));EditorApplication.Exit(report.failed.Count==0?0:1);
    }
}
#endif
