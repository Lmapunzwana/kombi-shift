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
        public int markingMeshes,markingTriangles,markingColliders,roadwayMarkingVertices,unsupportedRoadwayMarkingVertices;
        public int roadsideGuideVertices,unsupportedRoadsideGuideVertices,roadsideGuideVerticesOnRoad;
        public int signalisedJunctions,trafficSignalHeads,misplacedTrafficSignalHeads;
        public int roadSigns,unsupportedRoadSigns,roadSignsOnRoad,misfacingRoadSigns;
        public int signalPosts,unsupportedSignalPosts,signalPostsOnRoad;
        public List<string> details=new();
        public List<LaneIssue> laneIssues=new();
        public int reachableTrips;
    }
    static bool IsRoad(Collider collider)
    {
        if(collider.GetComponentInParent<CityRoadSurface>()!=null)return true;
        var renderer=collider.GetComponent<Renderer>()??collider.GetComponentInParent<Renderer>();
        return renderer!=null&&renderer.sharedMaterials.Any(m=>m!=null&&(m.name.ToLowerInvariant().Contains("asphalt")||m.shader!=null&&m.shader.name=="Kombi Shift/Road Surface"));
    }
    static void CheckRoadsidePost(Transform post,out bool supported,out bool onRoad)
    {
        supported=false;onRoad=false;
        Vector3 ground=post.position-Vector3.up*post.lossyScale.y;
        foreach(var hit in Physics.RaycastAll(ground+Vector3.up*.2f,Vector3.down,.6f,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.rigidbody!=null||hit.normal.y<.65f||Mathf.Abs(hit.point.y-ground.y)>.18f)continue;
            if(IsRoad(hit.collider))onRoad=true;else supported=true;
        }
        if(!supported)supported=CityRoadsidePlacement.HasVisualGround(ground,.18f);
    }
    static bool IsFacingTraffic(Transform post)
    {
        var zone=post.GetComponentInParent<KombiShift2.Gameplay.KombiRoadRuleZone>();
        if(zone!=null)
        {
            var label=zone.transform.Find("Readable road sign");
            Vector3 approach=-zone.travelDirection;approach.y=0f;
            return label!=null&&approach.sqrMagnitude>.01f&&Vector3.Dot(label.forward,approach.normalized)>.8f;
        }
        var junction=post.GetComponentInParent<CityIntersection>();
        if(junction==null)return true;
        var sign=junction.transform.Find(post.name.Replace(" sign post"," sign"));
        if(sign==null)return false;
        return junction.arms.Any(arm=>arm!=null&&arm.incoming&&Vector3.Dot(sign.forward,arm.Direction)>.8f);
    }
    public static Record Run()
    {
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);Physics.SyncTransforms();
        CityRoadsidePlacement.RefreshVisualGroundCache();
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
            report.roadwayMarkingVertices=markings.Where(m=>m.name=="Roadway white markings"||m.name=="Roadway yellow markings")
                .Where(m=>m.sharedMesh!=null).Sum(m=>m.sharedMesh.vertexCount);
            var markingHits=new RaycastHit[8];
            foreach(var marking in markings.Where(m=>(m.name=="Roadway white markings"||m.name=="Roadway yellow markings")&&m.sharedMesh!=null))
                foreach(var vertex in marking.sharedMesh.vertices)
                {
                    Vector3 point=marking.transform.TransformPoint(vertex);
                    int count=Physics.RaycastNonAlloc(point+Vector3.up*.15f,Vector3.down,markingHits,.35f,~0,QueryTriggerInteraction.Ignore);
                    bool onRoad=false;
                    for(int i=0;i<count;i++)
                    {
                        var hit=markingHits[i];
                        if(hit.rigidbody==null&&hit.normal.y>.65f&&Mathf.Abs(hit.point.y-point.y)<.1f&&IsRoad(hit.collider)){onRoad=true;break;}
                    }
                    if(!onRoad)report.unsupportedRoadwayMarkingVertices++;
                }
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
        foreach(var post in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            bool isSign=post.name.EndsWith(" sign post",StringComparison.Ordinal);
            bool isSignal=post.name=="Traffic signal post";
            if(!isSign&&!isSignal)continue;
            CheckRoadsidePost(post,out bool supported,out bool onRoad);
            if(isSign)
            {
                report.roadSigns++;
                if(!supported){report.unsupportedRoadSigns++;report.details.Add("Unsupported sign post: "+post.name+" in "+post.parent.name+" at "+post.position);}
                if(onRoad){report.roadSignsOnRoad++;report.details.Add("Sign post on road: "+post.name+" in "+post.parent.name+" at "+post.position);}
                if(!IsFacingTraffic(post)){report.misfacingRoadSigns++;report.details.Add("Misfacing sign post: "+post.name+" in "+post.parent.name+" at "+post.position);}
            }
            else
            {
                report.signalPosts++;
                if(!supported){report.unsupportedSignalPosts++;report.details.Add("Unsupported signal post in "+post.parent.name+" at "+post.position);}
                if(onRoad){report.signalPostsOnRoad++;report.details.Add("Signal post on road in "+post.parent.name+" at "+post.position);}
            }
        }
        foreach(var junction in Object.FindObjectsByType<CityIntersection>())
        {
            if(!junction.signalised)continue;
            report.signalisedJunctions++;
            foreach(var arm in junction.arms)
            {
                if(arm==null||!arm.incoming||arm.control!="Signal")continue;
                report.trafficSignalHeads+=(arm.lampHead!=null?1:0)+(arm.repeater!=null?1:0);
                if(arm.lampHead==null)
                {report.misplacedTrafficSignalHeads++;continue;}
                Vector3 outward=arm.Direction;
                Vector3 offset=arm.lampHead.transform.position-(arm.approachPoint+outward*2f);offset.y=0f;
                float along=Vector3.Dot(offset,outward);
                float lateral=Mathf.Abs(Vector3.Dot(offset,Vector3.Cross(Vector3.up,outward)));
                if(along<-.5f||along>26f||lateral<.5f||lateral>arm.width*.5f+10f||
                    Vector3.Dot(arm.lampHead.transform.forward,outward)<.8f)report.misplacedTrafficSignalHeads++;
                if(arm.repeater!=null)
                {
                    Vector3 repeaterOffset=arm.repeater.transform.position-(arm.approachPoint+outward*2f);repeaterOffset.y=0f;
                    float repeaterAlong=Vector3.Dot(repeaterOffset,outward);
                    float repeaterLateral=Mathf.Abs(Vector3.Dot(repeaterOffset,Vector3.Cross(Vector3.up,outward)));
                    if(repeaterAlong<-.5f||repeaterAlong>10f||repeaterLateral<.5f||repeaterLateral>arm.width*.5f+4f||
                        Vector3.Dot(arm.repeater.transform.forward,outward)<.8f)report.misplacedTrafficSignalHeads++;
                }
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
        if(report.markingMeshes<3||report.markingTriangles==0||report.roadwayMarkingVertices==0||
            report.unsupportedRoadwayMarkingVertices>0||report.markingColliders>0)
            report.details.Add("Roadway markings are missing, unsupported, or collidable.");
        if(report.roadsideGuideVertices==0||report.unsupportedRoadsideGuideVertices>0||report.roadsideGuideVerticesOnRoad>0)
            report.details.Add("Roadside traffic guides are missing, unsupported, or overlap a road surface.");
        if(report.roadSigns==0||report.unsupportedRoadSigns>0||report.roadSignsOnRoad>0||report.misfacingRoadSigns>0)
            report.details.Add("Road signs are missing, unsupported, inside a road surface, or facing away from traffic.");
        if(report.unsupportedSignalPosts>0||report.signalPostsOnRoad>0)
            report.details.Add("Traffic signal posts are unsupported or inside a road surface.");
        if(report.misplacedTrafficSignalHeads>0)report.details.Add("Traffic signal heads are missing or misplaced.");
        Directory.CreateDirectory("Docs/Validation/CityInfrastructure");File.WriteAllText("Docs/Validation/CityInfrastructure/scene-road-audit.json",JsonUtility.ToJson(report,true));
        Debug.Log("KOMBI_CITY_SCENE_AUDIT "+JsonUtility.ToJson(report));
        return report;
    }
}
