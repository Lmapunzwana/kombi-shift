using UnityEditor;
using UnityEngine;

public sealed class KombiProjectTour : EditorWindow
{
    Vector2 scroll;
    [MenuItem("Kombi Shift/Project Tour")]
    public static void ShowTour()
    {
        var window = GetWindow<KombiProjectTour>("Kombi Project Tour");
        window.minSize = new Vector2(460, 400);
    }
    void OnGUI()
    {
        GUILayout.Label("KOMBI SHIFT — PROJECT TOUR", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Start at Scenes, then follow the folders below. Select a folder to highlight it in the Project panel. Stop Play mode before editing.", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open menu")) KombiProjectWorkbench.Open();
        if (GUILayout.Button("Open First Shift")) KombiProjectWorkbench.OpenShift();
        if (GUILayout.Button("Full guide")) KombiProjectWorkbench.Guide();
        EditorGUILayout.EndHorizontal();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        Folder("Scenes", "Assets/KombiShift2/Scenes", "The two current game scenes: KombiMainMenu and KombiShift_FirstShift. All earlier demos are archived outside Assets.");
        Folder("Scripts", "Assets/KombiShift2/Scripts", "Your gameplay code. Gameplay = shift progression and personas; Vehicle = RCC keyboard setup, lights and wipers; Camera = chase/cabin; Traffic = moving vehicles; UI = menu and navigation; Weather = weather transitions. Stations handle real seated passengers and boarding; rivals compete for these same people. Persona economy/patience tuning is not fully connected to this shift.");
        Folder("Passenger data", "Assets/KombiShift2/Data/PassengerPersonas", "Six passenger persona assets now reference rigged passenger prefabs. Passengers sit at stops and walk into player or rival cabin seats. Patience, tolerance and tip values remain tuning data; this shift credits a fixed $2 per boarded passenger.");
        Folder("Player vehicle", "Assets/KombiShift2/Prefabs/Vehicles/Player", "The active Hiace RCC prefab and related vehicle materials. Open the prefab to inspect Rigidbody, wheel colliders and driving settings. Keep the prefab's asset GUID intact.");
        Folder("Traffic art", "Assets/KombiShift2/Art/Vehicles/Traffic", "Reusable vehicle art for traffic. Scene objects and traffic scripts provide movement.");
        Folder("Character art", "Assets/KombiShift2/Art/Characters", "Imported FBX characters, textures and materials. Passenger prefabs use these humanoid rigs with procedural sitting and walking poses. Inspect Rig and Animation tabs on the source models.");
        Folder("Environment", "Assets/KombiShift2/Art/Environment", "City buildings and roads, bus stop props and park props referenced by the current city scene. These are imported environment assets, not extra playable scenes.");
        Folder("Gameplay prefabs", "Assets/KombiShift2/Prefabs", "Reusable road and gameplay objects. A prefab is a reusable object template, whereas a scene is a whole level.");
        Folder("Resources", "Assets/KombiShift2/Resources", "Menu fonts and runtime-loaded settings/assets. The title screen uses a live 3D Hiace rather than the historical cover image. Moving resources requires updating their loading code.");
        Folder("Generated", "Assets/KombiShift2/Generated", "Generated city materials, terrain or wheel mesh data used by scene/prefab references. These are support assets, not old levels.");
        Folder("Shaders", "Assets/KombiShift2/Shaders", "Code controlling how selected materials render. Materials reference these shaders.");
        Folder("Original art sources", "Assets/KombiShift2/ThirdParty", "Source Hiace, traffic models and road textures. Keep source licences and attribution with them.");
        Folder("Car controller package", "Assets/Realistic Car Controller Pro", "Shared driving physics, cameras, audio, input, presets and editor support. Kept at its original path for package compatibility. Its old demo scenes were archived.");
        Folder("Road package", "Assets/EasyRoads3D", "Shared road materials, shaders and road tooling. The old demo scenes and terrain samples were archived; the current scene still needs road support assets.");
        Folder("Path plugin", "Assets/Plugins", "Bezier path support used by navigation. Third-party plugin source is separate from your gameplay scripts.");
        Folder("XR settings", "Assets/XR", "Inherited XR configuration referenced by project settings. Desktop play does not require a headset. This is configuration, not a second game.");
        Folder("Editor tools", "Assets/KombiShift2/Editor", "This tour and scene-opening/validation tools. Editor scripts assist development and are excluded from a player build. Earlier prototype generators are archived.");
        EditorGUILayout.EndScrollView();
    }
    static void Folder(string label, string path, string explanation)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label(label, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(explanation, EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Select folder", GUILayout.Width(130)))
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        EditorGUILayout.EndVertical();
    }
}
