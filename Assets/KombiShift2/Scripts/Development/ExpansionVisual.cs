#if UNITY_EDITOR
using System.Collections;
using System.IO;
using KombiShift2.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class KombiExpansionVisual : MonoBehaviour
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiMainMenu.unity");
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged+=Begin;EditorApplication.isPlaying=true;
    }
    static void Begin(PlayModeStateChange value)
    {if(value!=PlayModeStateChange.EnteredPlayMode)return;EditorApplication.playModeStateChanged-=Begin;new GameObject("Expansion visual evidence").AddComponent<KombiExpansionVisual>();}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory("Docs/Validation/Expansion/Visual");
        yield return new WaitForSeconds(2f);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Validation/Expansion/Visual/01-menu.png"));yield return new WaitForSeconds(.5f);
        yield return SceneManager.LoadSceneAsync("KombiShift_FirstShift");yield return new WaitForSeconds(3f);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Validation/Expansion/Visual/02-expanded-start-hud.png"));yield return new WaitForSeconds(.5f);
        var director=FindFirstObjectByType<KombiShiftGameDirector>();
        director.Vehicle.transform.SetPositionAndRotation(director.Stations[0].ServicePoint.position,director.Stations[0].ServicePoint.rotation);
        director.Vehicle.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
        yield return new WaitForSeconds(3f);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Docs/Validation/Expansion/Visual/03-station-service.png"));yield return new WaitForSeconds(.5f);
        EditorApplication.Exit(0);
    }
}
#endif
