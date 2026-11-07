#if UNITY_EDITOR
using System;
using Object=UnityEngine.Object;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using KombiShift2.Vehicle;
public static class KombiGroundingVerification
{
 public static void Run(){EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");new GameObject("Grounding verification",typeof(KombiGroundingRunner));EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.isPlaying=true;EditorApplication.ExecuteMenuItem("Window/General/Game");}
}
public sealed class KombiGroundingRunner:MonoBehaviour
{
 [Serializable]class Report{public List<string> passed=new(),failed=new();public float minimumUpright=1,maximumSpeed,maximumAirborneSeconds,maximumWheelMountDrift,peakMotorTorque;public int recoveries;}
 Report result=new();StringBuilder telemetry=new();RCCP_CarController car;RccHiaceTestBootstrap driver;Rigidbody body;WheelCollider[] wheels;Vector3[] mounts;float throttle,steer,brake,airborne;bool park=true,measure,done;string output;float began;
 void Update()
 {
  if(driver!=null){driver.verificationInput=true;driver.verificationThrottle=throttle;driver.verificationSteering=steer;driver.verificationBrake=brake;driver.verificationPark=park;}
  if(!measure||car==null)return;
  foreach(var wheel in wheels)result.peakMotorTorque=Mathf.Max(result.peakMotorTorque,Mathf.Abs(wheel.motorTorque));
  result.minimumUpright=Mathf.Min(result.minimumUpright,Vector3.Dot(car.transform.up,Vector3.up));result.maximumSpeed=Mathf.Max(result.maximumSpeed,car.absoluteSpeed);
  airborne=wheels.All(w=>!w.isGrounded)?airborne+Time.deltaTime:0;result.maximumAirborneSeconds=Mathf.Max(result.maximumAirborneSeconds,airborne);
  for(int i=0;i<wheels.Length;i++)result.maximumWheelMountDrift=Mathf.Max(result.maximumWheelMountDrift,Vector3.Distance(mounts[i],wheels[i].transform.localPosition));
  if(!done&&Time.realtimeSinceStartup-began>220){Check(false,"Verification completed within time limit");Finish();}
 }
 IEnumerator Start()
 {
  began=Time.realtimeSinceStartup;output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Builds/GroundingEvidence"));Directory.CreateDirectory(output);
  yield return new WaitForSeconds(2);car=Object.FindFirstObjectByType<RCCP_CarController>();driver=Object.FindFirstObjectByType<RccHiaceTestBootstrap>();body=car.GetComponent<Rigidbody>();wheels=car.GetComponentsInChildren<WheelCollider>();mounts=wheels.Select(w=>w.transform.localPosition).ToArray();
  yield return new WaitForSeconds(3);Check(wheels.Count(w=>w.isGrounded)==4,"All four wheels settle on the actual city road");
  Check(car.GetComponentsInChildren<Rigidbody>(true).Length==1,"Player has one physics body and no old camera joint");
  var rear=car.GetComponentsInChildren<RCCP_Axle>().First(a=>a.name.Contains("Rear"));Check(rear.isPower,"Rear axle receives engine power");
  Check(car.direction>0,"Parking does not request reverse gear");
  Vector3 start=body.position;Quaternion rotation=body.rotation;
  var director=Object.FindFirstObjectByType<KombiShift2.Gameplay.KombiShiftGameDirector>();
  Vector3 service=director.Stations[0].ServicePoint.position;service.y=Ground(service)+.13f;ResetAt(service,rotation);
  float boardDeadline=Time.time+25;while(director.Passengers<6&&Time.time<boardDeadline)yield return null;
  Check(director.Passengers>=6,"Six passengers board while the kombi uses dynamic physics");
  Check(car.GetComponentsInChildren<Collider>().Length==5&&wheels.Count(w=>w.isGrounded)>=3,"Boarded passengers add no colliders that lift the vehicle");
  ResetAt(start,rotation);yield return new WaitForSeconds(2);measure=true;yield return Shot("01-grounded-at-start");
  park=false;throttle=1;yield return new WaitForSeconds(2);Check(car.absoluteSpeed>5&&result.peakMotorTorque>1,"Throttle powers the wheels and accelerates the kombi");yield return new WaitForSeconds(4);Record("accelerating");
  float heading=car.transform.eulerAngles.y;steer=1;yield return new WaitForSeconds(4);steer=-1;yield return new WaitForSeconds(4);Record("hard turns");
  Check(Mathf.Abs(Mathf.DeltaAngle(heading,car.transform.eulerAngles.y))>3,"Hard steering turns the vehicle");yield return Shot("02-after-hard-turns");
  throttle=steer=0;park=true;yield return new WaitForSeconds(5);Record("braking");Check(car.absoluteSpeed<1,"Handbrake stops the vehicle from city speed");
  Vector3 stopped=body.position;yield return new WaitForSeconds(3);Check(Vector3.Distance(stopped,body.position)<.15f,"Held brake prevents creeping while parked");
  park=false;brake=1;Vector3 reverseStart=body.position;yield return new WaitForSeconds(4);
  Check(car.direction<0&&Vector3.Distance(reverseStart,body.position)>1,"Brake/reverse input drives the kombi backwards");
  brake=0;park=true;yield return new WaitForSeconds(2);
  measure=false;ResetAt(start,rotation);yield return new WaitForSeconds(2);
  Vector3 curbPosition=start+car.transform.forward*5;float y=Ground(curbPosition);var curb=GameObject.CreatePrimitive(PrimitiveType.Cube);curb.name="Verification kerb 12cm";curb.transform.SetPositionAndRotation(new Vector3(curbPosition.x,y+.06f,curbPosition.z),rotation);curb.transform.localScale=new Vector3(5,.12f,1);
  measure=true;throttle=.55f;park=false;yield return new WaitForSeconds(5);throttle=0;park=true;yield return new WaitForSeconds(3);Record("after kerb");
  Check(wheels.Count(w=>w.isGrounded)>=3,"Wheels regain road contact after crossing a 12cm kerb");yield return Shot("03-after-kerb");Destroy(curb);
  measure=false;ResetAt(start,rotation);yield return new WaitForSeconds(2);
  Vector3 obstaclePosition=start+car.transform.forward*8;var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Verification stopped traffic body";obstacle.transform.SetPositionAndRotation(new Vector3(obstaclePosition.x,Ground(obstaclePosition)+.8f,obstaclePosition.z),rotation);obstacle.transform.localScale=new Vector3(1.8f,1.6f,4);var other=obstacle.AddComponent<Rigidbody>();other.isKinematic=true;
  measure=true;throttle=1;park=false;yield return new WaitForSeconds(4);throttle=0;park=true;yield return new WaitForSeconds(3);Record("traffic impact");
  Check(wheels.Count(w=>w.isGrounded)>=3&&body.position.y-start.y<.6f,"Traffic-body collision does not leave the kombi floating");yield return Shot("04-after-traffic-impact");Destroy(obstacle);
  measure=false;result.recoveries=car.GetComponent<KombiVehicleStability>().Recoveries;
  Check(result.minimumUpright>.995f,"Body stays upright during turns, kerb crossing and impact");Check(result.maximumSpeed<111,"Speed stays within the configured emergency ceiling");Check(result.maximumAirborneSeconds<.8f,"No sustained loss of wheel contact");Check(result.maximumWheelMountDrift<.005f,"Wheel collider mounts do not move during steering");Check(result.recoveries==0,"Driving tests complete without recovery teleporting");Finish();
 }
 void ResetAt(Vector3 p,Quaternion q){body.position=p;body.rotation=q;body.linearVelocity=body.angularVelocity=Vector3.zero;throttle=steer=0;park=true;Object.FindFirstObjectByType<KombiShift2.CameraSystem.KombiDriveCamera>().ResetView();}
 float Ground(Vector3 p){float y=p.y-.1f;foreach(var h in Physics.RaycastAll(p+Vector3.up,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))if(h.rigidbody==null&&h.normal.y>.8f&&Mathf.Abs(h.point.y-y)<.5f)y=Mathf.Max(y,h.point.y);return y;}
 IEnumerator Shot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.3f);}
 void Record(string name){telemetry.AppendLine($"{name}: pos={body.position}; angles={body.rotation.eulerAngles}; speed={car.absoluteSpeed}; contacts={wheels.Count(w=>w.isGrounded)}; COM={body.centerOfMass}");foreach(var w in wheels){bool g=w.GetGroundHit(out var h);telemetry.AppendLine($" {w.name} mount={w.transform.localPosition} contact={(g?h.collider.name:"none")} motorTorque={w.motorTorque}");}}
 void Check(bool passed,string message){(passed?result.passed:result.failed).Add(message);Debug.Log("KOMBI_GROUNDING_CHECK "+(passed?"PASS ":"FAIL ")+message);}
 void Finish(){if(done)return;done=true;File.WriteAllText(Path.Combine(output,"grounding-results.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(output,"runtime-audit.txt"),telemetry.ToString());Debug.Log("KOMBI_GROUNDING_RESULT "+(result.failed.Count==0?"PASS":"FAIL"));EditorApplication.Exit(result.failed.Count==0?0:1);}
}
#endif
