using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KombiShift.Editor
{
    public static class SprintZeroSetup
    {
        [MenuItem("Kombi Shift/Configure project conventions")]
        public static void Configure()
        {
            if (Application.unityVersion != "6000.3.27f2")
                throw new InvalidOperationException("Use the pinned Unity 6000.3.27f2 Editor.");
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Project conventions configured. Run HDRP Wizard and check in generated settings and package lock.");
        }

        [MenuItem("Kombi Shift/Open Bootstrap")]
        public static void OpenBootstrap()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");
        }

        // CLI: -batchmode -projectPath PATH -executeMethod KombiShift.Editor.SprintZeroSetup.BuildWindows -quit
        public static void BuildWindows()
        {
            Configure();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Bootstrap.unity" },
                locationPathName = "Builds/Windows/KombiShift.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        }
    }
}
