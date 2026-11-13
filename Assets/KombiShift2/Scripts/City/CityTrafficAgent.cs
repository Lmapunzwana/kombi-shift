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
            CityDriverStyle.Cautious=>new(){acceleration=1.5f,braking=5,headway=2.4f,speedFactor=1},
            CityDriverStyle.Impatient=>new(){acceleration=3.2f,braking=6,headway=1.1f,speedFactor=1,passing=true},
            CityDriverStyle.Aggressive=>new(){acceleration=4,braking=7,headway=.95f,speedFactor=1,passing=true},
            _=>new(){acceleration=2.4f,braking=6,headway=1.6f,speedFactor=1,passing=true}
        };
    }
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CityTrafficAgent : MonoBehaviour
    {
        public CityDriverStyle style;
        public float decisionInterval=.12f;
        public CityDriverProfile Profile=>CityDriverProfile.For(style);
        public int CurrentNode=>path.Count>0?path[Mathf.Min(index,path.Count-1)]:-1;
        public float DecisionMilliseconds {get;private set;}
        public bool Passing=>passing;
        public int Replans {get;private set;}
        CityRoadNetwork graph;Rigidbody body;WheelCollider[] wheels;List<int> path=new();int index;
        float nextPlan;
        public float speedCapKph;
        float nextDecision,desiredSpeed,blockedAt,passingUntil,offset,currentOffset,stoppedSince;bool passing,stopCompleted;
        CityIntersection lastJunction;
        readonly RaycastHit[] hits=new RaycastHit[24];readonly System.Diagnostics.Stopwatch watch=new();
        public void Initialise(CityRoadNetwork value,int start,int destination)
        {graph=value;body=GetComponent<Rigidbody>();wheels=GetComponentsInChildren<WheelCollider>();path=graph.FindPath(start,destination);index=Mathf.Min(1,path.Count-1);desiredSpeed=0;passing=false;currentOffset=offset=0;blockedAt=0;}
        void FixedUpdate()
        {
            if(graph==null||path.Count<2||body.isKinematic)return;
            var profile=Profile;int nodeIndex=path[index];var node=graph.Nodes[nodeIndex];
            Vector3 target=graph.LanePoint(path,index);Vector3 delta=target-body.position;delta.y=0;
            if(delta.magnitude<3.5f)
            {
                if(index<path.Count-1)index++;
                else {ChooseDestination();return;}
                target=graph.LanePoint(path,index);delta=target-body.position;delta.y=0;
            }
            Vector3 heading=delta.normalized;
            if(Time.time>=nextDecision)
            {
                watch.Restart();nextDecision=Time.time+decisionInterval;
                float legalSpeedKph=speedCapKph>0?Mathf.Min(node.speedLimit,speedCapKph):node.speedLimit;
                desiredSpeed=legalSpeedKph/3.6f;
                float angle=Mathf.Abs(Vector3.SignedAngle(transform.forward,heading,Vector3.up));desiredSpeed*=Mathf.Lerp(1,.2f,Mathf.InverseLerp(12,65,angle));
                float speed=body.linearVelocity.magnitude;
                Vector3 origin=body.position+transform.forward*1.7f+Vector3.up*.8f;
                float brakingDistance=speed*speed/(2f*Mathf.Max(.5f,profile.braking));
                float detectionDistance=Mathf.Max(25f,Mathf.Max(speed*profile.headway+8f,brakingDistance+8f));
                bool blocked=Obstacle(origin,transform.forward,detectionDistance,out var hit);
                if(blocked)
                {
                    if(blockedAt<=0)blockedAt=Time.time;
                    float leadSpeed=hit.rigidbody!=null?Mathf.Max(0,Vector3.Dot(hit.rigidbody.linearVelocity,heading)):0f;
                    float stoppingDistance=Mathf.Max(0,hit.distance-2.5f);
                    float safeSpeed=Mathf.Sqrt(leadSpeed*leadSpeed+2f*profile.braking*stoppingDistance);
                    desiredSpeed=Mathf.Min(desiredSpeed,safeSpeed);
                    bool nearJunction=false;foreach(var control in CityIntersection.Active)if(Vector3.Distance(origin,control.transform.position)<60f&&Vector3.Dot(transform.forward,control.transform.position-origin)>0){nearJunction=true;break;}
                    bool straight=!nearJunction&&angle<8 && index+3<path.Count && Vector3.Angle(heading,graph.Nodes[path[index+3]].position-node.position)<12;
                    // Passing uses a second lane in the same direction; never the oncoming lane.
                    if(!passing&&profile.passing&&node.allowPassing&&node.width>=13&&straight&&Time.time-blockedAt>2&&hit.rigidbody!=null&&hit.rigidbody.linearVelocity.magnitude<speed+1)
                    {
                        Vector3 passOrigin=origin+transform.right*3.1f;
                        if(!Obstacle(passOrigin,transform.forward,Mathf.Max(35,speed*4),out _)&&!Obstacle(passOrigin,-transform.forward,20,out _)&&CityIntersection.Near(target)==null)
                        {passing=true;passingUntil=Time.time+7;offset=-3.1f;}
                    }
                    if(Time.time-blockedAt>20){ChooseDestination(nodeIndex);blockedAt=Time.time;}
                }
                else blockedAt=0;
                if(passing)
                {
                    if(Time.time>=passingUntil||!node.allowPassing)
                    {if(!Obstacle(origin-transform.right*3.1f,transform.forward,12,out _)&&!Obstacle(origin-transform.right*3.1f,-transform.forward,12,out _)){passing=false;offset=0;}}
                else desiredSpeed=Mathf.Min(legalSpeedKph/3.6f,speed+profile.acceleration);
                }
                var junction=CityIntersection.Near(target);
                if(junction!=lastJunction){lastJunction=junction;stopCompleted=false;stoppedSince=0;}
                if(junction!=null&&Vector3.Distance(body.position,junction.transform.position)<junction.radius+8f)
                {
                    bool stopRequired=junction.RequiresStop(heading);
                    if(stopRequired&&!stopCompleted)
                    {
                        if(speed<.3f){if(stoppedSince==0)stoppedSince=Time.time;stopCompleted=Time.time-stoppedSince>.4f;}
                        if(!stopCompleted)desiredSpeed=0;
                    }
                    Vector3 exit=index+3<path.Count?graph.LanePoint(path,index+3):target;
                    if(stopRequired&&!stopCompleted || !junction.MayEnter(body,heading,exit))desiredSpeed=0;
                    else desiredSpeed=Mathf.Min(desiredSpeed,junction.roundabout?4:5);
                }
                watch.Stop();DecisionMilliseconds=(float)watch.Elapsed.TotalMilliseconds;
            }
            currentOffset=Mathf.MoveTowards(currentOffset,offset,Time.fixedDeltaTime*1.2f);
            target+=-Vector3.Cross(Vector3.up,heading)*currentOffset;delta=target-body.position;delta.y=0;
            float steering=Vector3.SignedAngle(transform.forward,delta,Vector3.up);
            float error=desiredSpeed-body.linearVelocity.magnitude;
            for(int i=0;i<wheels.Length;i++)
            {
                wheels[i].steerAngle=i<2?Mathf.Clamp(steering,-34,34):0;
                wheels[i].motorTorque=error>.05f?Mathf.Min(error,profile.acceleration)*body.mass*wheels[i].radius/4:0;
                wheels[i].brakeTorque=desiredSpeed<.1f||error<-.25f?Mathf.Min(profile.braking,Mathf.Max(.8f,-error))*body.mass*wheels[i].radius/4:0;
            }
        }
        bool Obstacle(Vector3 origin,Vector3 direction,float distance,out RaycastHit nearest)
        {
            nearest=default;float closest=float.MaxValue;
            int count=Physics.SphereCastNonAlloc(origin,.9f,direction,hits,distance,~0,QueryTriggerInteraction.Ignore);
            var results=count==hits.Length?Physics.SphereCastAll(origin,.9f,direction,distance,~0,QueryTriggerInteraction.Ignore):hits;
            if(results!=hits)count=results.Length;
            for(int i=0;i<count;i++)
            {var hit=results[i];if(hit.transform.IsChildOf(transform)||hit.normal.y>.6f)continue;if(hit.distance<closest){closest=hit.distance;nearest=hit;}}
            return closest<float.MaxValue;
        }
        void ChooseDestination(int blocked=-1)
        {
            if(Time.time<nextPlan)return;nextPlan=Time.time+.5f;
            int start=graph.Nearest(body.position,0);if(start<0)return;
            for(int attempt=0;attempt<12;attempt++)
            {
                int end=UnityEngine.Random.Range(0,graph.Nodes.Length);if(graph.Nodes[end].component!=0||graph.Nodes[end].links.Length<2||Vector3.Distance(body.position,graph.Nodes[end].position)<120)continue;
                var candidate=graph.FindDirectedPath(start,end,transform.forward,-1,blocked);if(candidate.Count<2)continue;
                Vector3 initial=graph.Nodes[candidate[Mathf.Min(2,candidate.Count-1)]].position-body.position;initial.y=0;
                path=candidate;index=1;Replans++;return;
            }
            desiredSpeed=0;
        }
    }
}
