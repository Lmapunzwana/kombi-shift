using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
namespace KombiShift.JaydenStarter.Editor {
 public sealed class GameWorkbench:EditorWindow {
  Vector2 scroll;
  [MenuItem("Kombi Shift/Open Game Workbench")]
  public static void Open()=>GetWindow<GameWorkbench>("Kombi Shift");
  public static void OpenProject(){EditorSceneManager.OpenScene("Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity");Open();Debug.Log("KOMBI_EDITOR_READY: Game scene and workbench open. Press Play.");}
  void OnInspectorUpdate()=>Repaint();
  void OnGUI(){scroll=EditorGUILayout.BeginScrollView(scroll);GUILayout.Label("KOMBI SHIFT | PLAY, INSPECT, LEARN",EditorStyles.boldLabel);
   EditorGUILayout.HelpBox("Open the Game scene, press Play, then use these tools to inspect live systems. Stop Play before saving asset changes. Test actions below are Editor-only controls.",MessageType.Info);
   if(GUILayout.Button("Open Game scene")){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity");}
   if(GUILayout.Button(EditorApplication.isPlaying?"Stop Play":"Play in this Editor"))EditorApplication.isPlaying=!EditorApplication.isPlaying;
   if(GUILayout.Button("Select persistent game tuning"))Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameTuning>("Assets/KombiShiftJayden/Generated/GameTuning.asset");
   if(!Application.isPlaying&&GUILayout.Button("Open Art Workshop scene")){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/KombiShiftJayden/Generated/ArtWorkshop.unity");}
   if(GUILayout.Button("Open art catalog / reusable prefabs"))Selection.activeObject=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/KombiShiftJayden/Catalog");
   var game=FindFirstObjectByType<ShiftGame>();
   if(game!=null&&Application.isPlaying){GUILayout.Label("Screen: "+game.ScreenName+" | Rival: "+game.RivalStateName);GUILayout.Label("Passengers "+game.OnboardCount+" | Fares $"+game.GrossFares.ToString("F2")+" | Cash $"+game.Cash.ToString("F2"));GUILayout.Label("Fuel "+game.Fuel.ToString("F0")+" | Health "+game.Condition.ToString("F0"));
    if(GUILayout.Button("Start CBD"))game.EditorStartCity();if(GUILayout.Button("Start rainy highway"))game.EditorStartHighway();
    for(int i=0;i<(game.Rainy?4:6);i++)if(GUILayout.Button("Park at stop "+(i+1)))game.EditorGoToStop(i);
    if(GUILayout.Button("Board / drop / refuel"))game.EditorService();if(GUILayout.Button("Damage 15"))game.EditorDamage();if(GUILayout.Button("Restore health, wear, fuel and cash (test only)"))game.EditorRestore();if(GUILayout.Button("Pause / resume"))game.EditorPause();if(GUILayout.Button("Select generated world and managers"))Selection.activeGameObject=game.gameObject;
    if(GUILayout.Button("Run automated regression (resets this shift)"))game.EditorRunChecks();
   }
   GUILayout.Space(12);GUILayout.Label("UNDERSTAND THE CODE",EditorStyles.boldLabel);
   string[] files={"ShiftGame.cs","Systems/VehicleController.cs","Systems/VehicleWear.cs","Systems/PassengerAI.cs","Systems/RivalAI.cs","Systems/SpawnManager.cs","Systems/FuelSystem.cs","Systems/GameManager.cs","Systems/GameTuning.cs"};
   foreach(var f in files)if(GUILayout.Button(f))AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/KombiShiftJayden/Runtime/"+f));
   EditorGUILayout.HelpBox("Vehicle: local forward velocity + tyre grip, latched parking, bounded collision damage. Passengers: coroutine patience + boarding and destination payout. Rival: explicit FSM + road graph. Pools: preallocated pedestrians/hazards/traffic. HDRP: separate desktop project; Built-In: browser-compatible project.",MessageType.None);EditorGUILayout.EndScrollView();
  }
 }
 public static class CompleteProject {
  const string folder="Assets/KombiShiftJayden/Generated";
  [MenuItem("Kombi Shift/Prepare Complete Project")]
  public static void Prepare(){Directory.CreateDirectory(folder);AssetDatabase.Refresh();
   var tune=AssetDatabase.LoadAssetAtPath<GameTuning>(folder+"/GameTuning.asset");if(tune==null){tune=ScriptableObject.CreateInstance<GameTuning>();AssetDatabase.CreateAsset(tune,folder+"/GameTuning.asset");}
   var scene=EditorSceneManager.OpenScene(folder+"/KombiShiftStarter.unity");var game=UnityEngine.Object.FindFirstObjectByType<ShiftGame>();game.tuning=tune;game.engineRecording=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KombiShiftJayden/Resources/Audio/BusIdle.wav");game.conductorCall=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KombiShiftJayden/Resources/Audio/HwindiCall.wav");game.conductorReady=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KombiShiftJayden/Resources/Audio/HwindiReady.wav");game.conductorFare=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/KombiShiftJayden/Resources/Audio/HwindiFare.wav");game.name="Kombi Shift - Game Root (press Play)";EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   BuildAnimation();CatalogExporter.Export();game.passengerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KombiShiftJayden/Catalog/Passenger.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   if(!System.IO.File.Exists(folder+"/ArtWorkshop.unity")){var workshop=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);string[] props={"Kombi","Passenger","Rank shelter","Fuel pump","Road Straight 10m"};for(int i=0;i<props.Length;i++){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KombiShiftJayden/Catalog/"+props[i]+".prefab");var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.position=new Vector3((i-2)*4,0,0);}var camera=Camera.main;camera.transform.position=new Vector3(10,8,-18);camera.transform.LookAt(Vector3.up);EditorSceneManager.SaveScene(workshop,folder+"/ArtWorkshop.unity");EditorSceneManager.OpenScene(folder+"/KombiShiftStarter.unity");}
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(folder+"/KombiShiftStarter.unity",true)};AssetDatabase.SaveAssets();Debug.Log("KOMBI_PROJECT_PREPARED");
  }
  static void BuildAnimation(){string dir="Assets/KombiShiftJayden/Resources/Animation";Directory.CreateDirectory(dir);AssetDatabase.Refresh();string path=dir+"/Passenger.controller";
   if(AssetDatabase.LoadAssetAtPath<AnimatorController>(path)!=null)return;
   var controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("Walking",AnimatorControllerParameterType.Bool);var machine=controller.layers[0].stateMachine;
   AnimationClip Clip(string name,float angle,float duration){var c=new AnimationClip{name=name};c.SetCurve("Arm L",typeof(Transform),"localEulerAnglesRaw.x",AnimationCurve.EaseInOut(0,-angle,duration*.5f,angle));c.SetCurve("Arm R",typeof(Transform),"localEulerAnglesRaw.x",AnimationCurve.EaseInOut(0,angle,duration*.5f,-angle));var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(c,settings);AssetDatabase.CreateAsset(c,dir+"/"+name+".anim");return c;}
   var idle=machine.AddState("Waiting");idle.motion=Clip("Waiting",3,1.5f);var walk=machine.AddState("Walk to door");walk.motion=Clip("Walk",28,.65f);machine.defaultState=idle;
   var a=idle.AddTransition(walk);a.hasExitTime=false;a.duration=.15f;a.AddCondition(AnimatorConditionMode.If,0,"Walking");var b=walk.AddTransition(idle);b.hasExitTime=false;b.duration=.15f;b.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
  }
 }
}
