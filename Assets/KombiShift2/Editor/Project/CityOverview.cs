using System.Linq;
using KombiShift2.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KombiCityOverview
{
    static KombiCityOverview()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += ShowIfCity;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == KombiProjectWorkbench.Shift)
            EditorApplication.delayCall += ShowIfCity;
    }

    static void ShowIfCity()
    {
        if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.GetActiveScene().path == KombiProjectWorkbench.Shift)
            Show();
    }

    [MenuItem("Kombi Shift/Show City Overview")]
    public static void Show()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        if (scene.path != KombiProjectWorkbench.Shift) return;
        foreach (var camera in Object.FindObjectsByType<KombiDriveCamera>(FindObjectsSortMode.None))
            camera.ResetView();
        var roots = scene.GetRootGameObjects();
        var city = roots.Where(x => x.name.StartsWith("NYBlock"));
        var renderers = city.SelectMany(x => x.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        if (renderers.Length == 0)
            renderers = roots.SelectMany(x => x.GetComponentsInChildren<MeshRenderer>()).ToArray();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        var view = SceneView.GetWindow<SceneView>();
        view.in2DMode = false;
        view.LookAt(bounds.center, Quaternion.Euler(35f, -45f, 0f), Mathf.Max(30f, bounds.extents.magnitude * .9f), false, true);
        view.Focus();
        view.Repaint();
        Debug.Log("KOMBI_CITY_OVERVIEW_READY: Scene view framed; drive camera initialised above the road.");
    }
}
