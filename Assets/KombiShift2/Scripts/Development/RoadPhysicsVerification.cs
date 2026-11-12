#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class KombiRoadPhysicsVerification : MonoBehaviour
{
    [Serializable] public class Record
    {
        public string utc,unity,scope;
        public List<string> passed = new(), failed = new();
        public float safeDistance,fastDistance,safeVerticalSpeed,fastVerticalSpeed,safeHealth,fastHealth;
        public int contactSamples;
    }
    readonly Record report = new();
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged+=Begin; EditorApplication.isPlaying=true;
    }
    static void Begin(PlayModeStateChange value)
    { if(value!=PlayModeStateChange.EnteredPlayMode)return; EditorApplication.playModeStateChanged-=Begin; new GameObject("Road physics verification").AddComponent<KombiRoadPhysicsVerification>(); }
    void Check(bool ok,string message) { (ok?report.passed:report.failed).Add(message); Debug.Log("KOMBI_ROAD_PHYSICS "+(ok?"PASS ":"FAIL ")+message); }
    IEnumerator Start()
    {
        report.utc=DateTime.UtcNow.ToString("o");report.unity=Application.unityVersion;
        report.scope="Headless actual RCC suspension/hump physics at staged approach positions and initial velocities; no human driving or rendered comfort claim.";
        yield return new WaitForSeconds(2f);
        var director=FindFirstObjectByType<KombiShiftGameDirector>();var car=director.Vehicle;var body=car.GetComponent<Rigidbody>();
        foreach(var rival in director.Rivals){rival.enabled=false;rival.gameObject.SetActive(false);}
        foreach(var traffic in FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
        FindFirstObjectByType<RccHiaceTestBootstrap>().enabled=false;
        var inputs=new RCCP_Inputs { throttleInput=0,brakeInput=0,handbrakeInput=0,steerInput=0 };car.Inputs.OverrideInputs(inputs);
        var hump=KombiRoadRules.Active.Where(x=>x.rule==RoadRule.Hump).OrderBy(x=>Vector3.Distance(x.transform.position,car.transform.position)).First();
        var wheels=car.GetComponentsInChildren<WheelCollider>();
        for(int trial=0;trial<2;trial++)
        {
            body.isKinematic=true;
            Vector3 position=hump.transform.position-hump.travelDirection.normalized*9f;
            if(Physics.Raycast(position+Vector3.up*3f,Vector3.down,out var ground,10f,~0,QueryTriggerInteraction.Ignore))position.y=ground.point.y+.14f;
            car.transform.SetPositionAndRotation(position,Quaternion.LookRotation(hump.travelDirection));Physics.SyncTransforms();
            body.isKinematic=false;body.linearVelocity=hump.travelDirection.normalized*(trial==0?5f:16f);body.angularVelocity=Vector3.zero;
            Vector3 start=car.transform.position;float peak=0f;float until=Time.time+4f;
            while(Time.time<until)
            {
                peak=Mathf.Max(peak,Mathf.Abs(body.linearVelocity.y));
                foreach(var wheel in wheels)if(wheel.GetGroundHit(out _))report.contactSamples++;
                yield return new WaitForFixedUpdate();
            }
            float distance=Vector3.Dot(car.transform.position-start,hump.travelDirection.normalized);
            if(trial==0){report.safeDistance=distance;report.safeVerticalSpeed=peak;report.safeHealth=director.Condition.Health;}
            else{report.fastDistance=distance;report.fastVerticalSpeed=peak;report.fastHealth=director.Condition.Health;}
        }
        Check(report.safeDistance>10f,"safe-speed RCC vehicle crosses physical hump");
        Check(report.contactSamples>0 && report.safeVerticalSpeed<5f,"safe hump maintains suspension contact without launch velocity");
        Check(report.safeHealth==100f,"safe crossing does not damage vehicle");
        Check(report.fastDistance>10f && report.fastHealth<report.safeHealth,"fast crossing produces suspension-condition consequence");
        Check(Vector3.Dot(car.transform.up,Vector3.up)>.7f,"vehicle remains upright after fast hump");
        Directory.CreateDirectory("Docs/Validation/Expansion");File.WriteAllText("Docs/Validation/Expansion/road-physics.json",JsonUtility.ToJson(report,true));
        EditorApplication.Exit(report.failed.Count==0?0:1);
    }
}
#endif
