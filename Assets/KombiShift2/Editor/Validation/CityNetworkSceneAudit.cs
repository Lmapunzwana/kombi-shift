using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class KombiCityNetworkSceneAudit
{
    [Serializable] public class LaneIssue {public int from,to;public bool unsupported,blocked;public string collider;}
    [Serializable] public class Record
    {
        public int cityRenderers,roadSurfaces,physicalPotholes,nodes,links,unsupportedLaneSamples,blockedLaneSegments;
        public int markingMeshes,markingTriangles,markingColliders,roadsideGuideVertices,unsupportedRoadsideGuideVertices,roadsideGuideVerticesOnRoad;
        public int signalisedJunctions,trafficSignalHeads,misplacedTrafficSignalHeads;
        public List<string> details=new();
        public List<LaneIssue> laneIssues=new();
        public int reachableTrips;
    }
    static bool IsRoad(Collider collider)
    {
        if(collider.GetComponent<CityRoadSurface>()!=null)return true;
        var renderer=collider.GetComponent<Renderer>();
        return renderer!=null&&renderer.sharedMaterials.Any(m=>m!=null&&(m.name.ToLowerInvariant().Contains("asphalt")||m.shader.name=="Kombi Shift/Road Surface"));
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);Physics.SyncTransforms();
        var graph=Object.FindFirstObjectByType<CityRoadNetwork>();graph.Load(true);
        var report=new Record();
        report.cityRenderers=Object.FindObjectsByType<MeshFilter>().Count(m=>AssetDatabase.GetAssetPath(m.sharedMesh).Contains("Environment/City")||m.GetComponent<CityRoadSurface>()!=null);
        var roads=Object.FindObjectsByType<CityRoadSurface>();report.roadSurfaces=roads.Length;report.physicalPotholes=roads.Sum(r=>r.potholes.Length);report.nodes=graph.Nodes.Length;
        var markingRoot=GameObject.Find("City road markings");
        if(markingRoot!=null)
        {
            var markings=markingRoot.GetComponentsInChildren<MeshFilter>();
            report.markingMeshes=markings.Length;report.markingTriangles=markings.Where(m=>m.sharedMesh!=null).Sum(m=>m.sharedMesh.triangles.Length/3);
            report.markingColliders=markings.Count(m=>m.GetComponent<Collider>()!=null);
            var roadside=markingRoot.transform.Find("Roadside traffic guide mesh");
            var roadsideMesh=roadside!=null?roadside.GetComponent<MeshFilter>()?.sharedMesh:null;
            if(roadsideMesh!=null)
            {
                var guideHits=new RaycastHit[16];
                foreach(var vertex in roadsideMesh.vertices)
                {
                    report.roadsideGuideVertices++;
                    Vector3 point=roadside.TransformPoint(vertex);
                    int count=Physics.RaycastNonAlloc(point+Vector3.up*.15f,Vector3.down,guideHits,.3f,~0,QueryTriggerInteraction.Ignore);
                    bool supported=false,onRoad=false;
                    for(int i=0;i<count;i++)
                    {
                        var hit=guideHits[i];if(hit.rigidbody!=null||hit.normal.y<.65f||Mathf.Abs(hit.point.y-point.y)>.1f)continue;
                        if(IsRoad(hit.collider))onRoad=true;else supported=true;
                    }
                    if(!supported)report.unsupportedRoadsideGuideVertices++;
                    if(onRoad)report.roadsideGuideVerticesOnRoad++;
                }
            }
        }
        else report.details.Add("City road marking root is missing.");
        foreach(var junction in Object.FindObjectsByType<CityIntersection>())
        {
            if(!junction.signalised)continue;
            report.signalisedJunctions++;
            foreach(var arm in junction.arms)
            {
                if(arm==null||!arm.incoming||arm.control!="Signal")continue;
                int expectedHeads=1+(arm.width>=16f?1:0);
                report.trafficSignalHeads+=(arm.lampHead!=null?1:0)+(arm.repeater!=null?1:0);
                if(arm.lampHead==null||arm.width>=16f&&arm.repeater==null)
                {report.misplacedTrafficSignalHeads++;continue;}
                Vector3 outward=arm.Direction,left=Vector3.Cross(Vector3.up,outward);
                Vector3 expected=junction.transform.position+outward*(junction.radius+1f)+left*(arm.width*.5f+.8f);
                Vector3 offset=arm.lampHead.transform.position-expected;offset.y=0f;
                if(offset.magnitude>3f||Vector3.Dot(arm.lampHead.transform.forward,outward)<.8f)report.misplacedTrafficSignalHeads++;
                if(arm.repeater!=null&&expectedHeads<2)report.misplacedTrafficSignalHeads++;
            }
        }
        foreach(var road in roads)
        {
            foreach(var hole in road.potholes)
            {
                if(!Physics.Raycast(hole.centre+Vector3.up*.6f,Vector3.down,out var hit,2,~0,QueryTriggerInteraction.Ignore) || hole.centre.y-hit.point.y<hole.depth*.65f)
                    report.details.Add("Pothole filled or not deep enough: "+road.name+" "+hole.centre);
            }
        }
        var hits=new RaycastHit[32];
        for(int i=0;i<graph.Nodes.Length;i++)
        {
            var node=graph.Nodes[i];if(node.component!=0)continue;
            foreach(int next in node.links)
            {
                report.links++;Vector3 forward=(graph.Nodes[next].position-node.position).normalized;forward.y=0;forward.Normalize();
                float offset=node.width>=13?4.65f:1.55f;Vector3 left=-Vector3.Cross(Vector3.up,forward)*offset;
                Vector3 a=node.position+left,b=graph.Nodes[next].position+left;
                bool supported=true;
                for(int sample=0;sample<=2;sample++)
                {
                    var p=Vector3.Lerp(a,b,sample*.5f);
                    int count=Physics.RaycastNonAlloc(p+Vector3.up*.7f,Vector3.down,hits,2f,~0,QueryTriggerInteraction.Ignore);bool floor=false;
                    for(int h=0;h<count;h++)if(hits[h].rigidbody==null&&hits[h].normal.y>.7f&&Mathf.Abs(hits[h].point.y-p.y)<.55f&&IsRoad(hits[h].collider)){floor=true;break;}
                    if(!floor)supported=false;
                }
                if(!supported){report.unsupportedLaneSamples++;if(report.details.Count<70)report.details.Add("Unsupported lane edge "+i+" -> "+next);}
                int obstacles=Physics.BoxCastNonAlloc(a+Vector3.up*.95f,new Vector3(.95f,.45f,.2f),forward,hits,Quaternion.LookRotation(forward),Vector3.Distance(a,b),~0,QueryTriggerInteraction.Ignore);
                bool blocked=false;string colliderName="";
                for(int h=0;h<obstacles;h++)if(hits[h].rigidbody==null&&hits[h].collider.GetComponentInParent<KombiShift2.Traffic.KombiTrafficVehicle>()==null&&hits[h].normal.y<.55f&&hits[h].collider.GetComponentInParent<CityIntersection>()==null){blocked=true;colliderName=hits[h].collider.name+" / "+string.Join(",",hits[h].collider.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name).Distinct());break;}
                if(!supported||blocked)report.laneIssues.Add(new LaneIssue{from=i,to=next,unsupported=!supported,blocked=blocked,collider=colliderName});
                if(blocked){report.blockedLaneSegments++;if(report.details.Count<70)report.details.Add("Blocked lane edge "+i+" -> "+next+" near "+a);}
            }
        }
        foreach(var trip in CityTripCatalog.Load().trips)
        {
            var path=graph.FindTripPath(trip);
            if(path.Count<2)report.details.Add("Unreachable selected trip "+trip.destination);else report.reachableTrips++;
        }
        if(report.markingMeshes<2||report.markingTriangles==0||report.markingColliders>0)report.details.Add("Road marking meshes are missing, empty, or collidable.");
        if(report.roadsideGuideVertices==0||report.unsupportedRoadsideGuideVertices>0||report.roadsideGuideVerticesOnRoad>0)
            report.details.Add("Roadside traffic guides are missing, unsupported, or overlap a road surface.");
        if(report.misplacedTrafficSignalHeads>0)report.details.Add("Traffic signal heads are missing or misplaced.");
        Directory.CreateDirectory("Docs/Validation/CityInfrastructure");File.WriteAllText("Docs/Validation/CityInfrastructure/scene-road-audit.json",JsonUtility.ToJson(report,true));
        Debug.Log("KOMBI_CITY_SCENE_AUDIT "+JsonUtility.ToJson(report));
    }
}
