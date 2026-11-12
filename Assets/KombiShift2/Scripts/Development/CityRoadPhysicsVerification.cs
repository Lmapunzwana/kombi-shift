#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CityRoadPhysicsVerification : MonoBehaviour
{
    [Serializable] public class Trial {public float startSpeed,travel,peakVerticalSpeed,peakRollPitch,contactDepression,healthLost,peakWheelForce;public int impacts,contacts;}
    [Serializable] public class Report {public string unity,utc;public List<Trial> trials=new();public List<string> passed=new(),failed=new();}
    Report report=new();bool done;float began;
    public static void Run()
    {
        CityTripSelection.Selected=null;KombiChallenge.Selected=KombiStage.Morning;
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        foreach(var traffic in UnityEngine.Object.FindObjectsByType<KombiTrafficVehicle>())traffic.gameObject.SetActive(false);
        UnityEngine.Object.FindFirstObjectByType<CityTrafficFleet>().enabled=false;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        new GameObject("Actual city pothole physics verification",typeof(CityRoadPhysicsVerification));EditorApplication.isPlaying=true;
    }
    void Start(){began=Time.realtimeSinceStartup;report.unity=Application.unityVersion;report.utc=DateTime.UtcNow.ToString("o");StartCoroutine(Scenario());}
    void Update(){if(!done&&Time.realtimeSinceStartup-began>90){Check(false,"Physical pothole scenario timed out");Finish();}}
    void Check(bool value,string name){(value?report.passed:report.failed).Add(name);Debug.Log("KOMBI_CITY_PHYSICS "+(value?"PASS ":"FAIL ")+name);}
    IEnumerator Scenario()
    {
        yield return null;yield return null;
        var director=FindFirstObjectByType<KombiShiftGameDirector>();var car=director.Vehicle;var body=car.GetComponent<Rigidbody>();
        foreach(var rival in director.Rivals){rival.enabled=false;rival.GetComponent<Rigidbody>().isKinematic=true;}
        var input=FindFirstObjectByType<RccHiaceTestBootstrap>();input.verificationInput=true;input.verificationThrottle=input.verificationBrake=input.verificationSteering=0;
        var road=FindObjectsByType<CityRoadSurface>().First(r=>r.condition==RoadCondition.Severe&&r.potholes.Length>0);var hole=road.potholes[0];
        Check(Physics.Raycast(hole.centre+Vector3.up*.5f,Vector3.down,out var contact,2f)&&hole.centre.y-contact.point.y>.1f,"Pothole has an actual depressed collision surface");
        var wheels=car.GetComponentsInChildren<WheelCollider>();var wear=car.GetComponent<KombiRoadWear>();
        foreach(float speed in new[]{3f,12f})
        {
            body.isKinematic=true;var forward=hole.forward;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);
            body.position=hole.centre-forward*4+right*.6f+Vector3.up*.14f;body.rotation=Quaternion.LookRotation(forward);Physics.SyncTransforms();
            body.isKinematic=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            input.verificationPark=true;yield return new WaitForSeconds(2);input.verificationPark=false;
            Vector3 initialUp=car.transform.up;body.linearVelocity=forward*speed;
            var trial=new Trial{startSpeed=speed};float health=director.Condition.Health;int impacts=wear.RecordedImpacts;var start=body.position;float end=Time.time+5;
            while(Time.time<end)
            {
                trial.peakVerticalSpeed=Mathf.Max(trial.peakVerticalSpeed,Mathf.Abs(body.linearVelocity.y));trial.peakRollPitch=Mathf.Max(trial.peakRollPitch,Vector3.Angle(car.transform.up,initialUp));
                foreach(var wheel in wheels)if(wheel.GetGroundHit(out var hit)){trial.contacts++;trial.peakWheelForce=Mathf.Max(trial.peakWheelForce,hit.force);if(hit.collider==road.GetComponent<Collider>())trial.contactDepression=Mathf.Max(trial.contactDepression,hole.centre.y-hit.point.y);}
                if(Vector3.Dot(body.position-hole.centre,forward)>5)break;
                yield return new WaitForFixedUpdate();
            }
            trial.travel=Vector3.Dot(body.position-start,forward);trial.healthLost=health-director.Condition.Health;trial.impacts=wear.RecordedImpacts-impacts;report.trials.Add(trial);
            body.isKinematic=true;yield return new WaitForSeconds(1);
        }
        Check(report.trials.All(t=>t.contacts>0&&t.travel>4),"RCC wheels maintain contacts while physically crossing the city defect");
        Check(report.trials.Any(t=>t.contactDepression>.04f),"Wheel contacts follow the road depression rather than a flat overlay");
        Check(report.trials.Any(t=>t.peakRollPitch>.2f&&t.peakVerticalSpeed>.02f),"The unrestrained chassis pitches or rolls with suspension response");
        Check(report.trials[1].impacts>0,"Actual wheel contact records a high-speed pothole impact");
        Check(report.trials[1].healthLost>report.trials[0].healthLost,"High-speed crossing causes more cumulative wear than slow crossing");
        Check(report.trials.All(t=>t.peakVerticalSpeed<8&&t.peakRollPitch<70),"Pothole interaction remains bounded without launching the kombi");
        Finish();
    }
    void Finish(){if(done)return;done=true;File.WriteAllText("Docs/Validation/CityInfrastructure/pothole-physics.json",JsonUtility.ToJson(report,true));Debug.Log("KOMBI_CITY_PHYSICS_RESULT "+(report.failed.Count==0?"PASS":"FAIL"));EditorApplication.Exit(report.failed.Count==0?0:1);}
}
#endif
