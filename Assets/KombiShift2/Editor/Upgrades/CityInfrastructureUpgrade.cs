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
        BuildIntersections(root.transform,network);
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
    static void BuildIntersections(Transform root,CityRoadNetwork network)
    {
        var old=root.Find("City intersections");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var group=new GameObject("City intersections").transform;group.SetParent(root,false);
        var positions=new List<Vector3>();
        var circleNodes=network.Nodes.Where(n=>n.roundabout).ToArray();
        if(circleNodes.Length>0)
        {
            var centre=new Vector3(-656,circleNodes.Average(n=>n.position.y),262);
            var circle=new GameObject("Existing city roundabout — clockwise");circle.transform.SetParent(group,false);circle.transform.position=centre;
            var roundabout=circle.AddComponent<CityIntersection>();roundabout.roundabout=true;roundabout.radius=40;
            positions.Add(centre);
        }
        var lampMaterial=AssetDatabase.LoadAssetAtPath<Material>(Generated+"/TrafficLamp.mat");
        if(lampMaterial==null){lampMaterial=new Material(Shader.Find("Standard"));lampMaterial.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(lampMaterial,Generated+"/TrafficLamp.mat");}
        foreach(var node in network.Nodes)
        {
            if(node.component!=0||!node.junction||node.roundabout||positions.Any(p=>Vector3.Distance(p,node.position)<22))continue;
            positions.Add(node.position);var go=new GameObject("City junction "+positions.Count);go.transform.SetParent(group,false);go.transform.position=node.position;
            var intersection=go.AddComponent<CityIntersection>();intersection.signalised=positions.Count%3==0;intersection.stopControlled=!intersection.signalised&&positions.Count%2==0;intersection.radius=8;
            MakeJunctionSign(go.transform,intersection.signalised?"SIGNAL AHEAD":intersection.stopControlled?"STOP":"GIVE WAY",Mathf.Max(6,node.width*.5f+1));
            if(intersection.signalised)
            {
                var eastWest=new List<Renderer>();var northSouth=new List<Renderer>();
                foreach(var forward in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    float width=ApproachWidth(network,node.position,forward);
                    Vector3 left=-Vector3.Cross(Vector3.up,forward);
                    Vector3 position=node.position-forward*(intersection.radius+1f)+left*(width*.5f+.9f);
                    var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);post.name="Traffic signal post";post.transform.SetParent(go.transform,false);
                    post.transform.localPosition=new Vector3(position.x-node.position.x,1.5f,position.z-node.position.z);post.transform.localScale=new Vector3(.12f,1.5f,.12f);
                    var lamp=GameObject.CreatePrimitive(PrimitiveType.Sphere);lamp.name="Signal lamp";lamp.transform.SetParent(go.transform,false);
                    lamp.transform.localPosition=post.transform.localPosition+Vector3.up*1.5f;lamp.transform.localScale=Vector3.one*.4f;
                    Object.DestroyImmediate(lamp.GetComponent<Collider>());lamp.GetComponent<Renderer>().sharedMaterial=lampMaterial;
                    if(Mathf.Abs(forward.x)>Mathf.Abs(forward.z))eastWest.Add(lamp.GetComponent<Renderer>());else northSouth.Add(lamp.GetComponent<Renderer>());
                }
                intersection.eastWestLamps=eastWest.ToArray();intersection.northSouthLamps=northSouth.ToArray();
            }
        }
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
    static void MakeJunctionSign(Transform parent,string label,float roadside)
    {
        var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);post.name=label+" sign post";post.transform.SetParent(parent,false);post.transform.localPosition=new Vector3(roadside,1,-9);post.transform.localScale=new Vector3(.07f,1,.07f);
        var sign=new GameObject(label+" sign",typeof(TextMesh));sign.transform.SetParent(parent,false);sign.transform.localPosition=new Vector3(roadside,2.1f,-9);
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
        void Strip(Vector3 a,Vector3 b,float width,List<Vector3> vertices,List<int> triangles)
        {
            Vector3 right=Vector3.Cross(Vector3.up,(b-a).normalized)*width*.5f;
            if(!GroundPaint(a-right,out var p0)||!GroundPaint(a+right,out var p1)||!GroundPaint(b+right,out var p2)||!GroundPaint(b-right,out var p3))return;
            int n=vertices.Count;vertices.Add(p0);vertices.Add(p1);vertices.Add(p2);vertices.Add(p3);triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
        }
        void Dashed(Vector3 a,Vector3 b,float width,List<Vector3> vertices,List<int> triangles)
        {
            Vector3 direction=b-a;direction.y=0f;float length=direction.magnitude;if(length<.1f)return;direction/=length;
            float first=Vector3.Dot(a,direction),last=Vector3.Dot(b,direction);
            Vector3 lowPoint=first<last?a:b,highPoint=first<last?b:a;
            float low=Mathf.Min(first,last),high=Mathf.Max(first,last);
            const float period=6f,dashLength=3.6f;
            for(float dash=Mathf.Floor(low/period)*period;dash<high;dash+=period)
            {
                float from=Mathf.Max(low,dash+.4f),to=Mathf.Min(high,dash+dashLength);
                if(to-from<.2f)continue;
                Strip(Vector3.Lerp(lowPoint,highPoint,(from-low)/(high-low)),Vector3.Lerp(lowPoint,highPoint,(to-low)/(high-low)),width,vertices,triangles);
            }
        }
        var seen=new HashSet<long>();
        for(int i=0;i<network.Nodes.Length;i++)
        {
            var node=network.Nodes[i];if(node.component!=0)continue;
            foreach(int next in node.links)
            {
                int low=Mathf.Min(i,next),high=Mathf.Max(i,next);long key=((long)low<<32)|(uint)high;if(!seen.Add(key))continue;
                var other=network.Nodes[next];if(node.junction||other.junction||node.roundabout||other.roundabout)continue;
                Vector3 direction=other.position-node.position;direction.y=0f;if(direction.sqrMagnitude<.01f)continue;direction.Normalize();
                Vector3 right=Vector3.Cross(Vector3.up,direction);float roadWidth=Mathf.Min(node.width,other.width);
                float edge=Mathf.Max(.8f,roadWidth*.5f-.35f);
                Strip(node.position+right*edge,other.position+right*edge,.12f,whiteVertices,whiteTriangles);
                Strip(node.position-right*edge,other.position-right*edge,.12f,whiteVertices,whiteTriangles);
                bool passing=node.allowPassing&&other.allowPassing&&roadWidth>=13f;
                if(passing)Dashed(node.position,other.position,.14f,yellowVertices,yellowTriangles);
                else
                {
                    Strip(node.position+right*.13f,other.position+right*.13f,.13f,yellowVertices,yellowTriangles);
                    Strip(node.position-right*.13f,other.position-right*.13f,.13f,yellowVertices,yellowTriangles);
                }
                if(roadWidth>=13f)
                {
                    Dashed(node.position+right*3.1f,other.position+right*3.1f,.1f,whiteVertices,whiteTriangles);
                    Dashed(node.position-right*3.1f,other.position-right*3.1f,.1f,whiteVertices,whiteTriangles);
                }
            }
        }
        foreach(var junction in Object.FindObjectsByType<CityIntersection>())
        {
            if(junction.roundabout)continue;
            foreach(var forward in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
            {
                float roadWidth=ApproachWidth(network,junction.transform.position,forward);
                Vector3 left=-Vector3.Cross(Vector3.up,forward);
                float laneOffset=roadWidth>=13f?4.65f:Mathf.Min(roadWidth*.22f,1.55f);
                float stopDistance=junction.radius+1f;
                if(junction.signalised||junction.stopControlled)
                {
                    int laneCount=roadWidth>=13f?2:1;
                    for(int lane=0;lane<laneCount;lane++)
                    {
                        Vector3 stop=junction.transform.position-forward*stopDistance+left*(laneOffset-lane*3.1f);
                        Strip(stop-left*1.5f,stop+left*1.5f,.28f,whiteVertices,whiteTriangles);
                    }
                }
                if(junction.signalised)
                {
                    for(int stripe=0;stripe<5;stripe++)
                    {
                        Vector3 crossing=junction.transform.position-forward*(stopDistance+1.5f+stripe*.58f);
                        Strip(crossing-left*(roadWidth*.5f-.25f),crossing+left*(roadWidth*.5f-.25f),.28f,whiteVertices,whiteTriangles);
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
        var yellowMesh=SaveMarkingMesh(yellowVertices,yellowTriangles,"City yellow road markings",Generated+"/CityMarkingsYellow.asset");
        var whiteObject=new GameObject("Shared white marking mesh",typeof(MeshFilter),typeof(MeshRenderer));whiteObject.transform.SetParent(group,false);
        whiteObject.GetComponent<MeshFilter>().sharedMesh=whiteMesh;whiteObject.GetComponent<MeshRenderer>().sharedMaterial=white;
        var yellowObject=new GameObject("Shared yellow marking mesh",typeof(MeshFilter),typeof(MeshRenderer));yellowObject.transform.SetParent(group,false);
        yellowObject.GetComponent<MeshFilter>().sharedMesh=yellowMesh;yellowObject.GetComponent<MeshRenderer>().sharedMaterial=yellow;
    }
    static Mesh SaveMarkingMesh(List<Vector3> vertices,List<int> triangles,string name,string path)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(asset==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,asset);Object.DestroyImmediate(mesh);return asset;
    }
}
