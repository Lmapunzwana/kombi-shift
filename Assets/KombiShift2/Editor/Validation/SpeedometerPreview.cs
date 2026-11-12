using KombiShift2.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KombiSpeedometerPreview
{
    [MenuItem("Kombi Shift/Validation/Capture Working Speedometer")]
    public static void Run()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {Debug.LogWarning("Save the scene and leave Play mode before starting the disposable speedometer preview.");return;}
        CityTripSelection.Selected=null;
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorSettings.enterPlayModeOptionsEnabled=true;
        EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        new GameObject("Speedometer live capture",typeof(SpeedometerCapture));
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying=true;
    }
}
