using UnityEditor;
using UnityEngine;
public static class KombiMenuPreview
{
    [MenuItem("Kombi Shift/Play Main Menu")]
    public static void Open()
    {
        KombiProjectWorkbench.Open();
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.playModeStateChanged+=Ready;
        EditorApplication.delayCall+=()=>EditorApplication.EnterPlaymode();
    }
    static void Ready(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        EditorApplication.playModeStateChanged-=Ready;
        Debug.Log("KOMBI_MENU_PREVIEW_READY: current Hiace cover and menu running.");
    }
}
