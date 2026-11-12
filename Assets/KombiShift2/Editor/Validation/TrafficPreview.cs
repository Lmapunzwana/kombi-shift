using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using KombiShift2.City;

public static class KombiTrafficPreview
{
    [MenuItem("Kombi Shift/Validation/Inspect Moving Traffic And Compact HUD")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)
        {
            CityTripSelection.Selected=null;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="KombiShift_FirstShift")
                UnityEngine.SceneManagement.SceneManager.LoadScene("KombiShift_FirstShift");
            EditorApplication.isPaused=false;
            new GameObject("Live traffic observation",typeof(TrafficMotionCapture));return;
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
    static void Ready(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        EditorApplication.playModeStateChanged-=Ready;
        new GameObject("Live traffic observation",typeof(TrafficMotionCapture));
    }
}
