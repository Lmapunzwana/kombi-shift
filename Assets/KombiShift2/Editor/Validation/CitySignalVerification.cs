using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class KombiCitySignalVerification
{
    [Serializable] sealed class JunctionDocument { public JunctionRecord[] junctions; }
    [Serializable] sealed class JunctionRecord { public string id,control; public ArmRecord[] arms; }
    [Serializable] sealed class ArmRecord { public string id,control; public bool incoming; }
    [Serializable] sealed class Report
    {
        public int junctions,signalisedJunctions,signalisedIncomingArms,primaryLampHeads;
        public int classificationMismatches,lampPlacementFailures,lightCycleFailures;
        public List<string> details=new();
    }
    [MenuItem("Kombi Shift/Verify Classified Junction Controls")]
    public static void Run()
    {
        if(EditorSceneManager.GetActiveScene().path!=KombiProjectWorkbench.Shift)EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/CityJunctions.json");
        var document=JsonUtility.FromJson<JunctionDocument>(asset.text);var expected=document.junctions.ToDictionary(x=>x.id);
        var report=new Report();var scene=Object.FindObjectsByType<CityIntersection>().ToDictionary(x=>x.junctionId);report.junctions=scene.Count;
        foreach(var pair in expected)
        {
            if(!scene.TryGetValue(pair.Key,out var junction)){report.classificationMismatches++;report.details.Add(pair.Key+": missing scene controller");continue;}
            var record=pair.Value;
            if(junction.control!=record.control||junction.signalised!=(record.control=="Signalised"))
            {report.classificationMismatches++;report.details.Add(pair.Key+": classified "+record.control+", scene "+junction.control);}
            int expectedSignals=(record.arms??Array.Empty<ArmRecord>()).Count(a=>a.incoming&&a.control=="Signal");
            int actualSignals=junction.arms.Count(a=>a!=null&&a.incoming&&a.control=="Signal");
            if(expectedSignals!=actualSignals){report.classificationMismatches++;report.details.Add(pair.Key+": expected "+expectedSignals+" signal arms, scene "+actualSignals);}
            if(!junction.signalised)continue;report.signalisedJunctions++;report.signalisedIncomingArms+=actualSignals;
            foreach(var arm in junction.arms.Where(a=>a!=null&&a.incoming&&a.control=="Signal"))
            {
                if(arm.lampHead==null){report.lampPlacementFailures++;report.details.Add(pair.Key+"/"+arm.id+": missing primary lamp head");continue;}
                report.primaryLampHeads++;Vector3 outward=arm.Direction,inward=-outward,left=-Vector3.Cross(Vector3.up,inward);
                Vector3 expectedPosition=junction.transform.position+outward*(junction.radius+1f)+left*(arm.width*.5f+.8f)+Vector3.up*3f;
                if(Vector3.Distance(arm.lampHead.transform.position,expectedPosition)>3f||Vector3.Dot(arm.lampHead.transform.forward,outward)<.8f)
                {report.lampPlacementFailures++;report.details.Add(pair.Key+"/"+arm.id+": primary lamp is not at the left approach edge facing traffic");}
            }
            int phases=junction.PhaseCount;
            for(int phase=0;phase<phases;phase++)
            {
                var green=junction.arms.Where(a=>a!=null&&a.incoming&&a.phase==phase).ToArray();
                if(green.Length==0){report.lightCycleFailures++;report.details.Add(pair.Key+": empty phase "+phase);continue;}
                for(int a=0;a<green.Length;a++)for(int b=a+1;b<green.Length;b++)
                    if(Mathf.Abs(Mathf.DeltaAngle(green[a].bearing,green[b].bearing))<110f)
                    {report.lightCycleFailures++;report.details.Add(pair.Key+": conflicting green arms "+green[a].id+" and "+green[b].id);}
            }
            foreach(var arm in junction.arms.Where(a=>a!=null&&a.incoming))if(arm.phase<0||arm.phase>=phases)
            {report.lightCycleFailures++;report.details.Add(pair.Key+"/"+arm.id+": never receives green");}
        }
        if(scene.Count!=expected.Count){report.classificationMismatches++;report.details.Add("Classifier has "+expected.Count+" junctions; scene has "+scene.Count);}
        Directory.CreateDirectory("Docs/Validation/CityInfrastructure");
        File.WriteAllText("Docs/Validation/CityInfrastructure/junction-control-audit.json",JsonUtility.ToJson(report,true)+"\n");
        Debug.Log("KOMBI_CITY_SIGNAL_AUDIT "+JsonUtility.ToJson(report));
        if(report.classificationMismatches+report.lampPlacementFailures+report.lightCycleFailures>0)throw new InvalidOperationException("Junction control verification failed; inspect junction-control-audit.json.");
    }
}
