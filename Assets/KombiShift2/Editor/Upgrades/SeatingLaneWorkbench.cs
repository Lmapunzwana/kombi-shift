using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// A one-shot project repair request. It never exits Play mode or discards an unsaved scene.
[InitializeOnLoad]
public static class KombiSeatingLaneWorkbench
{
    const string Request = "Library/KombiSeatingLane.request";
    const string Status = "Library/KombiSeatingLane.status";
    static double next;
    static KombiSeatingLaneWorkbench() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next || !File.Exists(Request))return;
        next=EditorApplication.timeSinceStartup+2;
        if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText(Status,"Waiting for Play mode to stop; Editor stays open.");return;}
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=KombiProjectWorkbench.Shift){File.WriteAllText(Status,"Open the driving scene to continue; no scene was changed.");return;}
        string action=File.ReadAllText(Request).Trim();
        if(action!="audit" && scene.isDirty){File.WriteAllText(Status,"Save the driving scene before applying the repair; unsaved edits were preserved.");return;}
        try
        {
            if(action=="audit")KombiSeatingLayoutAudit.Run();
            else if(action=="repair")KombiPassengerSeatingLaneRepair.Apply();
            else if(action=="inspect")KombiSeatingLanePreview.Inspect();
            else throw new InvalidOperationException("Unknown seating repair action.");
            File.Delete(Request);File.WriteAllText(Status,"Completed: "+action);
        }
        catch(Exception ex){File.Delete(Request);File.WriteAllText(Status,"Failed: "+ex.Message);Debug.LogException(ex);}
    }
}
