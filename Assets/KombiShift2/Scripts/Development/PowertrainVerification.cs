#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using KombiShift2.Vehicle;
using KombiShift2.Traffic;
using Object=UnityEngine.Object;
public static class KombiPowertrainVerification
{
 public static void Run(){EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");new GameObject("Powertrain verification",typeof(KombiPowertrainRunner));EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.isPlaying=true;EditorApplication.ExecuteMenuItem("Window/General/Game");}
}
public sealed class KombiPowertrainRunner:MonoBehaviour
{
 [Serializable]class Report{public List<string> passed=new(),failed=new();public float maximumSpeed,maxRpmAt55To65,maxRpmAbove55,maximumEngineVolume,maximumEnginePitch;public List<int> gears=new();public string telemetry="";}
 Report report=new();RccHiaceTestBootstrap input;RCCP_CarController car;bool accelerate,park=true;string folder;
 void Update(){if(input==null)return;input.verificationInput=true;input.verificationThrottle=accelerate?1:0;input.verificationPark=park;}
 IEnumerator Start()
 {
  folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Builds/PowertrainEvidence"));Directory.CreateDirectory(folder);yield return new WaitForSeconds(2);
  input=Object.FindFirstObjectByType<RccHiaceTestBootstrap>();car=Object.FindFirstObjectByType<RCCP_CarController>();
  foreach(var traffic in Object.FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
  foreach(var rival in Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None))rival.gameObject.SetActive(false);
  yield return new WaitForSeconds(2);var engine=car.GetComponentInChildren<RCCP_Engine>();var gearbox=car.GetComponentInChildren<RCCP_Gearbox>();
  Check(engine.maxEngineRPM==5000&&gearbox.shiftUpRPM==3200,"Diesel-range rev limit and earlier automatic shifts apply after registration");
  var audio=new SerializedObject(car.GetComponentInChildren<RCCP_Audio>());var p=audio.GetIterator();
  while(p.NextVisible(true))if(p.propertyPath.StartsWith("engineSounds")&&p.propertyType==SerializedPropertyType.Float){if(p.name=="maxVolume")report.maximumEngineVolume=Mathf.Max(report.maximumEngineVolume,p.floatValue);if(p.name=="maxPitch")report.maximumEnginePitch=Mathf.Max(report.maximumEnginePitch,p.floatValue);}
  Check(report.maximumEngineVolume<=.281f&&report.maximumEngineVolume>0,"Engine layers remain audible at reduced volume");Check(report.maximumEnginePitch<=1.151f,"Engine loop pitch cannot reach the old shrill setting");
  Check(KombiPowertrain.ThrottleFactor(55,false)==1,"Throttle is no longer cut at 55 km/h");
  Check(KombiPowertrain.ThrottleFactor(100,false)>0&&KombiPowertrain.ThrottleFactor(100,false)<1&&KombiPowertrain.ThrottleFactor(105,false)==0,"Higher-speed governor tapers power gradually");
  accelerate=true;park=false;float end=Time.time+15;bool captured=false;float tick=Time.time;
  while(Time.time<end)
  {
   report.maximumSpeed=Mathf.Max(report.maximumSpeed,car.absoluteSpeed);if(!report.gears.Contains(car.currentGear))report.gears.Add(car.currentGear);
   if(car.absoluteSpeed>=55){report.maxRpmAbove55=Mathf.Max(report.maxRpmAbove55,car.engineRPM);if(car.absoluteSpeed<=65)report.maxRpmAt55To65=Mathf.Max(report.maxRpmAt55To65,car.engineRPM);}
   if(Time.time>=tick){report.telemetry+=$"speed={car.absoluteSpeed:F1} gear={car.currentGear+1} rpm={car.engineRPM:F0} throttle={car.throttleInput_V:F2}\n";tick=Time.time+1;}
   if(!captured&&car.absoluteSpeed>65){ScreenCapture.CaptureScreenshot(Path.Combine(folder,"above-55.png"));captured=true;}
   yield return null;
  }
  Check(report.maximumSpeed>70,"Real wheel-collider driving accelerates beyond 55 km/h");Check(report.gears.Count>=3,"Automatic transmission changes through at least three gears");Check(report.maxRpmAt55To65>0&&report.maxRpmAt55To65<4000,"Passing 55 to 65 km/h stays below 4000 RPM");Check(report.maxRpmAbove55<5100,"Higher-speed running remains within the configured engine range");
  accelerate=false;park=true;yield return new WaitForSeconds(5);Check(car.absoluteSpeed<1,"Parking brake still stops the vehicle after the faster run");Check(car.GetComponent<KombiVehicleStability>().Recoveries==0&&Vector3.Dot(car.transform.up,Vector3.up)>.995f,"Higher-speed test stays upright without a recovery reset");
  File.WriteAllText(Path.Combine(folder,"powertrain-results.json"),JsonUtility.ToJson(report,true));Debug.Log("KOMBI_POWERTRAIN_RESULT "+(report.failed.Count==0?"PASS":"FAIL"));EditorApplication.Exit(report.failed.Count==0?0:1);
 }
 void Check(bool ok,string name){(ok?report.passed:report.failed).Add(name);Debug.Log("KOMBI_POWERTRAIN_CHECK "+(ok?"PASS ":"FAIL ")+name);}
}
#endif
