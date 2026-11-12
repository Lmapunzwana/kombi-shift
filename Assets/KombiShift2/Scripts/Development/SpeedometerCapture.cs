#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEngine;

public sealed class SpeedometerCapture : MonoBehaviour
{
    [Serializable] sealed class Record
    {
        public string unity,scope="Rendered actual gameplay; controlled RCC acceleration/braking, not a human playtest";
        public float movingSpeed,distance,maximum,average,elapsed,stopped;
        public bool speedChanged,distanceAdvanced,brakingStopped,stoppedTimeAdvanced;
    }
    static SpeedometerCapture active;
    Record record=new();
    string folder;
    RccHiaceTestBootstrap input;
    void Awake(){if(active!=null&&active!=this){Destroy(gameObject);return;}active=this;}
    IEnumerator Start()
    {
        if(active!=this)yield break;
        folder=Path.GetFullPath("Docs/Validation/Speedometer");Directory.CreateDirectory(folder);
        record.unity=Application.unityVersion;
        Application.runInBackground=true;
        yield return null;yield return null;
        var director=FindAnyObjectByType<KombiShiftGameDirector>();
        input=FindAnyObjectByType<RccHiaceTestBootstrap>();
        if(director==null||input==null){Debug.LogError("Speedometer capture requires the driving scene.");yield break;}
        var car=director.Vehicle;
        foreach(var traffic in FindObjectsByType<KombiTrafficVehicle>())traffic.gameObject.SetActive(false);
        // Fleet shares its root with stations, road markings and signals. Keep that root active.
        foreach(var fleet in FindObjectsByType<KombiShift2.City.CityTrafficFleet>()){fleet.StopAllCoroutines();fleet.enabled=false;}
        foreach(var agent in FindObjectsByType<KombiShift2.City.CityTrafficAgent>())agent.gameObject.SetActive(false);
        foreach(var rival in FindObjectsByType<RivalKombiAI>())rival.gameObject.SetActive(false);
        input.verificationInput=true;input.verificationPark=true;
        yield return new WaitForSeconds(3f);
        var gauge=FindAnyObjectByType<KombiSpeedometer>();
        float startDistance=gauge.DistanceMetres;
        input.verificationPark=false;input.verificationThrottle=.75f;
        float deadline=Time.time+12f;
        while((gauge.SpeedKph<38f||gauge.DistanceMetres-startDistance<12f)&&Time.time<deadline)yield return null;
        record.movingSpeed=gauge.SpeedKph;
        record.speedChanged=record.movingSpeed>5f;
        record.distanceAdvanced=gauge.DistanceMetres-startDistance>5f;
        yield return Capture("speedometer-driving.png");
        input.verificationThrottle=0f;input.verificationBrake=1f;input.verificationPark=true;
        deadline=Time.time+8f;
        while(car.absoluteSpeed>.5f&&Time.time<deadline)yield return null;
        record.brakingStopped=car.absoluteSpeed<.5f;
        float before=gauge.StoppedSeconds;
        yield return new WaitForSeconds(2.2f);
        record.stoppedTimeAdvanced=gauge.StoppedSeconds-before>2f;
        record.distance=gauge.DistanceMetres;record.maximum=gauge.MaximumSpeedKph;record.average=gauge.AverageSpeedKph;
        record.elapsed=gauge.ElapsedSeconds;record.stopped=gauge.StoppedSeconds;
        yield return Capture("speedometer-stopped.png");
        File.WriteAllText(Path.Combine(folder,"live-capture.json"),JsonUtility.ToJson(record,true));
        Debug.Log("KOMBI_SPEEDOMETER_CAPTURE "+JsonUtility.ToJson(record));
        // This runner is explicitly opt-in and changes only the disposable Play session.
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(folder,name),texture.EncodeToPNG());Destroy(texture);
    }
    void OnDestroy(){if(active==this)active=null;}
}
#endif
