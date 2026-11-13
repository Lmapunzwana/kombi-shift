using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using KombiShift2.City;

[InitializeOnLoad]
public static class KombiTrafficPreview
{
    static KombiTrafficPreview()
    {
        EditorApplication.delayCall+=()=>{
            const string request="Temp/KombiTrafficObservation.request";
            if(!File.Exists(request))return;
            File.Delete(request);Run();
        };
    }
    [MenuItem("Kombi Shift/Validation/Inspect Moving Traffic And Compact HUD")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded-=SceneReady;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded+=SceneReady;
            EditorApplication.isPaused=false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("KombiShift_FirstShift");return;
        }
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {Debug.LogWarning("Save the scene and leave Play mode before traffic verification.");return;}
        CityTripSelection.Selected=null;
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorApplication.playModeStateChanged-=Ready;
        EditorApplication.playModeStateChanged+=Ready;
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }
    static void SceneReady(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded-=SceneReady;
        new GameObject("Live traffic observation",typeof(TrafficMotionCapture));
    }
    static void Ready(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        EditorApplication.playModeStateChanged-=Ready;
        new GameObject("Live traffic observation",typeof(TrafficMotionCapture));
    }
}
