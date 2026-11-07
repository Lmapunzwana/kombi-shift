using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class KombiProjectGroundingAudit
{
 public static void Run()
 {
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
  var car=Object.FindFirstObjectByType<RCCP_CarController>();var b=new StringBuilder();
  foreach(var component in car.GetComponentsInChildren<Component>(true))
  {
   if(component==null)continue;
   if(component is Transform t){b.AppendLine($"TRANSFORM {PathOf(t)} local={t.localPosition} angles={t.localEulerAngles} scale={t.localScale}");continue;}
   if(component is Renderer r){b.AppendLine($"RENDERER {PathOf(r.transform)} bounds={r.bounds}");continue;}
   var so=new SerializedObject(component);var p=so.GetIterator();b.AppendLine($"COMPONENT {PathOf(component.transform)} {component.GetType().Name}");
   while(p.NextVisible(true))if(p.depth<=1)b.AppendLine($" {p.propertyPath}: {Value(p)}");
  }
  foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())b.AppendLine("ROOT "+root.name);
  Directory.CreateDirectory("Docs/Validation/Grounding");File.WriteAllText("Docs/Validation/Grounding/setup-audit.txt",b.ToString());
  var roots=AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath)
   .Concat(AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&(p.Contains("/Resources/")||p.EndsWith(".cs")||p.EndsWith(".dll")||p.EndsWith(".asmdef")||p.EndsWith(".asset")))).ToArray();
  var used=new HashSet<string>(AssetDatabase.GetDependencies(roots,true));
  var candidates=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p)&&!used.Contains(p)).Select(p=>new Item{path=p,bytes=File.Exists(p)?new FileInfo(p).Length:0}).OrderByDescending(x=>x.bytes).ToArray();
  File.WriteAllText("Docs/Validation/Grounding/unreferenced-assets.json",JsonUtility.ToJson(new Items{assets=candidates},true));Debug.Log("KOMBI_GROUNDING_AUDIT_READY");
 }
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static string Value(SerializedProperty p)=>p.propertyType==SerializedPropertyType.ObjectReference?(p.objectReferenceValue!=null?p.objectReferenceValue.name:"null"):p.propertyType==SerializedPropertyType.Float?p.floatValue.ToString():p.propertyType==SerializedPropertyType.Integer?p.intValue.ToString():p.propertyType==SerializedPropertyType.Boolean?p.boolValue.ToString():p.propertyType==SerializedPropertyType.Vector3?p.vector3Value.ToString():p.propertyType==SerializedPropertyType.Enum?p.enumValueIndex.ToString():p.propertyType.ToString();
 [Serializable]class Item{public string path;public long bytes;}[Serializable]class Items{public Item[] assets;}
}
