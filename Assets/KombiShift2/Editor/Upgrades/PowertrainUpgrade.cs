using System.IO;
using System.Text;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class KombiPowertrainUpgrade
{
 public static void Apply()
 {
  const string path="Assets/KombiShift2/Prefabs/Vehicles/Player/Hiace_Player.prefab";
  var prefab=PrefabUtility.LoadPrefabContents(path);Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);Configure(Object.FindFirstObjectByType<RCCP_CarController>().gameObject);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  KombiProjectWorkbench.Validate();Debug.Log("KOMBI_POWERTRAIN_UPGRADED");
 }
 static void Configure(GameObject car)
 {
  var tuning=car.GetComponent<KombiPowertrain>();if(tuning==null)tuning=car.AddComponent<KombiPowertrain>();tuning.Configure();
  var audio=car.GetComponentInChildren<RCCP_Audio>();var so=new SerializedObject(audio);var p=so.GetIterator();var text=new StringBuilder();
  while(p.NextVisible(true))
  {
   if(p.propertyPath.StartsWith("engineSounds"))text.AppendLine(p.propertyPath+"="+(p.propertyType==SerializedPropertyType.Float?p.floatValue.ToString():p.propertyType.ToString()));
   if(p.propertyType!=SerializedPropertyType.Float)continue;
   if(p.propertyPath.StartsWith("engineSounds")&&p.name=="maxVolume")p.floatValue=.28f;
   if(p.propertyPath.StartsWith("engineSounds")&&p.name=="minPitch")p.floatValue=.65f;
   if(p.propertyPath.StartsWith("engineSounds")&&p.name=="maxPitch")p.floatValue=1.15f;
  }
  so.ApplyModifiedPropertiesWithoutUndo();Directory.CreateDirectory("Docs/Validation/Powertrain");File.WriteAllText("Docs/Validation/Powertrain/audio-fields-before.txt",text.ToString());
 }
}
