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
        public float greenSeconds,amberSeconds,allRedSeconds,longestRedSeconds;
        public List<string> details=new();
    }
    [MenuItem("Kombi Shift/Verify Classified Junction Controls")]
    public static void Run()
    {
        if(EditorSceneManager.GetActiveScene().path!=KombiProjectWorkbench.Shift)EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/CityJunctions.json");
        var document=JsonUtility.FromJson<JunctionDocument>(asset.text);var expected=document.junctions.ToDictionary(x=>x.id);
        var report=new Report
        {
            greenSeconds=CityIntersection.GreenSeconds,
            amberSeconds=CityIntersection.AmberSeconds,
            allRedSeconds=CityIntersection.AllRedSeconds
        };
        float phaseSeconds=report.greenSeconds+report.amberSeconds+report.allRedSeconds;
        if(report.greenSeconds<5f||report.amberSeconds<2f||report.allRedSeconds<1f)
        {report.lightCycleFailures++;report.details.Add("Signal timing is below the safe green, amber, or all-red minimum.");}
        var scene=Object.FindObjectsByType<CityIntersection>().ToDictionary(x=>x.junctionId);report.junctions=scene.Count;
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
                report.primaryLampHeads++;Vector3 outward=arm.Direction;
                Vector3 primaryOffset=arm.lampHead.transform.position-(arm.approachPoint+outward*2f);primaryOffset.y=0f;
                float primaryAlong=Vector3.Dot(primaryOffset,outward);
                float primaryLateral=Mathf.Abs(Vector3.Dot(primaryOffset,Vector3.Cross(Vector3.up,outward)));
                if(primaryAlong<-.5f||primaryAlong>26f||primaryLateral<.5f||primaryLateral>arm.width*.5f+10f||
                    Vector3.Dot(arm.lampHead.transform.forward,outward)<.8f)
                {report.lampPlacementFailures++;report.details.Add(pair.Key+"/"+arm.id+": primary lamp is not at the left approach edge facing traffic");}
                if(arm.repeater!=null)
                {
                    Vector3 repeaterOffset=arm.repeater.transform.position-(arm.approachPoint+outward*2f);repeaterOffset.y=0f;
                    float repeaterAlong=Vector3.Dot(repeaterOffset,outward);
                    float repeaterLateral=Mathf.Abs(Vector3.Dot(repeaterOffset,Vector3.Cross(Vector3.up,outward)));
                    if(repeaterAlong<-.5f||repeaterAlong>10f||repeaterLateral<.5f||repeaterLateral>arm.width*.5f+4f||
                        Vector3.Dot(arm.repeater.transform.forward,outward)<.8f)
                    {report.lampPlacementFailures++;report.details.Add(pair.Key+"/"+arm.id+": repeater is not at the opposite roadside facing traffic");}
                }
            }
            int phases=junction.PhaseCount;
            float longestRed=phases*phaseSeconds-report.greenSeconds;
            report.longestRedSeconds=Mathf.Max(report.longestRedSeconds,longestRed);
            float previousLongestRed=phases*15f-10f;
            if(longestRed>previousLongestRed*.8f)
            {report.lightCycleFailures++;report.details.Add(pair.Key+": red wait was not reduced by at least 20 percent");}
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
