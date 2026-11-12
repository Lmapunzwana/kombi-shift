using System.IO;
using System.Linq;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class KombiStructureGroundingUpgrade
{
 const string Player="Assets/KombiShift2/Prefabs/Vehicles/Player/Hiace_Player.prefab";
 public static void Apply()
 {
  var prefab=PrefabUtility.LoadPrefabContents(Player);Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,Player);PrefabUtility.UnloadPrefabContents(prefab);
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
  var car=Object.FindFirstObjectByType<RCCP_CarController>();Configure(car.gameObject);
  var roots=EditorSceneManager.GetActiveScene().GetRootGameObjects();
  var systems=Group("00 Systems and Lighting");var environment=Group("01 Environment");var roads=Group("02 Roads and Route");var player=Group("03 Player and Camera");var stops=Group("04 Passenger Stations");var traffic=Group("05 Traffic and Rivals");
  foreach(var root in roots)
  {
   string n=root.name;if(n.Length>3&&char.IsDigit(n[0])&&char.IsDigit(n[1]))continue;
   Transform parent=n=="Hiace_RCC_Player"||n=="Kombi Drive Camera"?player:n.StartsWith("Traffic ")||n=="Competing Kombis"?traffic:n=="Passenger Stations"||n.Contains("Passenger Stop")||n=="Mbare Drop-off"?stops:n.StartsWith("City Street")||n.StartsWith("Asphalt Street")||n.StartsWith("Centre Line")||n.StartsWith("bz")||n=="CBD to Mbare Route"?roads:n.Contains("Systems")||n.Contains("Manager")||n.Contains("Light")||n.Contains("Sky Fill")||n=="Presentation Settings"?systems:environment;
   root.transform.SetParent(parent,true);
  }
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  AssetDatabase.SaveAssets();KombiProjectWorkbench.Validate();Debug.Log("KOMBI_STRUCTURE_GROUNDING_READY");
 }
 static Transform Group(string name){var existing=GameObject.Find(name);return existing!=null?existing.transform:new GameObject(name).transform;}
 static void Configure(GameObject root)
 {
  if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  foreach(var t in root.GetComponentsInChildren<Transform>(true).ToArray())
  {
   if(t==null)continue;
   if(t.name.StartsWith("Model_Skyline")||t.name=="RCCP_BodyTilt"||t.name=="RCCP_ExteriorCameras"||t.name=="RCCP_Customizer"||t.name=="RCCP_NOS"||t.name=="RCCP_Dashboard")Object.DestroyImmediate(t.gameObject);
  }
  if(root.GetComponent<KombiVehicleStability>()==null)root.AddComponent<KombiVehicleStability>();
  var body=root.GetComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;body.centerOfMass=new Vector3(0,.24f,.04f);body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
  foreach(var axle in root.GetComponentsInChildren<RCCP_Axle>()){axle.autoAlignWheelColliders=false;axle.antirollForce=0;axle.isPower=axle.name.Contains("Rear");}
  var aero=root.GetComponentInChildren<RCCP_AeroDynamics>();aero.autoReset=false;aero.dynamicCOM=false;var com=aero.transform.Find("COM");com.localPosition=new Vector3(0,.24f,.04f);
  var damage=root.GetComponentInChildren<RCCP_Damage>();if(damage!=null){damage.wheelDamage=false;damage.wheelDetachment=false;}
  var assist=root.GetComponentInChildren<RCCP_Stability>();assist.steeringHelper=false;assist.tractionHelper=false;
  foreach(var wheel in root.GetComponentsInChildren<WheelCollider>()){wheel.ConfigureVehicleSubsteps(5f,8,12);wheel.suspensionDistance=.24f;var spring=wheel.suspensionSpring;spring.spring=65000;spring.damper=6000;wheel.suspensionSpring=spring;}
 }
}
