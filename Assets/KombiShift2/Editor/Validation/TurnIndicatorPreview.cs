using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KombiTurnIndicatorPreview
{
    static KombiTurnIndicatorPreview()
    {
        EditorApplication.delayCall+=()=>{
            const string request="Temp/KombiTurnIndicators.request";
            if(!File.Exists(request))return;
            File.Delete(request);Run();
        };
    }
    [MenuItem("Kombi Shift/Validation/Inspect Working Turn Indicators")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)
        {
            SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
            EditorApplication.isPaused=false;
            SceneManager.LoadScene("KombiShift_FirstShift");return;
        }
        if(SceneManager.GetActiveScene().isDirty)
        {Debug.LogWarning("Save the scene before starting indicator verification.");return;}
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorApplication.playModeStateChanged-=Ready;EditorApplication.playModeStateChanged+=Ready;
        EditorApplication.EnterPlaymode();
    }
    static void Loaded(Scene scene,LoadSceneMode mode)
    {SceneManager.sceneLoaded-=Loaded;new GameObject("Turn indicator observation",typeof(TurnIndicatorVerification));}
    static void Ready(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        EditorApplication.playModeStateChanged-=Ready;
        new GameObject("Turn indicator observation",typeof(TurnIndicatorVerification));
    }
}
