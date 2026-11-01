using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace KombiShift.JaydenStarter.Editor
{
    public static class StarterSceneBuilder
    {
        [MenuItem("Kombi Shift/Jayden/Create Starter Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string folder = "Assets/KombiShiftJayden/Generated";
            Directory.CreateDirectory(folder);
            // Persist shader references so build stripping does not remove runtime-selected shaders.
            string shaderName = GraphicsSettings.currentRenderPipeline == null ? "Standard" :
                GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit";
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError("Cannot find " + shaderName + ". Finish render pipeline setup first."); return; }
            string materialPath = folder + "/StarterSurface.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            var root = new GameObject("Jayden Standalone Starter");
            var game = root.AddComponent<ShiftGame>(); game.surfaceTemplate = material;
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/KombiShiftStarter.unity");
            EditorSceneManager.SaveScene(scene, path); AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            Debug.Log("Starter scene saved: " + path + ". Press Play. Add this scene to a separate Build Profile to build the starter.");
        }
    }
}
