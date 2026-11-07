using System.Linq;
using KombiShift2.Traffic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class KombiFinalStructureValidation
{
 public static void Run()
 {
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Menu);Prune(GameObject.Find("Hiace Showcase"),true);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
  foreach(var rival in Object.FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None))Prune(rival.gameObject,false);
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  KombiProjectWorkbench.Validate();Debug.Log("KOMBI_FINAL_STRUCTURE_VALIDATED");
 }
 static void Prune(GameObject root,bool showcase)
 {
  if(root==null)return;
  foreach(var child in root.transform.Cast<Transform>().ToArray())
   if(child.name.StartsWith("Model_Skyline")||child.name.StartsWith("RCCP_")||(showcase&&child.name=="Passenger Cabin"))Object.DestroyImmediate(child.gameObject);
 }
}
