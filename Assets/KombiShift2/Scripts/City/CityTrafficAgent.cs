using System;
using System.Collections.Generic;
using UnityEngine;
namespace KombiShift2.City
{
    public enum CityDriverStyle { Cautious, Normal, Impatient, Aggressive }
    [Serializable] public struct CityDriverProfile
    {
        public float acceleration,braking,headway,speedFactor;
        public bool passing;
        public static CityDriverProfile For(CityDriverStyle style)=>style switch
        {
            CityDriverStyle.Cautious=>new(){acceleration=1.5f,braking=5,headway=2.4f,speedFactor=.85f},
            CityDriverStyle.Impatient=>new(){acceleration=3.2f,braking=6,headway=1.1f,speedFactor=1.05f,passing=true},
            CityDriverStyle.Aggressive=>new(){acceleration=4,braking=7,headway=.95f,speedFactor=1.1f,passing=true},
            _=>new(){acceleration=2.4f,braking=6,headway=1.6f,speedFactor=1,passing=true}
        };
    }
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CityTrafficAgent : MonoBehaviour
    {
        public CityDriverStyle style;
        public float decisionInterval=.12f;
        public bool allowOvertaking; // Safe default: stay in lane and wait for clearance.
        public CityDriverProfile Profile=>CityDriverProfile.For(style);
        public int CurrentNode=>path.Count>0?path[Mathf.Min(index,path.Count-1)]:-1;
        public float DecisionMilliseconds {get;private set;}
        public bool Passing=>passing;
        public int Replans {get;private set;}
        CityRoadNetwork graph;Rigidbody body;BoxCollider chassis;WheelCollider[] wheels;List<int> path=new();int index;
        float nextPlan;
        public float speedCapKph;
        float nextDecision,desiredSpeed,blockedAt,passingUntil,offset,currentOffset,stoppedSince;bool passing,stopCompleted;
        CityIntersection lastJunction;
        RaycastHit[] hits=new RaycastHit[24];readonly System.Diagnostics.Stopwatch watch=new();
        public int SaturatedQueries {get;private set;}
        public Rigidbody Body {get { CachePhysics(); return body; }}
        float decisionPhase;
        void CachePhysics()
        {if(body==null)body=GetComponent<Rigidbody>();if(chassis==null)chassis=GetComponent<BoxCollider>();if(wheels==null)wheels=GetComponentsInChildren<WheelCollider>();}
        public void SetSchedule(int slot) => decisionPhase=Mathf.Repeat(slot*.61803399f,1f);
        public void Initialise(CityRoadNetwork value,int start,int destination)
        {Initialise(value,value.FindPath(start,destination));}
        public void Initialise(CityRoadNetwork value,IReadOnlyList<int> preparedPath)
        {
            ResetForPool();graph=value;path=new List<int>(preparedPath);index=path.Count>1?1:0;
            nextDecision=Time.time+decisionPhase*Mathf.Max(.02f,decisionInterval);
        }
        public void ResetForPool()
        {
            CachePhysics();CityIntersection.ReleaseAll(body);
            path.Clear();graph=null;index=0;lastJunction=null;
            nextPlan=nextDecision=desiredSpeed=blockedAt=passingUntil=offset=currentOffset=stoppedSince=0;
            passing=stopCompleted=false;DecisionMilliseconds=0;Replans=SaturatedQueries=0;
            watch.Reset();
            foreach(var wheel in wheels){wheel.motorTorque=0;wheel.brakeTorque=4000;wheel.steerAngle=0;}
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        }
        public void SetDetailedPhysics(bool detailed)
        {
            CachePhysics();
            if(body.isKinematic==!detailed)return;
            if(!detailed)
            {
                CityIntersection.ReleaseAll(body);lastJunction=null;stopCompleted=false;stoppedSince=0;
                body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
                body.collisionDetectionMode=CollisionDetectionMode.Discrete;
                body.isKinematic=true;
            }
            else
            {
                body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                desiredSpeed=0;nextDecision=Time.time+decisionPhase*Mathf.Max(.02f,decisionInterval);body.WakeUp();
            }
        }
        void OnDisable(){ResetForPool();}
        void FixedUpdate()
        {
            if(graph==null||path.Count<2||body.isKinematic)return;
            var profile=Profile;int nodeIndex=path[index];var node=graph.Nodes[nodeIndex];
            Vector3 target=graph.LanePoint(path,index);Vector3 delta=target-body.position;delta.y=0;
            if(delta.magnitude<3.5f)
            {
                if(index<path.Count-1)index++;
                else {ChooseDestination();return;}
                nodeIndex=path[index];node=graph.Nodes[nodeIndex];
                target=graph.LanePoint(path,index);delta=target-body.position;delta.y=0;
            }
            Vector3 heading=delta.normalized;
            if(Time.time>=nextDecision)
            {
                watch.Restart();nextDecision=Time.time+Mathf.Max(.02f,decisionInterval);
                desiredSpeed=Mathf.Min(20f,node.speedLimit)/3.6f*Mathf.Min(1f,profile.speedFactor);
                if(speedCapKph>0)desiredSpeed=Mathf.Min(desiredSpeed,speedCapKph/3.6f);
                float angle=Mathf.Abs(Vector3.SignedAngle(transform.forward,heading,Vector3.up));desiredSpeed*=Mathf.Lerp(1,.2f,Mathf.InverseLerp(12,65,angle));
                float speed=body.linearVelocity.magnitude;
                Vector3 origin=body.position+transform.forward*1.7f+Vector3.up*1.15f;
                float headway=Mathf.Max(2.4f,profile.headway);
                float stoppingDistance=speed*speed/(2f*profile.braking)+speed*(headway+decisionInterval)+4f;
                bool blocked=Obstacle(origin,transform.forward,Mathf.Max(12f,stoppingDistance),out var hit);
                if(blocked)
                {
                    if(blockedAt<=0)blockedAt=Time.time;
                    desiredSpeed=Mathf.Min(desiredSpeed,Mathf.Max(0,(hit.distance-3f)/headway));
                    bool nearJunction=CityIntersection.Ahead(origin,transform.forward,60f);
                    bool straight=!nearJunction&&angle<8 && index+3<path.Count && Vector3.Angle(heading,graph.Nodes[path[index+3]].position-node.position)<12;
                    // Passing uses a second lane in the same direction; never the oncoming lane.
                    if(!passing&&allowOvertaking&&profile.passing&&node.allowPassing&&node.width>=13&&straight&&Time.time-blockedAt>2&&hit.rigidbody!=null&&hit.rigidbody.linearVelocity.magnitude<speed+1)
                    {
                        Vector3 passOrigin=origin+transform.right*3.1f;
                        if(!Obstacle(passOrigin,transform.forward,Mathf.Max(35,speed*4),out _)&&!Obstacle(passOrigin,-transform.forward,20,out _)&&CityIntersection.Near(target)==null)
                        {passing=true;passingUntil=Time.time+7;offset=-3.1f;}
                    }
                    if(Time.time-blockedAt>20 && ChooseDestination(nodeIndex))return;
                }
                else blockedAt=0;
                if(passing)
                {
                    if(Time.time>=passingUntil||!node.allowPassing)
                    {if(!Obstacle(origin-transform.right*3.1f,transform.forward,12,out _)&&!Obstacle(origin-transform.right*3.1f,-transform.forward,12,out _)){passing=false;offset=0;}}
                    else desiredSpeed=Mathf.Min(desiredSpeed,speed+profile.acceleration);
                }
                var junction=CityIntersection.Near(target);
                if(junction!=lastJunction){lastJunction=junction;stopCompleted=false;stoppedSince=0;}
                if(junction!=null&&Vector3.Distance(body.position,junction.transform.position)<junction.radius+8f)
                {
                    if(junction.stopControlled&&!stopCompleted)
                    {
                        if(speed<.3f){if(stoppedSince==0)stoppedSince=Time.time;stopCompleted=Time.time-stoppedSince>.6f;}
                        else stoppedSince=0;
                        if(!stopCompleted)desiredSpeed=0;
                    }
                    Vector3 exit=index+3<path.Count?graph.LanePoint(path,index+3):target;
                    if(junction.stopControlled&&!stopCompleted || !junction.MayEnter(body,heading,exit))desiredSpeed=0;
                    else desiredSpeed=Mathf.Min(desiredSpeed,junction.roundabout?2.5f:3f);
                }
                watch.Stop();DecisionMilliseconds=(float)watch.Elapsed.TotalMilliseconds;
            }
            currentOffset=Mathf.MoveTowards(currentOffset,offset,Time.fixedDeltaTime*1.2f);
            target+=-Vector3.Cross(Vector3.up,heading)*currentOffset;delta=target-body.position;delta.y=0;
            float steering=Vector3.SignedAngle(transform.forward,delta,Vector3.up);
            // The emergency check runs every physics tick, independent of staggered AI decisions.
            float actualSpeed=body.linearVelocity.magnitude;
            Vector3 motion=body.linearVelocity;motion.y=0;
            Vector3 safetyDirection=motion.sqrMagnitude>.01f?motion.normalized:transform.forward;
            Vector3 safetyOrigin=transform.TransformPoint(chassis.center);
            int nearby=CityPhysicsQueries.BoxCast(safetyOrigin,chassis.size*.5f+new Vector3(.12f,0,.12f),safetyDirection,transform.rotation,
                1.2f+actualSpeed*(decisionInterval+.2f),ref hits,out var sweep);
            float gap=float.PositiveInfinity;
            for(int h=0;h<nearby;h++)
            {
                var contact=sweep[h];
                if(contact.transform.IsChildOf(transform)||contact.normal.y>.6f||contact.collider.GetComponentInParent<CityRoadSurface>()!=null)continue;
                gap=Mathf.Min(gap,contact.distance);
            }
            bool emergency=!float.IsPositiveInfinity(gap);
            if(emergency)
            {
                desiredSpeed=0f;
                // Last-resort contact prevention also covers corner/side sweeps that a centre ray misses.
                if(gap<.4f+actualSpeed*Time.fixedDeltaTime*2f)
                {body.linearVelocity=new Vector3(0,body.linearVelocity.y,0);actualSpeed=0f;}
            }
            float error=desiredSpeed-actualSpeed;
            for(int i=0;i<wheels.Length;i++)
            {
                wheels[i].steerAngle=i<2?Mathf.Clamp(steering,-34,34):0;
                wheels[i].motorTorque=error>.05f?Mathf.Min(error,profile.acceleration)*body.mass*wheels[i].radius/4:0;
                // Holding a stop must use full braking; tapering to 0.8 let cars creep into queues.
                float braking=desiredSpeed<.1f?Mathf.Max(8f,profile.braking):error<-.25f?Mathf.Min(profile.braking,-error):0f;
                wheels[i].brakeTorque=braking*body.mass*wheels[i].radius/4f;
            }
        }
        bool Obstacle(Vector3 origin,Vector3 direction,float distance,out RaycastHit nearest)
        {
            nearest=default;float closest=float.MaxValue;
            int count=CityPhysicsQueries.SphereCast(origin,.9f,direction,distance,ref hits,out var results,out bool saturated);
            if(saturated)SaturatedQueries++;
            for(int i=0;i<count;i++)
            {var hit=results[i];if(hit.transform.IsChildOf(transform)||hit.normal.y>.6f||hit.collider.GetComponentInParent<CityRoadSurface>()!=null)continue;if(hit.distance<closest){closest=hit.distance;nearest=hit;}}
            return closest<float.MaxValue;
        }
        bool ChooseDestination(int blocked=-1)
        {
            desiredSpeed=0;
            if(Time.time<nextPlan)return false;
            int start=graph.Nearest(body.position,0);if(start<0)return false;
            for(int attempt=0;attempt<12;attempt++)
            {
                int end=UnityEngine.Random.Range(0,graph.Nodes.Length);
                if(graph.Nodes[end].component!=0||graph.Nodes[end].links.Length<2||(body.position-graph.Nodes[end].position).sqrMagnitude<14400)continue;
                if(!graph.TryBeginTrafficPathSearch())return false;
                nextPlan=Time.time+.5f;
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var candidate=graph.FindDirectedPath(start,end,transform.forward,-1,blocked);
                graph.RecordTrafficPathSearch((float)timer.Elapsed.TotalMilliseconds);
                if(candidate.Count<2)return false;
                CityIntersection.ReleaseAll(body);lastJunction=null;stopCompleted=false;stoppedSince=0;
                path=candidate;index=1;Replans++;blockedAt=0;passing=false;offset=0;passingUntil=0;return true;
            }
            nextPlan=Time.time+.5f;return false;
        }
    }
}
