#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KombiShift2.CameraSystem;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using KombiShift2.Traffic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class KombiDrivingMenuVerification
{
 public static void Run()
 {
  EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiMainMenu.unity");
  new GameObject("Menu and live physics verification",typeof(KombiDrivingMenuVerificationRunner));
  EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
  EditorApplication.isPlaying=true;EditorApplication.ExecuteMenuItem("Window/General/Game");
 }
}
[DefaultExecutionOrder(5000)]
public sealed class KombiDrivingMenuVerificationRunner:MonoBehaviour
{
 [Serializable]class Report{public List<string> passed=new();public List<string> failed=new();public string physics;}
 Report report=new();string output;float began;bool done;RCCP_CarController car;float throttle;bool hold=true;RccHiaceTestBootstrap driver;
 void Start(){DontDestroyOnLoad(gameObject);output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Builds/Expansion2026/MenuCameraEvidence"));Directory.CreateDirectory(output);began=Time.realtimeSinceStartup;StartCoroutine(Run());}
 void Update()
 {
  if(done)return;
  if(Time.realtimeSinceStartup-began>180){Check(false,"Test timed out");Finish();}
  if(driver!=null){driver.verificationInput=true;driver.verificationThrottle=throttle;driver.verificationPark=hold;}
 }
 void Check(bool value,string name){(value?report.passed:report.failed).Add(name);Debug.Log("KOMBI_MENU_CAMERA_CHECK "+(value?"PASS ":"FAIL ")+name);}
 IEnumerator Shot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.3f);}
 IEnumerator Run()
 {
  yield return new WaitForSeconds(1f);
  var menu=FindFirstObjectByType<KombiMainMenu>();var camera=Camera.main;
  Check(menu!=null&&GameObject.Find("Hiace Showcase")!=null,"Menu displays the actual 3D Hiace");
  Check(menu.GetComponent<AudioSource>()!=null,"Menu has sound feedback");
  Vector3 initialCamera=camera.transform.position;yield return new WaitForSeconds(1f);
  Check(Vector3.Distance(initialCamera,camera.transform.position)>.005f,"Menu camera moves around the live vehicle");
  yield return Shot("01-live-menu");menu.Focus(1);yield return new WaitForSeconds(.25f);
  Check(menu.SelectedItem==1&&menu.Highlight(1)>.9f,"Menu focus animates the selected card");yield return Shot("02-focused-menu");
  menu.Activate(1);Check(menu.ControlsOpen,"How to play opens from its menu action");yield return Shot("03-controls");menu.ClosePanel();
  menu.Activate(2);Check(menu.SettingsOpen,"Settings opens from its menu action");yield return Shot("04-settings");menu.ClosePanel();
  menu.Activate(0);yield return new WaitForSeconds(.12f);Check(menu.Starting&&menu.TransitionProgress>0,"Start action plays a transition before loading");
  float deadline=Time.realtimeSinceStartup+100;
  while((SceneManager.GetActiveScene().name!="KombiShift_FirstShift"||FindFirstObjectByType<RCCP_CarController>()==null)&&Time.realtimeSinceStartup<deadline)yield return null;
  yield return null;car=FindFirstObjectByType<RCCP_CarController>();
  Check(car!=null,"Start shift loads the playable scene");
  if(car==null){Finish();yield break;}
  driver=FindFirstObjectByType<RccHiaceTestBootstrap>();driver.verificationInput=true;driver.verificationPark=true;
  foreach(var c in FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))Debug.Log("KOMBI_CAMERA_AUDIT "+c.name+" enabled="+c.enabled+" depth="+c.depth+" position="+c.transform.position+" parent="+(c.transform.parent!=null?c.transform.parent.name:"root"));
  foreach(var traffic in FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None))traffic.gameObject.SetActive(false);
  foreach(var rival in FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None))rival.gameObject.SetActive(false);
  var body=car.GetComponent<Rigidbody>();var follow=FindFirstObjectByType<KombiDriveCamera>();camera=follow.GetComponent<Camera>();
  Debug.Log("KOMBI_CAMERA_TARGET "+follow.Vehicle.name+" "+follow.Vehicle.position+" follow="+follow.transform.position+" enabled="+camera.enabled);
  Check(camera.enabled&&camera==Camera.main,"Follow camera owns the visible game view");
  Check(FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Only the gameplay camera remains in the city scene");
  Check(!body.isKinematic,"Camera test uses normal dynamic vehicle physics");
  yield return new WaitForSeconds(3f);
  float initialY=car.transform.position.y;
  bool above=true,upright=true;float end=Time.time+4f;
  while(Time.time<end){above&=camera.transform.position.y>car.transform.position.y+.5f;upright&=Vector3.Dot(camera.transform.up,Vector3.up)>.7f;yield return null;}
  Check(above&&upright,"Chase camera stays above the road with a level horizon");
  Check(Vector3.Dot(car.transform.up,Vector3.up)>.95f,"Dynamic kombi stays upright while parked");
  yield return Shot("05-chase-view");
  Vector3 before=camera.transform.position;float beforeDistance=follow.Distance;follow.Orbit(80,20);follow.Zoom(2);yield return new WaitForSeconds(.5f);
  Check(Vector3.Distance(before,camera.transform.position)>1f&&Mathf.Abs(follow.OrbitYaw-80)<.1f,"Orbit control moves the chase camera around the kombi");
  Check(Mathf.Approximately(follow.Distance,Mathf.Clamp(beforeDistance+2f,4.5f,12f)),"Zoom applies its requested delta and clamp with the saved setting");yield return Shot("06-mouse-orbit");
  follow.CycleView();yield return new WaitForSeconds(.4f);Check(follow.CabinView&&camera.transform.position.y>car.transform.position.y+.7f,"Camera switch opens an above-ground driver view");yield return Shot("07-driver-view");
  follow.CycleView();follow.ResetView();
  Quaternion parkedRotation=car.transform.rotation;body.isKinematic=true;car.transform.rotation=Quaternion.Euler(70,parkedRotation.eulerAngles.y,125);follow.ResetView();
  Check(camera.transform.position.y>car.transform.position.y+.5f&&Vector3.Dot(camera.transform.up,Vector3.up)>.7f,"Camera ignores vehicle roll and pitch instead of going under the map");
  car.transform.rotation=parkedRotation;body.isKinematic=false;follow.ResetView();yield return null;
  Vector3 start=car.transform.position;hold=false;throttle=.45f;yield return new WaitForSeconds(4f);
  float moved=Vector3.Distance(start,car.transform.position);Check(moved>2f&&car.absoluteSpeed>2f,"Kombi moves using real wheel-collider driving physics");
  float heading=car.transform.eulerAngles.y;driver.verificationSteering=.45f;yield return new WaitForSeconds(1.8f);
  Check(Mathf.Abs(Mathf.DeltaAngle(heading,car.transform.eulerAngles.y))>3f,"Steering turns the dynamic kombi without rolling the camera");
  driver.verificationSteering=0;
  throttle=0;hold=true;yield return new WaitForSeconds(3f);
  Check(car.absoluteSpeed<3f,"Handbrake stops the dynamic kombi");
  Check(camera.transform.position.y>car.transform.position.y+.5f,"Chase camera remains visible after driving");
  var stability=car.GetComponent<KombiVehicleStability>();Check(stability!=null&&stability.Recoveries==0,"Live driving does not require under-road recovery");
  report.physics=$"engine={car.engineRunning}; rpm={car.engineRPM:F0}; gear={car.currentGear}; controllerEnabled={car.enabled}; startY={initialY:F2}; finalY={car.transform.position.y:F2}; distance={moved:F2}; up={Vector3.Dot(car.transform.up,Vector3.up):F2}; recoveries={stability.Recoveries}";
  yield return Shot("08-after-driving");Finish();
 }
 void Finish(){if(done)return;done=true;File.WriteAllText(Path.Combine(output,"menu-camera-results.json"),JsonUtility.ToJson(report,true));Debug.Log("KOMBI_MENU_CAMERA_RESULT "+(report.failed.Count==0?"PASS":"FAIL"));EditorApplication.Exit(report.failed.Count==0?0:1);}
}
#endif
