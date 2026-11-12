using System.Collections;
using System.Collections.Generic;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using UnityEngine;
namespace KombiShift2.City
{
    public sealed class CityTrafficFleet : MonoBehaviour
    {
        public float fullPhysicsDistance=350f,spawnDistance=450f;
        readonly List<CityTrafficAgent> pool=new();CityRoadNetwork graph;Transform player;float next;int spawnCursor;
        public int ActiveCount {get;private set;}
        public float MeanDecisionMilliseconds {get;private set;}
        IEnumerator Start()
        {
            graph=GetComponent<CityRoadNetwork>();
            var director=FindFirstObjectByType<KombiShiftGameDirector>();
            if(graph==null || director==null || director.Vehicle==null){Debug.LogError("City traffic requires a road network and player.",this);yield break;}
            graph.Load();if(graph.Nodes.Length==0)yield break;
            var legacy=FindObjectsByType<KombiTrafficVehicle>();
            if(legacy.Length==0)yield break;
            var source=legacy[0].gameObject;
            foreach(var car in legacy){car.enabled=false;car.gameObject.SetActive(false);}
            player=director.Vehicle.transform;
            yield return null;
            int count=new KombiShiftConfiguration(KombiChallenge.Selected).CityTrafficVehicles;
            for(int i=0;i<count;i++)
            {
                var agent=CreateVehicle(source,i);pool.Add(agent);agent.gameObject.SetActive(false);
                if(i%4==0)yield return null;
            }
        }
        CityTrafficAgent CreateVehicle(GameObject source,int number)
        {
            var go=new GameObject("City traffic "+number);go.transform.SetParent(transform,false);
            var visual=Instantiate(source,go.transform);visual.name="Vehicle art";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.SetActive(true);
            foreach(var script in visual.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
            foreach(var collider in visual.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var oldBody in visual.GetComponentsInChildren<Rigidbody>()){oldBody.isKinematic=true;Destroy(oldBody);}
            var renderers=visual.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            visual.transform.localPosition+=Vector3.up*(.12f-bounds.min.y);
            bounds.center+=Vector3.up*(.12f-bounds.min.y);
            // One simple physical chassis and four suspension contacts; imported art stays intact.
            var box=go.AddComponent<BoxCollider>();box.center=go.transform.InverseTransformPoint(bounds.center)+Vector3.up*.18f;box.size=new Vector3(Mathf.Clamp(bounds.size.x*.85f,1.4f,2.3f),Mathf.Clamp(bounds.size.y*.65f,.8f,1.5f),Mathf.Clamp(bounds.size.z*.9f,3f,5.2f));
            var body=go.AddComponent<Rigidbody>();body.mass=1500;body.centerOfMass=new Vector3(0,.35f,0);body.angularDamping=2;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            float halfWidth=box.size.x*.4f,halfLength=box.size.z*.32f;
            for(int i=0;i<4;i++)
            {
                var wheel=new GameObject("Suspension "+i);wheel.transform.SetParent(go.transform,false);wheel.transform.localPosition=new Vector3(i%2==0?-halfWidth:halfWidth,.4f,i<2?halfLength:-halfLength);
                var contact=wheel.AddComponent<WheelCollider>();contact.radius=.31f;contact.suspensionDistance=.22f;contact.mass=25;
                var spring=contact.suspensionSpring;spring.spring=35000;spring.damper=4500;spring.targetPosition=.5f;contact.suspensionSpring=spring;
                var sideways=contact.sidewaysFriction;sideways.stiffness=1.4f;contact.sidewaysFriction=sideways;
            }
            var agent=go.AddComponent<CityTrafficAgent>();agent.style=(CityDriverStyle)(number%4);agent.SetSchedule(number);
            var tint=new MaterialPropertyBlock();tint.SetColor("_Color",Color.HSVToRGB((number*.173f)%1f,.35f,.85f));
            foreach(var renderer in renderers)for(int m=0;m<renderer.sharedMaterials.Length;m++)if(renderer.sharedMaterials[m]!=null&&renderer.sharedMaterials[m].name.ToLowerInvariant().Contains("body"))renderer.SetPropertyBlock(tint,m);
            return agent;
        }
        void Update()
        {
            if(graph==null||player==null||Time.timeScale==0f)return;
            // Reuse at most one pool slot per frame. Searches share the graph's per-frame budget
            // with vehicle replans and never run twice for the same successful spawn.
            if(pool.Count>0)
            {
                var candidate=pool[spawnCursor++%pool.Count];
                if(!candidate.gameObject.activeSelf)TrySpawn(candidate);
            }
            if(Time.time<next)return;next=Time.time+.5f;
            int active=0;float cost=0;
            foreach(var agent in pool)
            {
                if(!agent.gameObject.activeSelf)continue;
                float distance=(agent.transform.position-player.position).sqrMagnitude;
                float despawn=spawnDistance+200f;
                if(distance>despawn*despawn && !Visible(agent.transform.position))
                {agent.gameObject.SetActive(false);continue;}
                bool detailed=distance<fullPhysicsDistance*fullPhysicsDistance;
                // A visible moving vehicle should not freeze at the simulation boundary.
                agent.SetDetailedPhysics(detailed||Visible(agent.transform.position));
                active++;cost+=agent.DecisionMilliseconds;
            }
            ActiveCount=active;MeanDecisionMilliseconds=active>0?cost/active:0;
        }
        static bool Visible(Vector3 point)
        {
            var camera=Camera.main;if(camera==null)return false;
            var viewport=camera.WorldToViewportPoint(point);
            return viewport.z>0&&viewport.x>-.1f&&viewport.x<1.1f&&viewport.y>-.1f&&viewport.y<1.1f;
        }
        void TrySpawn(CityTrafficAgent agent)
        {
            for(int attempt=0;attempt<40;attempt++)
            {
                int start=Random.Range(0,graph.Nodes.Length);var node=graph.Nodes[start];if(node.component!=0||node.junction||node.links.Length<2)continue;
                float distance=Vector3.Distance(node.position,player.position);if(distance<80||distance>spawnDistance)continue;
                int end=Random.Range(0,graph.Nodes.Length);if(graph.Nodes[end].component!=0)continue;
                if(!graph.TryBeginTrafficPathSearch())return;
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var path=graph.FindPath(start,end);
                graph.RecordTrafficPathSearch((float)timer.Elapsed.TotalMilliseconds);
                if(path.Count<3)continue;
                Vector3 point=graph.LanePoint(path,0),forward=graph.LanePoint(path,1)-point;forward.y=0;
                if(Physics.CheckBox(point+Vector3.up,.95f*Vector3.one,Quaternion.LookRotation(forward),~0,QueryTriggerInteraction.Ignore))continue;
                if(Visible(point))continue;
                agent.transform.SetPositionAndRotation(point+Vector3.up*.12f,Quaternion.LookRotation(forward));
                var body=agent.Body;body.isKinematic=false;body.position=agent.transform.position;body.rotation=agent.transform.rotation;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
                agent.Initialise(graph,path);agent.gameObject.SetActive(true);return;
            }
        }
    }
}
