using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace KombiShift.JaydenStarter.Editor {
 [InitializeOnLoad] public static class EditorVerification {
  static EditorVerification(){EditorApplication.update+=Tick;}
  public static void Run(){SessionState.SetBool("Kombi.Verify",true);SessionState.SetBool("Kombi.VerifyStarted",false);SessionState.SetString("Kombi.VerifyTime",DateTime.UtcNow.ToString("O"));EditorSceneManager.OpenScene("Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity");EditorApplication.isPlaying=true;}
  static void Tick(){if(!SessionState.GetBool("Kombi.Verify",false))return;var start=DateTime.Parse(SessionState.GetString("Kombi.VerifyTime",DateTime.UtcNow.ToString("O"))).ToUniversalTime();if((DateTime.UtcNow-start).TotalSeconds>180){Debug.LogError("KOMBI_EDITOR_VERIFY_TIMEOUT");SessionState.SetBool("Kombi.Verify",false);EditorApplication.Exit(1);return;}
   if(EditorApplication.isPlaying&&!SessionState.GetBool("Kombi.VerifyStarted",false)){var g=UnityEngine.Object.FindFirstObjectByType<ShiftGame>();if(g!=null&&g.Wear!=null){SessionState.SetBool("Kombi.VerifyStarted",true);g.EditorRunChecks();}}
   string p=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/concept-results.json"));if(File.Exists(p)&&File.GetLastWriteTimeUtc(p)>start){string report=File.ReadAllText(p);bool ok=report.Contains("\"failed\": []");Debug.Log("KOMBI_EDITOR_PLAY_VERIFIED "+p+" "+ok);SessionState.SetBool("Kombi.Verify",false);EditorApplication.isPlaying=false;EditorApplication.Exit(ok?0:1);}
  }
 }
}
