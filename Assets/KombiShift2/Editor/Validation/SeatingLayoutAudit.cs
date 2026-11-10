using System.IO;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Vehicle;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KombiSeatingLayoutAudit
{
    public static void Run()
    {
        if(Application.isBatchMode)EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        else if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=KombiProjectWorkbench.Shift)throw new System.InvalidOperationException("Open the driving scene for the seating audit.");
        var car=Object.FindFirstObjectByType<RCCP_CarController>();
        var log=new System.Text.StringBuilder();
        foreach(var renderer in car.GetComponentsInChildren<Renderer>())
        {
            var b=renderer.bounds;
            log.AppendLine(renderer.name+" localCentre="+car.transform.InverseTransformPoint(b.center).ToString("F3")+" size="+b.size.ToString("F3"));
        }
        foreach(var person in Object.FindObjectsByType<StationPassenger>(FindObjectsSortMode.None).Take(6))
        {
            if(Application.isBatchMode)person.PoseSeatedPreview();
            var hip=person.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips);
            log.AppendLine("ACTOR "+person.name+" scale="+person.transform.lossyScale+" hipLocal="+person.transform.InverseTransformPoint(hip.position).ToString("F3"));
            Bounds bounds=new Bounds();bool started=false;
            foreach(var skin in person.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();skin.BakeMesh(mesh);
                foreach(var vertex in mesh.vertices)
                {var p=person.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));if(!started){bounds=new Bounds(p,Vector3.zero);started=true;}else bounds.Encapsulate(p);}
                Object.DestroyImmediate(mesh);
            }
            log.AppendLine("BAKED "+bounds.ToString("F3"));
        }
        Directory.CreateDirectory("Docs/Validation/SeatingLane");File.WriteAllText("Docs/Validation/SeatingLane/baseline-layout.txt",log.ToString());
        Debug.Log("KOMBI_SEATING_AUDIT_SAVED");
    }
}
