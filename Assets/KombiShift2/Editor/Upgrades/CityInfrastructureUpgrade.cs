using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.City;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class KombiCityInfrastructureUpgrade
{
    const string Root="City Infrastructure";
    const string Generated="Assets/KombiShift2/Generated/CityNetwork";
    public static void VerifyRoadPhysics(){Apply();CityRoadPhysicsVerification.Run();}
    public static void VerifyGameplay(){Apply();KombiCityVerification.Run();}
    public static void VerifyBothStages(){Apply();KombiCityVerification.Both();}
    [MenuItem("Kombi Shift/Rebuild Classified Junction Controls")]
    public static void RebuildJunctionControls()
    {
        if(Application.isBatchMode)EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=KombiProjectWorkbench.Shift||scene.isDirty)
            throw new InvalidOperationException("Save the driving scene and stop Play mode before rebuilding junction controls.");
        var root=GameObject.Find(Root);var network=root!=null?root.GetComponent<CityRoadNetwork>():null;
        if(network==null)throw new InvalidOperationException("The saved city infrastructure is missing its road network.");
        network.Load(true);BuildIntersections(root.transform,network);Physics.SyncTransforms();BuildMarkings(root.transform,network);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        KombiCitySignalVerification.Run();
    }
    [MenuItem("Kombi Shift/Repair Road Signs And Markings")]
    public static void RepairRoadSignsAndMarkings()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before repairing road signs and markings.");
        var scene=SceneManager.GetActiveScene();
        if(scene.isDirty)throw new InvalidOperationException("Save the current scene before repairing road signs and markings.");
        if(scene.path!=KombiProjectWorkbench.Shift)scene=EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        if(scene.isDirty)throw new InvalidOperationException("Save the driving scene before repairing road signs and markings.");
        Directory.CreateDirectory("Review/BeforeRoadsideCleanup");
        const string backup="Review/BeforeRoadsideCleanup/KombiShift_FirstShift.unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        var root=GameObject.Find(Root);var network=root!=null?root.GetComponent<CityRoadNetwork>():null;
        if(network==null)throw new InvalidOperationException("The saved city infrastructure is missing its road network.");
        CityRoadsidePlacement.RefreshVisualGroundCache();
        network.Load(true);BuildIntersections(root.transform,network);Physics.SyncTransforms();BuildMarkings(root.transform,network);
        int signs=KombiShiftExpansionUpgrade.RepositionRoadSignsInOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        KombiCitySignalVerification.Run();
        var audit=KombiCityNetworkSceneAudit.Run();
        if(audit.roadSigns==0||audit.roadSignsOnRoad>0||audit.unsupportedRoadSigns>0||audit.misfacingRoadSigns>0||
            audit.signalPostsOnRoad>0||audit.unsupportedSignalPosts>0||audit.misplacedTrafficSignalHeads>0||
            audit.roadwayMarkingVertices==0||audit.unsupportedRoadsideGuideVertices>0||audit.roadsideGuideVerticesOnRoad>0)
            throw new InvalidOperationException("Roadside sign or marking audit failed; inspect scene-road-audit.json.");
        Debug.Log("KOMBI_ROADSIDE_CLEANUP signs="+signs+" roadSignsOnRoad="+audit.roadSignsOnRoad+
            " signalPostsOnRoad="+audit.signalPostsOnRoad+" roadwayMarkingVertices="+audit.roadwayMarkingVertices);
    }
    [MenuItem("Kombi Shift/Apply Existing City Infrastructure")]
    public static void Apply()
    {
        if(Application.isBatchMode)EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=KombiProjectWorkbench.Shift || scene.isDirty)
            throw new InvalidOperationException("Save the driving scene and stop Play mode before the city upgrade.");
        Directory.CreateDirectory("Review/BeforeCityInfrastructure");if(!File.Exists("Review/BeforeCityInfrastructure/KombiShift_FirstShift.unity"))File.Copy(scene.path,"Review/BeforeCityInfrastructure/KombiShift_FirstShift.unity");
        Directory.CreateDirectory(Generated);
        var root=GameObject.Find(Root);if(root==null)root=new GameObject(Root);
        var network=root.GetComponent<CityRoadNetwork>()??root.AddComponent<CityRoadNetwork>();
        network.network=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/CityRoadNetwork.json");network.Load(true);
        if(network.Nodes.Length==0)throw new InvalidOperationException("Bake the city road graph first.");
        var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/KombiShift2/Generated/City/City Asphalt.mat");
        asphalt.shader=Shader.Find("Kombi Shift/City Asphalt World");asphalt.enableInstancing=true;asphalt.SetColor("_Color",new Color(.75f,.75f,.75f));
        const string textures="Assets/KombiShift2/ThirdParty/PolyHaven/WornAsphalt/Textures/";
        asphalt.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"worn_asphalt_diff_2k.jpg"));
        asphalt.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"worn_asphalt_nor_gl_2k.jpg"));
        asphalt.SetTexture("_RoughnessMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"worn_asphalt_rough_2k.jpg"));
        asphalt.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"worn_asphalt_ao_2k.jpg"));EditorUtility.SetDirty(asphalt);
        int roads=0,holes=0;var report=new System.Text.StringBuilder();
        foreach(var filter in Object.FindObjectsByType<MeshFilter>().OrderBy(f=>f.name,StringComparer.Ordinal))
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null || filter.sharedMesh==null)continue;
            var surface=filter.GetComponent<CityRoadSurface>();
            if(surface==null && (!AssetDatabase.GetAssetPath(filter.sharedMesh).Contains("Environment/City") || !renderer.sharedMaterials.Any(m=>m!=null&&m.name=="road")))continue;
            roads++;surface??=filter.gameObject.AddComponent<CityRoadSurface>();
            if(surface.sourceMesh==null)surface.sourceMesh=filter.sharedMesh;
            filter.sharedMesh=surface.sourceMesh;
            var sourceCollider=filter.GetComponent<MeshCollider>()??filter.gameObject.AddComponent<MeshCollider>();sourceCollider.sharedMesh=surface.sourceMesh;
            Physics.SyncTransforms();
            int conditionKey=int.TryParse(filter.name.Trim(),out int roadNumber)?roadNumber%10:roads%10;
            surface.condition=conditionKey<2?RoadCondition.Severe:conditionKey<5?RoadCondition.Moderate:RoadCondition.Good;
            surface.potholes=Array.Empty<CityPothole>();
            var material=renderer.sharedMaterials;for(int i=0;i<material.Length;i++)if(material[i]!=null&&(material[i].name=="road"||material[i]==asphalt))material[i]=asphalt;renderer.sharedMaterials=material;
            if(surface.potholes.Length==0 && surface.condition!=RoadCondition.Good)
            {
                var candidates=network.Nodes.Where(n=>n.component==0&&!n.junction&&n.width>6&&renderer.bounds.Contains(n.position)).OrderBy(n=>n.position.x+n.position.z*7).ToArray();
                var selected=new List<CityPothole>();
                foreach(var node in candidates)
                {
                    if(selected.Count>=(surface.condition==RoadCondition.Severe?5:2))break;
                    if(selected.Any(h=>Vector3.Distance(h.centre,node.position)<45f))continue;
                    if(network.Nodes.Any(n=>n.junction&&Vector3.Distance(n.position,node.position)<18f))continue;
                    int ni=Array.IndexOf(network.Nodes,node);var next=network.Nodes[node.links[0]].position;
                    Vector3 forward=(next-node.position).normalized;Vector3 point=node.position-Vector3.Cross(Vector3.up,forward)*1.55f;
                    if(!Physics.Raycast(point+Vector3.up*.5f,Vector3.down,out var hit,2f,~0,QueryTriggerInteraction.Ignore)||hit.collider.gameObject!=filter.gameObject)continue;
                    selected.Add(new CityPothole{centre=hit.point,forward=forward,radius=surface.condition==RoadCondition.Severe?1.1f:.8f,depth=surface.condition==RoadCondition.Severe?.18f:.09f});
                }
                surface.potholes=selected.ToArray();
            }
            var mesh=CutPotholes(surface.sourceMesh,filter.transform,surface.potholes);
            // Stable mesh path on repeated upgrades, independent of transient scene instance IDs.
            string path=Generated+"/Road-"+filter.name.Trim().Replace('/','-')+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else {EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
            filter.sharedMesh=saved;var collider=filter.GetComponent<MeshCollider>()??filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=saved;
            holes+=surface.potholes.Length;report.AppendLine(filter.name+": "+surface.condition+", physical depressions "+surface.potholes.Length);
        }
        foreach(var stop in Object.FindObjectsByType<KombiShift2.Gameplay.PassengerStation>())
        {
            if(stop.TryGetComponent<Renderer>(out var marker)&&!marker.enabled&&stop.TryGetComponent<Collider>(out var markerCollider))markerCollider.enabled=false;
            int closest=network.Nearest(stop.ServicePoint.position,0);if(closest<0)continue;
            var service=stop.ServicePoint;Vector3 servicePosition=service.position;Quaternion rotation=service.rotation;
            var group=service.parent;Vector3 left=-service.right;var p=network.Nodes[closest].position+left*(network.Nodes[closest].width*.5f+1.5f);p.y=group.position.y;
            group.position=p;service.SetPositionAndRotation(servicePosition,rotation);
        }
        Physics.SyncTransforms();
        RepairCityFoliage(root.transform);
        BuildIntersections(root.transform,network);Physics.SyncTransforms();
        BuildMarkings(root.transform,network);
        var stationPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/CityPassengerStation.prefab");
        if(stationPrefab==null)
        {
            var originalStation=Object.FindObjectsByType<KombiShift2.Gameplay.PassengerStation>().First(s=>!s.Terminal && s.People.Length==6);
            var template=Object.Instantiate(originalStation.ServicePoint.parent.gameObject);
            template.name="City Passenger Station";
            var station=template.GetComponent<KombiShift2.Gameplay.PassengerStation>()??template.AddComponent<KombiShift2.Gameplay.PassengerStation>();
            station.Configure(template.GetComponentsInChildren<KombiShift2.Gameplay.StationPassenger>(),template.transform.Find("Kombi Service Point"),template.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Bench Seat ")).ToArray(),false);
            stationPrefab=PrefabUtility.SaveAsPrefabAsset(template,Generated+"/CityPassengerStation.prefab");Object.DestroyImmediate(template);
        }
        var session=root.GetComponent<CityTripSession>()??root.AddComponent<CityTripSession>();session.stationPrefab=stationPrefab;
        if(root.GetComponent<CityNavigation>()==null)root.AddComponent<CityNavigation>();
        if(root.GetComponent<CityTrafficFleet>()==null)root.AddComponent<CityTrafficFleet>();
        var culling=root.GetComponent<CityDistanceCulling>()??root.AddComponent<CityDistanceCulling>();
        culling.cityRenderers=Object.FindObjectsByType<MeshFilter>().Where(m=>AssetDatabase.GetAssetPath(m.sharedMesh).Contains("Environment/City")||m.GetComponent<CityRoadSurface>()!=null).Select(m=>m.GetComponent<Renderer>()).Where(r=>r!=null).ToArray();
        var director=Object.FindFirstObjectByType<KombiShift2.Gameplay.KombiShiftGameDirector>();
        if(director.GetComponent<KombiShift2.UI.KombiDropoffMarker>()==null)director.gameObject.AddComponent<KombiShift2.UI.KombiDropoffMarker>();
        foreach(var car in Object.FindObjectsByType<KombiPassengerCabin>())
        {
            if(car.GetComponent<KombiVehicleCondition>()==null)car.gameObject.AddComponent<KombiVehicleCondition>();
            if(car.GetComponent<KombiRoadWear>()==null)car.gameObject.AddComponent<KombiRoadWear>();
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Docs/Validation/CityInfrastructure");File.WriteAllText("Docs/Validation/CityInfrastructure/surface-upgrade.txt","Road meshes "+roads+"; physical potholes "+holes+"\n"+report);
        Debug.Log("KOMBI_CITY_SURFACES_SAVED roads="+roads+" potholes="+holes);
        KombiCityNetworkSceneAudit.Run();
    }
    static Mesh CutPotholes(Mesh source,Transform transform,CityPothole[] holes)
    {
        var vertices=source.vertices;var output=new List<Vector3>();var triangles=new List<int>();
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var indices=source.GetTriangles(sub);
            for(int i=0;i<indices.Length;i+=3)
            {
                var polygons=new List<List<Vector3>>{new(){transform.TransformPoint(vertices[indices[i]]),transform.TransformPoint(vertices[indices[i+1]]),transform.TransformPoint(vertices[indices[i+2]])}};
                foreach(var hole in holes)
                {
                    var remaining=new List<List<Vector3>>();
                    foreach(var polygon in polygons)
                    {
                        var bounds=new Bounds(polygon[0],Vector3.zero);foreach(var p in polygon)bounds.Encapsulate(p);
                        if(!bounds.Intersects(new Bounds(hole.centre,new Vector3(hole.radius*2,.8f,hole.radius*2)))){remaining.Add(polygon);continue;}
                        var inside=polygon;
                        foreach(var plane in new[]{new Vector4(1,0,0,-hole.centre.x+hole.radius),new Vector4(-1,0,0,hole.centre.x+hole.radius),new Vector4(0,0,1,-hole.centre.z+hole.radius),new Vector4(0,0,-1,hole.centre.z+hole.radius)})
                        {if(inside.Count<3)break;var outside=Clip(inside,-plane);if(outside.Count>=3)remaining.Add(outside);inside=Clip(inside,plane);}
                        if(inside.Count>=3)for(int t=1;t<inside.Count-1;t++)Subdivide(inside[0],inside[t],inside[t+1],hole,remaining,0);
                    }
                    polygons=remaining;
                }
                foreach(var polygon in polygons)for(int t=1;t<polygon.Count-1;t++)
                {int start=output.Count;output.Add(transform.InverseTransformPoint(polygon[0]));output.Add(transform.InverseTransformPoint(polygon[t]));output.Add(transform.InverseTransformPoint(polygon[t+1]));triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);}
            }
        }
        var mesh=new Mesh{name=source.name+" Physical Road",indexFormat=IndexFormat.UInt32};mesh.SetVertices(output);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var uv=output.Select(p=>new Vector2(p.x,p.z)).ToArray();mesh.uv=uv;mesh.RecalculateTangents();return mesh;
    }
    static List<Vector3> Clip(List<Vector3> input,Vector4 plane)
    {
        var result=new List<Vector3>();if(input.Count==0)return result;
        float D(Vector3 p)=>p.x*plane.x+p.z*plane.z+plane.w;
        Vector3 previous=input[input.Count-1];float a=D(previous);
        foreach(var current in input){float b=D(current);if((a>=0)!=(b>=0))result.Add(Vector3.Lerp(previous,current,a/(a-b)));if(b>=0)result.Add(current);previous=current;a=b;}return result;
    }
    static void Subdivide(Vector3 a,Vector3 b,Vector3 c,CityPothole hole,List<List<Vector3>> output,int level)
    {
        if(level<5&&Mathf.Max(Vector3.Distance(a,b),Mathf.Max(Vector3.Distance(b,c),Vector3.Distance(c,a)))>.4f)
        {Vector3 ab=(a+b)*.5f,bc=(b+c)*.5f,ca=(c+a)*.5f;Subdivide(a,ab,ca,hole,output,level+1);Subdivide(ab,b,bc,hole,output,level+1);Subdivide(ca,bc,c,hole,output,level+1);Subdivide(ab,bc,ca,hole,output,level+1);return;}
        Vector3 Lower(Vector3 p)=>p-Vector3.up*hole.Depression(p);
        output.Add(new List<Vector3>{Lower(a),Lower(b),Lower(c)});
    }
    static void RepairCityFoliage(Transform root)
    {
        var group=root.Find("City tree trunk collisions");if(group==null){group=new GameObject("City tree trunk collisions").transform;group.SetParent(root,false);}
        var existing=new List<Vector3>();foreach(var collider in Object.FindObjectsByType<CapsuleCollider>())if(collider.name.ToLowerInvariant().Contains("trunk"))existing.Add(collider.bounds.center);
        foreach(var collider in Object.FindObjectsByType<MeshCollider>())
        {
            string name=collider.name.ToLowerInvariant();if(!name.Contains("tree")||!name.Contains("plane"))continue;
            var bounds=collider.GetComponent<Renderer>()!=null?collider.GetComponent<Renderer>().bounds:collider.bounds;collider.enabled=false;
            Vector3 position=new(bounds.center.x,bounds.min.y+1.25f,bounds.center.z);
            if(existing.Any(p=>new Vector2(p.x-position.x,p.z-position.z).sqrMagnitude<.5f))continue;
            var trunk=new GameObject("City tree trunk");trunk.transform.SetParent(group,false);trunk.transform.position=position;
            var capsule=trunk.AddComponent<CapsuleCollider>();capsule.radius=.16f;capsule.height=2.5f;existing.Add(position);
        }
    }
    [Serializable] sealed class JunctionDocument { public JunctionRecord[] junctions; }
    [Serializable] sealed class JunctionRecord { public string id,type,control;public Vector3 centre;public float radius;public ArmRecord[] arms; }
    [Serializable] sealed class ArmRecord { public string id,control;public float bearing,width;public bool major,incoming,outgoing;public int approachNode;public Vector3 approachLanePoint; }
    static void BuildIntersections(Transform root,CityRoadNetwork network)
    {
        var source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/CityJunctions.json");
        if(source==null)throw new InvalidOperationException("Classify city junctions before applying the city infrastructure.");
        var document=JsonUtility.FromJson<JunctionDocument>(source.text);
        if(document?.junctions==null||document.junctions.Length==0)throw new InvalidDataException("CityJunctions.json contains no junctions.");
        var old=root.Find("City intersections");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var group=new GameObject("City intersections").transform;group.SetParent(root,false);
        var lampMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/TrafficLamp.mat");
        if(lampMaterial==null){lampMaterial=new Material(Shader.Find("Standard"));lampMaterial.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(lampMaterial,Generated+"/TrafficLamp.mat");}
        foreach(var record in document.junctions)
        {
            var go=new GameObject(record.id+" — "+record.type+" — "+record.control);go.transform.SetParent(group,false);go.transform.position=record.centre;
            var intersection=go.AddComponent<CityIntersection>();intersection.junctionId=record.id;intersection.junctionType=record.type;intersection.control=record.control;
            intersection.signalised=record.control=="Signalised";intersection.roundabout=record.control=="RoundaboutYield";intersection.radius=record.radius;
            var arms=record.arms??Array.Empty<ArmRecord>();intersection.stopControlled=arms.Any(a=>a.control=="Stop");
            var runtimeArms=new List<CityIntersectionArm>();
            int nextMinorPhase=arms.Any(a=>a.major)?1:0;
            foreach(var arm in arms)
            {
                int phase=arm.major?0:nextMinorPhase;
                int approachIndex=arm.approachNode;
                if(approachIndex<0||approachIndex>=network.Nodes.Length||network.Nodes[approachIndex].component!=0)
                    approachIndex=network.Nearest(arm.approachLanePoint,0);
                if(approachIndex<0)throw new InvalidOperationException(record.id+"/"+arm.id+" has no road-network approach node.");
                var approachNode=network.Nodes[approachIndex];
                float approachWidth=approachNode.width>0f?approachNode.width:arm.width;
                var runtime=new CityIntersectionArm{id=arm.id,control=arm.control,bearing=arm.bearing,width=approachWidth,major=arm.major,incoming=arm.incoming,outgoing=arm.outgoing,phase=phase,approachPoint=approachNode.position};
                Vector3 outward=new(Mathf.Sin(arm.bearing*Mathf.Deg2Rad),0,Mathf.Cos(arm.bearing*Mathf.Deg2Rad));
                Vector3 stop=approachNode.position+outward*2f;
                if(arm.control=="Signal"&&arm.incoming)
                {
                    if(!CityRoadsidePlacement.TryFind(stop,outward,approachWidth,1,out var lampPosition,out bool needsFooting,24f,10f))
                        throw new InvalidOperationException(record.id+"/"+arm.id+" has no supported roadside location for its signal.");
                    if(needsFooting)CityRoadsidePlacement.CreateFooting(go.transform,lampPosition);
                    runtime.lampHead=MakeSignalHead(go.transform,"Signal lamp head "+arm.id,lampPosition,outward,lampMaterial);
                    if(approachWidth>=16f)
                    {
                        if(CityRoadsidePlacement.TryFind(stop,outward,approachWidth,-1,out var repeaterPosition,out bool repeaterNeedsFooting))
                        {
                            if(repeaterNeedsFooting)CityRoadsidePlacement.CreateFooting(go.transform,repeaterPosition);
                            runtime.repeater=MakeSignalHead(go.transform,"Far-side repeater "+arm.id,repeaterPosition,outward,lampMaterial);
                        }
                        else Debug.LogWarning("Omitted far-side repeater at "+record.id+"/"+arm.id+" because the opposite roadside is unsupported.");
                    }
                    if(CityRoadsidePlacement.TryFind(stop+outward*8f,outward,approachWidth,1,out var signalSignPosition,out bool signalSignNeedsFooting))
                    {
                        if(signalSignNeedsFooting)CityRoadsidePlacement.CreateFooting(go.transform,signalSignPosition);
                        MakeJunctionSign(go.transform,"SIGNAL AHEAD",signalSignPosition,outward);
                    }
                    else Debug.LogWarning("Omitted SIGNAL AHEAD sign at "+record.id+"/"+arm.id+" because no supported roadside ground is available.");
                }
                else if((arm.control=="Stop"||arm.control=="GiveWay")&&arm.incoming)
                {
                    if(CityRoadsidePlacement.TryFind(stop,outward,approachWidth,1,out var signPosition,out bool signNeedsFooting))
                    {
                        if(signNeedsFooting)CityRoadsidePlacement.CreateFooting(go.transform,signPosition);
                        MakeJunctionSign(go.transform,arm.control=="Stop"?"STOP":"GIVE WAY",signPosition,outward);
                    }
                    else Debug.LogWarning("Omitted "+arm.control+" sign at "+record.id+"/"+arm.id+" because no supported roadside ground is available.");
                }
                runtimeArms.Add(runtime);
            }
            intersection.arms=runtimeArms.ToArray();
        }
    }
    static Renderer MakeSignalHead(Transform parent,string name,Vector3 position,Vector3 facing,Material material)
    {
        var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);post.name="Traffic signal post";post.transform.SetParent(parent);post.transform.position=position+Vector3.up*1.5f;post.transform.localScale=new Vector3(.12f,1.5f,.12f);
        var lamp=GameObject.CreatePrimitive(PrimitiveType.Sphere);lamp.name=name;lamp.transform.SetParent(parent);lamp.transform.position=position+Vector3.up*3f;lamp.transform.localScale=Vector3.one*.4f;lamp.transform.rotation=Quaternion.LookRotation(facing.normalized);
        Object.DestroyImmediate(lamp.GetComponent<Collider>());var renderer=lamp.GetComponent<Renderer>();renderer.sharedMaterial=material;return renderer;
    }
    static float ApproachWidth(CityRoadNetwork network,Vector3 centre,Vector3 forward)
    {
        float nearest=float.MaxValue,width=7f;
        foreach(var node in network.Nodes)
        {
            if(node.component!=0||node.junction||node.roundabout)continue;
            Vector3 delta=node.position-centre;delta.y=0f;float distance=delta.magnitude;
            if(distance<.5f||distance>45f||Vector3.Dot(delta/distance,-forward)<.7f||distance>=nearest)continue;
            nearest=distance;width=node.width;
        }
        return width;
    }
    static void MakeJunctionSign(Transform parent,string label,Vector3 position,Vector3 facing)
    {
        var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);post.name=label+" sign post";post.transform.SetParent(parent);post.transform.position=position+Vector3.up;post.transform.localScale=new Vector3(.07f,1,.07f);
        var sign=new GameObject(label+" sign",typeof(TextMesh));sign.transform.SetParent(parent);sign.transform.position=position+Vector3.up*2.1f;sign.transform.rotation=Quaternion.LookRotation(facing.normalized);
        var text=sign.GetComponent<TextMesh>();text.text=label;text.fontSize=40;text.characterSize=.04f;text.anchor=TextAnchor.MiddleCenter;text.color=label=="STOP"?new Color(1,.35f,.25f):Color.white;
    }
    static void BuildMarkings(Transform root,CityRoadNetwork network)
    {
        var legacy=GameObject.Find("Road carriage markings");if(legacy!=null)legacy.SetActive(false);
        var edges=GameObject.Find("Road edge lines");if(edges!=null)edges.SetActive(false);
        var old=root.Find("City road markings");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var group=new GameObject("City road markings").transform;group.SetParent(root,false);
        var whiteVertices=new List<Vector3>();var whiteTriangles=new List<int>();
        var yellowVertices=new List<Vector3>();var yellowTriangles=new List<int>();
        var guideVertices=new List<Vector3>();var guideTriangles=new List<int>();
        var paintHits=new RaycastHit[24];
        bool GroundPaint(Vector3 point,out Vector3 projected)
        {
            int count=Physics.RaycastNonAlloc(point+Vector3.up*3,Vector3.down,paintHits,6f,~0,QueryTriggerInteraction.Ignore);float highest=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var hit=paintHits[i];if(hit.rigidbody!=null||hit.normal.y<.65f)continue;
                var road=hit.collider.GetComponent<CityRoadSurface>();var renderer=hit.collider.GetComponent<Renderer>();
                if(road==null&&(renderer==null||!renderer.sharedMaterials.Any(m=>m!=null&&(m.name.Contains("Asphalt")||m.shader.name=="Kombi Shift/Road Surface"))))continue;
                highest=Mathf.Max(highest,hit.point.y);
            }
            projected=new Vector3(point.x,highest+.025f,point.z);return !float.IsNegativeInfinity(highest);
        }
        bool GroundRoadside(Vector3 point,out Vector3 projected)
        {
            int count=Physics.RaycastNonAlloc(point+Vector3.up*3,Vector3.down,paintHits,6f,~0,QueryTriggerInteraction.Ignore);
            float highest=float.NegativeInfinity,roadHeight=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var hit=paintHits[i];if(hit.rigidbody!=null||hit.normal.y<.65f||Mathf.Abs(hit.point.y-point.y)>.9f)continue;
                var renderer=hit.collider.GetComponent<Renderer>();
                bool isRoad=hit.collider.GetComponent<CityRoadSurface>()!=null||
                    renderer!=null&&renderer.sharedMaterials.Any(m=>m!=null&&(m.name.Contains("Asphalt")||m.shader.name=="Kombi Shift/Road Surface"));
                if(isRoad){roadHeight=Mathf.Max(roadHeight,hit.point.y);continue;}
                highest=Mathf.Max(highest,hit.point.y);
            }
            if(float.IsNegativeInfinity(highest)||!float.IsNegativeInfinity(roadHeight)&&Mathf.Abs(roadHeight-(highest+.035f))<=.1f)
            {projected=default;return false;}
            projected=new Vector3(point.x,highest+.035f,point.z);return !float.IsNegativeInfinity(highest);
        }
        void Strip(Vector3 a,Vector3 b,float width,List<Vector3> vertices,List<int> triangles)
        {
            Vector3 direction=b-a;direction.y=0f;if(direction.sqrMagnitude<.01f)return;
            Vector3 right=Vector3.Cross(Vector3.up,direction.normalized)*width*.5f;
            if(!GroundPaint(a-right,out var p0)||!GroundPaint(a+right,out var p1)||!GroundPaint(b+right,out var p2)||!GroundPaint(b-right,out var p3))return;
            int n=vertices.Count;vertices.Add(p0);vertices.Add(p1);vertices.Add(p2);vertices.Add(p3);triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
        }
        void RoadsideStrip(Vector3 a,Vector3 b,float width)
        {
            Vector3 direction=b-a;direction.y=0f;if(direction.sqrMagnitude<.01f)return;
            Vector3 right=Vector3.Cross(Vector3.up,direction.normalized)*width*.5f;
            if(!GroundRoadside(a-right,out var p0)||!GroundRoadside(a+right,out var p1)||!GroundRoadside(b+right,out var p2)||!GroundRoadside(b-right,out var p3))return;
            int n=guideVertices.Count;guideVertices.Add(p0);guideVertices.Add(p1);guideVertices.Add(p2);guideVertices.Add(p3);guideTriangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
        }
        void RoadLine(Vector3[] points,float offset,float width,List<Vector3> vertices,List<int> triangles)
        {
            for(int i=1;i<points.Length;i++)
            {
                Vector3 direction=points[i]-points[i-1];direction.y=0f;if(direction.sqrMagnitude<.01f)continue;
                Vector3 right=Vector3.Cross(Vector3.up,direction.normalized);
                Strip(points[i-1]+right*offset,points[i]+right*offset,width,vertices,triangles);
            }
        }
        void DashedRoadLine(Vector3[] points,float offset,float width,List<Vector3> vertices,List<int> triangles)
        {
            const float dashLength=3.6f,period=6f;
            float distance=0f;
            for(int i=1;i<points.Length;i++)
            {
                Vector3 direction=points[i]-points[i-1];direction.y=0f;float length=direction.magnitude;if(length<.1f)continue;
                direction/=length;Vector3 right=Vector3.Cross(Vector3.up,direction);
                float segmentEnd=distance+length;
                for(float dash=Mathf.Floor(distance/period)*period;dash<segmentEnd;dash+=period)
                {
                    float from=Mathf.Max(distance,dash+.3f),to=Mathf.Min(segmentEnd,dash+dashLength);
                    if(to-from<.25f)continue;
                    Vector3 a=Vector3.Lerp(points[i-1],points[i],(from-distance)/length)+right*offset;
                    Vector3 b=Vector3.Lerp(points[i-1],points[i],(to-distance)/length)+right*offset;
                    Strip(a,b,width,vertices,triangles);
                }
                distance=segmentEnd;
            }
        }
        var adjacency=Enumerable.Range(0,network.Nodes.Length).Select(_=>new List<int>()).ToArray();
        var seen=new HashSet<long>();
        for(int i=0;i<network.Nodes.Length;i++)
        {
            var node=network.Nodes[i];if(node.component!=0)continue;
            foreach(int next in node.links)
            {
                if(next<0||next>=network.Nodes.Length)continue;
                int low=Mathf.Min(i,next),high=Mathf.Max(i,next);long key=((long)low<<32)|(uint)high;if(!seen.Add(key))continue;
                var other=network.Nodes[next];if(other.component!=0||node.junction||other.junction||node.roundabout||other.roundabout)continue;
                adjacency[i].Add(next);adjacency[next].Add(i);
                Vector3 direction=other.position-node.position;direction.y=0f;if(direction.sqrMagnitude<.01f)continue;direction.Normalize();
                Vector3 right=Vector3.Cross(Vector3.up,direction);float roadWidth=Mathf.Min(node.width,other.width);
                float roadside=roadWidth*.5f+.8f;
                RoadsideStrip(node.position+right*roadside,other.position+right*roadside,.08f);
                RoadsideStrip(node.position-right*roadside,other.position-right*roadside,.08f);
            }
        }
        bool CanContinue(int previous,int current,int next)
        {
            if(adjacency[current].Count!=2)return false;
            var before=network.Nodes[previous];var at=network.Nodes[current];var after=network.Nodes[next];
            if(Mathf.Abs(before.width-at.width)>.2f||Mathf.Abs(after.width-at.width)>.2f||
                before.allowPassing!=at.allowPassing||after.allowPassing!=at.allowPassing)return false;
            Vector3 incoming=at.position-before.position,outgoing=after.position-at.position;incoming.y=0f;outgoing.y=0f;
            return incoming.sqrMagnitude>.01f&&outgoing.sqrMagnitude>.01f&&Vector3.Angle(incoming,outgoing)<35f;
        }
        bool IsBreak(int index)
        {
            if(adjacency[index].Count!=2)return true;
            return !CanContinue(adjacency[index][0],index,adjacency[index][1]);
        }
        long EdgeKey(int a,int b)=>((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
        var consumed=new HashSet<long>();var chains=new List<List<int>>();
        void Trace(int start,int first)
        {
            var chain=new List<int>{start};int previous=start,current=first;
            while(consumed.Add(EdgeKey(previous,current)))
            {
                chain.Add(current);
                if(adjacency[current].Count!=2)break;
                int next=adjacency[current][0]==previous?adjacency[current][1]:adjacency[current][0];
                if(!CanContinue(previous,current,next)||consumed.Contains(EdgeKey(current,next)))break;
                previous=current;current=next;
            }
            if(chain.Count>1)chains.Add(chain);
        }
        for(int i=0;i<adjacency.Length;i++)if(IsBreak(i))
            foreach(int next in adjacency[i])if(!consumed.Contains(EdgeKey(i,next)))Trace(i,next);
        for(int i=0;i<adjacency.Length;i++)foreach(int next in adjacency[i])
            if(!consumed.Contains(EdgeKey(i,next)))Trace(i,next);
        foreach(var chain in chains)
        {
            var points=chain.Select(i=>network.Nodes[i].position).ToArray();
            float roadWidth=network.Nodes[chain[0]].width;
            bool passing=roadWidth>=13f&&chain.All(i=>network.Nodes[i].allowPassing);
            RoadLine(points,roadWidth*.5f-.25f,.12f,whiteVertices,whiteTriangles);
            RoadLine(points,-roadWidth*.5f+.25f,.12f,whiteVertices,whiteTriangles);
            if(roadWidth>=13f)
            {
                RoadLine(points,.16f,.14f,yellowVertices,yellowTriangles);
                RoadLine(points,-.16f,.14f,yellowVertices,yellowTriangles);
                if(passing)
                {
                    DashedRoadLine(points,3.1f,.12f,whiteVertices,whiteTriangles);
                    DashedRoadLine(points,-3.1f,.12f,whiteVertices,whiteTriangles);
                }
            }
            else RoadLine(points,0f,.14f,yellowVertices,yellowTriangles);
        }
        foreach(var junction in Object.FindObjectsByType<CityIntersection>())
        {
            if(junction.roundabout)continue;
            foreach(var arm in junction.arms)
            {
                if(arm==null||!arm.incoming)continue;
                Vector3 outward=arm.Direction,left=Vector3.Cross(Vector3.up,outward);float roadWidth=arm.width;
                if(arm.control=="Signal"||arm.control=="Stop")
                {
                    Vector3 stop=arm.approachPoint+outward*(arm.control=="Signal"?2f:1.5f);
                    Strip(stop+left*.2f,stop+left*(roadWidth*.5f-.25f),.35f,whiteVertices,whiteTriangles);
                }
                if(arm.control=="Signal")
                {
                    for(int stripe=0;stripe<6;stripe++)
                    {
                        Vector3 crossing=arm.approachPoint-outward*(1f+stripe*.65f);
                        Strip(crossing-left*(roadWidth*.5f-.25f),crossing+left*(roadWidth*.5f-.25f),.42f,whiteVertices,whiteTriangles);
                    }
                }
            }
        }
        var white=AssetDatabase.LoadAssetAtPath<Material>("Assets/KombiShift2/Generated/City/City Road Marking.mat");
        var yellow=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/City Road Marking Yellow.mat");
        if(yellow==null){yellow=new Material(white){name="City Road Marking Yellow"};AssetDatabase.CreateAsset(yellow,Generated+"/City Road Marking Yellow.mat");}
        white.SetColor("_Color",new Color(.9f,.92f,.9f));white.enableInstancing=true;EditorUtility.SetDirty(white);
        yellow.SetColor("_Color",new Color(1f,.78f,.08f));yellow.enableInstancing=true;EditorUtility.SetDirty(yellow);
        var whiteMesh=SaveMarkingMesh(whiteVertices,whiteTriangles,"City white road markings",Generated+"/CityMarkings.asset");
        var yellowMesh=SaveMarkingMesh(yellowVertices,yellowTriangles,"City yellow centre markings",Generated+"/CityMarkingsYellow.asset");
        var guideMesh=SaveMarkingMesh(guideVertices,guideTriangles,"City roadside traffic guides",Generated+"/CityRoadsideGuides.asset");
        var whiteObject=new GameObject("Roadway white markings",typeof(MeshFilter),typeof(MeshRenderer));whiteObject.transform.SetParent(group,false);
        whiteObject.GetComponent<MeshFilter>().sharedMesh=whiteMesh;whiteObject.GetComponent<MeshRenderer>().sharedMaterial=white;
        var yellowObject=new GameObject("Roadway yellow markings",typeof(MeshFilter),typeof(MeshRenderer));yellowObject.transform.SetParent(group,false);
        yellowObject.GetComponent<MeshFilter>().sharedMesh=yellowMesh;yellowObject.GetComponent<MeshRenderer>().sharedMaterial=yellow;
        var guideObject=new GameObject("Roadside traffic guide mesh",typeof(MeshFilter),typeof(MeshRenderer));guideObject.transform.SetParent(group,false);
        guideObject.GetComponent<MeshFilter>().sharedMesh=guideMesh;guideObject.GetComponent<MeshRenderer>().sharedMaterial=yellow;
    }
    static Mesh SaveMarkingMesh(List<Vector3> vertices,List<int> triangles,string name,string path)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(asset==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,asset);Object.DestroyImmediate(mesh);return asset;
    }
}
