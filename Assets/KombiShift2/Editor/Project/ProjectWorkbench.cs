using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KombiProjectWorkbench
{
    public const string Menu = "Assets/KombiShift2/Scenes/KombiMainMenu.unity";
    public const string Shift = "Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity";

    [MenuItem("Kombi Shift/Open Main Menu")]
    public static void Open()
    {
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Menu);
    }

    [MenuItem("Kombi Shift/Open First Shift")]
    public static void OpenShift()
    {
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(Shift);
        KombiCityOverview.Show();
    }

    public static void OpenForReview()
    {
        OpenShift();
        KombiProjectTour.ShowTour();
    }

    [MenuItem("Kombi Shift/Open Project Guide")]
    public static void Guide() => EditorUtility.RevealInFinder(Path.GetFullPath("Docs/PROJECT_GUIDE.md"));

    public static void Validate()
    {
        string[] scenes = { Menu, Shift };
        var sceneAssets = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x => x).ToArray();
        if (!sceneAssets.SequenceEqual(scenes.OrderBy(x => x))) throw new Exception("Unexpected legacy scenes remain.");
        string report = "CLEANUP VALIDATION\n";
        foreach (string path in scenes)
        {
            var scene = EditorSceneManager.OpenScene(path);
            int missing = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
            if (missing != 0) throw new Exception(path + ": " + missing + " missing scripts.");
            string[] dependencies = AssetDatabase.GetDependencies(path, true);
            if (dependencies.Any(x => x.StartsWith("Assets/BusModel/") || x.Contains("KombiDrive.unity"))) throw new Exception("Old bus dependency remains.");
            if (path == Shift && UnityEngine.Object.FindObjectsByType<RCCP_CarController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0) throw new Exception("Player vehicle missing.");
            report += path + ": loaded; missing scripts = " + missing + "; dependencies = " + dependencies.Length + "\n";
        }
        EditorBuildSettings.scenes = scenes.Select(x => new EditorBuildSettingsScene(x, true)).ToArray();
        EditorSceneManager.OpenScene(Menu);
        Directory.CreateDirectory("Docs");
        File.WriteAllText("Docs/CLEANUP_VALIDATION.txt", report + "Only 2 scene assets remain. Player vehicle found. Old autonomous bus not required.\n");
        Debug.Log("KOMBI_CLEANUP_VERIFIED\n" + report);
    }
}
