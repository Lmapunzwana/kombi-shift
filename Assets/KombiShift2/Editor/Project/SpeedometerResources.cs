using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Essential resources are part of the installed Unity UI package, not a new dependency.
[InitializeOnLoad]
public static class KombiSpeedometerResources
{
    const string Pending="Kombi.Speedometer.TmpImportPending";
    static KombiSpeedometerResources()
    {
        EditorApplication.delayCall+=Ensure;
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=Ensure;};
    }
    [MenuItem("Kombi Shift/Assets/Ensure Speedometer Text Resources")]
    public static void Ensure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))return;
        if(SessionState.GetBool(Pending,false))return;
        SessionState.SetBool(Pending,true);
        AssetDatabase.importPackageCompleted+=Imported;
        AssetDatabase.importPackageFailed+=Failed;
        TMP_PackageResourceImporter.ImportResources(true,false,false);
    }
    static void Imported(string package)
    {
        if(package!="TMP Essential Resources")return;
        Cleanup();Debug.Log("KOMBI_SPEEDOMETER_RESOURCES_READY: TMP essentials imported without examples.");
    }
    static void Failed(string package,string error)
    {Cleanup();Debug.LogError("Speedometer text resource import failed: "+error);}
    static void Cleanup()
    {
        SessionState.SetBool(Pending,false);
        AssetDatabase.importPackageCompleted-=Imported;AssetDatabase.importPackageFailed-=Failed;
    }
}
