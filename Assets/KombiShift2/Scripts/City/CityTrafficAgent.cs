using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KombiShift2.City
{
    public enum CityDriverStyle { Cautious, Normal, Impatient, Aggressive }
    [Serializable] public struct CityDriverProfile
    {
        public float acceleration,braking,headway,speedFactor,minimumGap,gapAcceptance;
        public bool passing;
        public static CityDriverProfile For(CityDriverStyle style)=>style switch
        {
            CityDriverStyle.Cautious=>new(){acceleration=1.5f,braking=5,headway=2.4f,speedFactor=.85f,minimumGap=3f,gapAcceptance=4f},
            CityDriverStyle.Impatient=>new(){acceleration=3.2f,braking=6,headway=1.1f,speedFactor=1.05f,minimumGap=2f,gapAcceptance=2.7f,passing=true},
            CityDriverStyle.Aggressive=>new(){acceleration=4,braking=7,headway=.95f,speedFactor=1.1f,minimumGap=1.8f,gapAcceptance=2.4f,passing=true},
            _=>new(){acceleration=2.4f,braking=6,headway=1.6f,speedFactor=1,minimumGap=2.5f,gapAcceptance=3.2f,passing=true}
        };
    }
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CityTrafficAgent : MonoBehaviour
    {
        struct LaneSample { public Vector3 point;public int node;public LaneSample(Vector3 p,int n){point=p;node=n;} }
        public CityDriverStyle style; public float decisionInterval=.12f,speedCapKph;
        public CityDriverProfile Profile=>CityDriverProfile.For(style);
        public int CurrentNode=>lane.Count>0?lane[Mathf.Min(laneIndex,lane.Count-1)].node:-1;
        public float DecisionMilliseconds {get;private set;} public bool Passing=>passing; public int Replans {get;private set;}
        public float JunctionWaitSeconds=>junctionWaitStarted>0?Time.time-junctionWaitStarted:0;
        public float StalledSeconds=>Time.time-lastProgressTime;
        public bool AtJunction=>lastJunction!=null;
        public int RedLightEntries {get;private set;} public int StopSignViolations {get;private set;} public int JunctionEntries {get;private set;}
        public string DenialReason {get;private set;} public string JunctionId=>lastJunction!=null?lastJunction.junctionId:"";
        CityRoadNetwork graph;Rigidbody body,playerBody;WheelCollider[] wheels;List<int> path=new();readonly List<LaneSample> lane=new();int laneIndex;
        CityRoadSurface[] surfaces=Array.Empty<CityRoadSurface>();float nextPlan,nextDecision,desiredSpeed,blockedAt,passingUntil,offset,currentOffset,stoppedSince,junctionWaitStarted,kinematicSpeed,lastProgressTime;
        bool passing,stopCompleted,insideJunction,junctionAuthorized;Vector3 junctionApproachHeading;CityIntersection lastJunction;readonly RaycastHit[] hits=new RaycastHit[24];readonly Collider[] exitHits=new Collider[24];readonly System.Diagnostics.Stopwatch watch=new();

        public void Initialise(CityRoadNetwork value,int start,int destination,List<int> directedPath=null)
        {graph=value;body=GetComponent<Rigidbody>();var director=FindFirstObjectByType<KombiShift2.Gameplay.KombiShiftGameDirector>();playerBody=director!=null&&director.Vehicle!=null?director.Vehicle.GetComponent<Rigidbody>():null;wheels=GetComponentsInChildren<WheelCollider>();surfaces=FindObjectsByType<CityRoadSurface>(FindObjectsSortMode.None);path=directedPath??graph.FindDirectedPath(start,destination,transform.forward);BuildLane();desiredSpeed=0;kinematicSpeed=0;passing=false;currentOffset=offset=0;blockedAt=0;lastJunction=null;lastProgressTime=Time.time;}
        void BuildLane()
        {
            lane.Clear();laneIndex=0;if(path.Count<2)return;
            var basePoints=new Vector3[path.Count];for(int i=0;i<path.Count;i++)basePoints[i]=graph.LanePoint(path,i);
            lane.Add(new LaneSample(basePoints[0],path[0]));
            for(int i=1;i<path.Count-1;i++)
            {
                bool connector=graph.Nodes[path[i]].junction||CityIntersection.Near(basePoints[i])!=null;
                if(!connector){lane.Add(new LaneSample(basePoints[i],path[i]));continue;}
                Vector3 a=Vector3.Lerp(basePoints[i-1],basePoints[i],.55f),b=basePoints[i],c=Vector3.Lerp(basePoints[i],basePoints[i+1],.45f);
                for(int sample=0;sample<=5;sample++){float t=sample/5f;lane.Add(new LaneSample(Vector3.Lerp(Vector3.Lerp(a,b,t),Vector3.Lerp(b,c,t),t),path[i]));}
            }
            lane.Add(new LaneSample(basePoints[^1],path[^1]));laneIndex=Mathf.Min(1,lane.Count-1);
        }
        void FixedUpdate()
        {
            if(graph==null||lane.Count<2)return;var profile=Profile;var sample=lane[Mathf.Min(laneIndex,lane.Count-1)];Vector3 target=sample.point;Vector3 delta=target-body.position;delta.y=0;
            if(delta.magnitude<2.2f||(laneIndex<lane.Count-1&&Vector3.Dot(transform.forward,delta)<-.35f)){if(laneIndex<lane.Count-1){laneIndex++;lastProgressTime=Time.time;}else{ChooseDestination();return;}sample=lane[laneIndex];target=sample.point;delta=target-body.position;delta.y=0;}
            Vector3 heading=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;
            if(Time.time>=nextDecision)Decide(profile,sample.node,target,heading);
            currentOffset=Mathf.MoveTowards(currentOffset,offset,Time.fixedDeltaTime*1.2f);target+=-Vector3.Cross(Vector3.up,heading)*currentOffset;delta=target-body.position;delta.y=0;
            // Recovery must also run while distant cars use kinematic movement.
            // Previously the kinematic return skipped both stuck and gridlock recovery.
            flippedFor=CityRoadRecovery.IsFlipped(transform)?flippedFor+Time.fixedDeltaTime:0f;
            float movementSpeed=body.isKinematic?kinematicSpeed:body.linearVelocity.magnitude;
            stuckFor=desiredSpeed>.5f&&movementSpeed<.3f?stuckFor+Time.fixedDeltaTime:0f;
            // Gridlock breaker: under 5 m of movement in 40 s -> leave the road for fleet respawn.
            if(jamSince==0f||(body.position-jamAnchor).sqrMagnitude>25f){jamAnchor=body.position;jamSince=Time.time;}
            if(flippedFor>2f||stuckFor>8f){RecoverOnLane();return;}
            if(Time.time-jamSince>40f){jamSince=0f;gameObject.SetActive(false);return;}
            if(body.isKinematic){KinematicMove(delta,profile);return;}
            float steering=Vector3.SignedAngle(transform.forward,delta,Vector3.up),error=desiredSpeed-body.linearVelocity.magnitude;
            for(int i=0;i<wheels.Length;i++){wheels[i].steerAngle=i<2?Mathf.Clamp(steering,-34,34):0;wheels[i].motorTorque=error>.05f?Mathf.Min(error,profile.acceleration)*body.mass*wheels[i].radius/4:0;wheels[i].brakeTorque=desiredSpeed<.1f||error<-.25f?Mathf.Min(profile.braking,Mathf.Max(.8f,-error))*body.mass*wheels[i].radius/4:0;}
        }
        float flippedFor,stuckFor,jamSince;Vector3 jamAnchor;
        void RecoverOnLane()
        {
            flippedFor=stuckFor=0f;laneIndex=Mathf.Min(laneIndex+2,lane.Count-1);lastProgressTime=Time.time;
            Vector3 point=lane[laneIndex].point,next=lane[Mathf.Min(laneIndex+1,lane.Count-1)].point,forward=next-point;forward.y=0;
            if(forward.sqrMagnitude<.01f)forward=transform.forward;forward.y=0;
            if(body.isKinematic)kinematicSpeed=0f;else{body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            body.position=point+Vector3.up*.35f;body.rotation=Quaternion.LookRotation(forward.normalized,Vector3.up);
            transform.SetPositionAndRotation(body.position,body.rotation);
        }
        void Decide(CityDriverProfile profile,int nodeIndex,Vector3 target,Vector3 heading)
        {
            watch.Restart();nextDecision=Time.time+decisionInterval;var node=graph.Nodes[nodeIndex];float speed=body.isKinematic?kinematicSpeed:body.linearVelocity.magnitude;
            float targetSpeed=node.speedLimit/3.6f*profile.speedFactor;if(speedCapKph>0)targetSpeed=Mathf.Min(targetSpeed,speedCapKph/3.6f);
            float angle=Mathf.Abs(Vector3.SignedAngle(transform.forward,heading,Vector3.up));targetSpeed*=Mathf.Lerp(1,.35f,Mathf.InverseLerp(15,70,angle));
            Vector3 origin=body.position+transform.forward*1.7f+Vector3.up*.8f;
            if(Leader(origin,transform.forward,Mathf.Max(12,speed*profile.headway+8),out var leader))
            {
                if(blockedAt<=0)blockedAt=Time.time;float leaderSpeed=leader.rigidbody!=null?leader.rigidbody.linearVelocity.magnitude:0;float gap=Mathf.Max(.2f,leader.distance-2f),dv=speed-leaderSpeed;
                float desiredGap=profile.minimumGap+3f+speed*profile.headway+speed*dv/(2*Mathf.Sqrt(profile.acceleration*profile.braking));
                float availableGap=Mathf.Max(0f,gap-profile.minimumGap-3f);
                float safeFollowingSpeed=Mathf.Sqrt(Mathf.Max(0f,leaderSpeed*leaderSpeed+2f*profile.braking*availableGap));
                targetSpeed=Mathf.Min(targetSpeed,safeFollowingSpeed);
                float acceleration=profile.acceleration*(1-Mathf.Pow(speed/Mathf.Max(.5f,targetSpeed),4)-Mathf.Pow(desiredGap/gap,2));
                targetSpeed=Mathf.Clamp(speed+acceleration*decisionInterval,0,targetSpeed);
            } else blockedAt=0;
            HandlePassing(profile,node,origin,heading,angle,speed,ref targetSpeed);
            SlowForPothole(heading,node.speedLimit,ref targetSpeed);
            var junction=lastJunction;
            if(junction!=null)
            {
                Vector3 toJunction=junction.transform.position-body.position;toJunction.y=0;
                if(toJunction.magnitude>junction.radius+15f&&Vector3.Dot(transform.forward,toJunction)<0f)
                {lastJunction=null;junction=null;junctionWaitStarted=0;stopCompleted=false;insideJunction=false;junctionAuthorized=false;}
            }
            if(junction==null)junction=UpcomingJunction();
            if(junction!=lastJunction)
            {
                lastJunction=junction;stopCompleted=false;stoppedSince=0;junctionWaitStarted=0;insideJunction=false;junctionAuthorized=false;
                junctionApproachHeading=junction!=null?junction.transform.position-body.position:transform.forward;junctionApproachHeading.y=0;if(junctionApproachHeading.sqrMagnitude>.01f)junctionApproachHeading.Normalize();
            }
            if(junction!=null)
            {
                float junctionDistance=Vector3.Distance(body.position,junction.transform.position);
                if(junctionDistance<junction.radius+35f)HandleJunction(junction,junctionApproachHeading,speed,profile,ref targetSpeed);
                bool nowInside=junctionDistance<junction.radius*.7f;
                if(nowInside&&!insideJunction&&!junctionAuthorized&&(junction.signalised||junction.RequiresStop(junctionApproachHeading)))
                {
                    Vector3 approach=body.position-junction.transform.position;approach.y=0;if(approach.sqrMagnitude<.01f)approach=-junctionApproachHeading;
                    Vector3 held=junction.transform.position+approach.normalized*junction.radius*.85f;held.y=body.position.y;body.position=held;
                    if(body.isKinematic)kinematicSpeed=0;else{body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}targetSpeed=0;nowInside=false;
                }
                if(nowInside&&!insideJunction)JunctionEntries++;insideJunction=nowInside;
            }
            desiredSpeed=targetSpeed;watch.Stop();DecisionMilliseconds=(float)watch.Elapsed.TotalMilliseconds;
            if(junction==null&&((blockedAt>0&&Time.time-blockedAt>20f)||StalledSeconds>20f)){ChooseDestination(nodeIndex);blockedAt=Time.time;lastProgressTime=Time.time;}
        }
        CityIntersection UpcomingJunction()
        {
            for(int i=laneIndex;i<Mathf.Min(lane.Count,laneIndex+40);i++)
            {
                int nodeId=lane[i].node;var node=graph.Nodes[nodeId];
                var exact=(node.junction||node.roundabout)?CityIntersection.Active.FirstOrDefault(x=>x.nodeIds!=null&&Array.IndexOf(x.nodeIds,nodeId)>=0):null;
                if(exact!=null)return exact;
                var crossed=CityIntersection.Active.FirstOrDefault(x=>Vector3.Distance(lane[i].point,x.transform.position)<=x.radius+2f);if(crossed!=null)return crossed;
            }
            return null;
        }
        void HandleJunction(CityIntersection junction,Vector3 heading,float speed,CityDriverProfile profile,ref float targetSpeed)
        {
            if(junctionAuthorized)
            {
                junction.MayEnter(body,heading,ExitPoint(junction));
                junctionWaitStarted=0;DenialReason="";return;
            }
            bool stopRequired=junction.RequiresStop(heading);float distance=Mathf.Max(0,Vector3.Distance(body.position,junction.transform.position)-junction.radius);
            if(junction.signalised&&junction.MustStop(heading))
            {
                float safeStopping=speed*speed/(2*Mathf.Max(1,profile.braking));if(distance>safeStopping+1f)targetSpeed=Mathf.Min(targetSpeed,Mathf.Sqrt(2*profile.braking*Mathf.Max(0,distance-1f)));
            }
            if(stopRequired&&!stopCompleted){if(speed<.3f){if(stoppedSince==0)stoppedSince=Time.time;stopCompleted=Time.time-stoppedSince>=.6f;}targetSpeed=0;}
            else if(junction.control=="GiveWay"||junction.control=="Priority")targetSpeed=Mathf.Min(targetSpeed,15f/3.6f);
            else if(junction.roundabout)targetSpeed=Mathf.Min(targetSpeed,18f/3.6f);
            else if(junction.control=="Merge")targetSpeed=Mathf.Min(targetSpeed,20f/3.6f);
            Vector3 exit=ExitPoint(junction);bool boxClear=ExitClear(exit);bool controllerAllows=junction.MayEnter(body,heading,exit);
            bool timeoutClear=JunctionWaitSeconds>6f;bool signalOkay=!junction.signalised||!junction.MustStop(heading),stopOkay=!stopRequired||stopCompleted;
            bool allowed=signalOkay&&stopOkay&&(boxClear||timeoutClear)&&controllerAllows;
            if(!allowed)
            {
                targetSpeed=0;if(junctionWaitStarted<=0&&distance<3f&&speed<.5f)junctionWaitStarted=Time.time;DenialReason=!signalOkay?"signal":!stopOkay?"stop":!boxClear&&!timeoutClear?"blocked-exit":!controllerAllows?"reservation":"unknown";
                float approachSpeed=body.isKinematic?kinematicSpeed:body.linearVelocity.magnitude;float clampDistance=1.5f+approachSpeed*approachSpeed/(2*Mathf.Max(1f,profile.braking))+approachSpeed*Time.fixedDeltaTime;
                if(distance<clampDistance)
                {if(body.isKinematic)kinematicSpeed=0;else body.linearVelocity=Vector3.zero;}
            }
            else {junctionWaitStarted=0;junctionAuthorized=true;DenialReason="";}
        }
        Vector3 ExitPoint(CityIntersection junction)
        {
            int end=Mathf.Min(lane.Count-1,laneIndex+36),closest=laneIndex;float distance=float.MaxValue;
            for(int i=laneIndex;i<=end;i++){float d=Vector3.Distance(lane[i].point,junction.transform.position);if(d<distance){distance=d;closest=i;}}
            for(int i=closest+1;i<=end;i++)if(Vector3.Distance(lane[i].point,junction.transform.position)>junction.radius+4f)return lane[i].point;
            return lane[end].point;
        }
        bool ExitClear(Vector3 exit)
        {
            int count=Physics.OverlapSphereNonAlloc(exit,2.2f,exitHits,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++){var rb=exitHits[i].attachedRigidbody;if(rb!=null&&rb!=body&&IsRoadUser(rb))return false;}return true;
        }
        void HandlePassing(CityDriverProfile profile,CityRoadNode node,Vector3 origin,Vector3 heading,float angle,float speed,ref float targetSpeed)
        {
            bool nearJunction=CityIntersection.Active.Any(x=>Vector3.Distance(origin,x.transform.position)<60f&&Vector3.Dot(transform.forward,x.transform.position-origin)>0);
            if(!passing&&profile.passing&&node.allowPassing&&node.width>=13&&!nearJunction&&angle<8&&blockedAt>0&&Time.time-blockedAt>2)
            {Vector3 passOrigin=origin+transform.right*3.1f;if(!Leader(passOrigin,transform.forward,35,out _)&&!Leader(passOrigin,-transform.forward,20,out _)){passing=true;passingUntil=Time.time+7;offset=-3.1f;}}
            if(passing&&(Time.time>=passingUntil||!node.allowPassing||nearJunction)){if(!Leader(origin-transform.right*3.1f,transform.forward,12,out _)&&!Leader(origin-transform.right*3.1f,-transform.forward,12,out _)){passing=false;offset=0;}}
            if(passing)targetSpeed=Mathf.Min(node.speedLimit/3.6f*profile.speedFactor,speed+profile.acceleration);
        }
        void SlowForPothole(Vector3 heading,float limit,ref float targetSpeed)
        {
            CityPothole? nearest=null;float distance=25f;
            foreach(var surface in surfaces)foreach(var hole in surface.potholes){Vector3 delta=hole.centre-body.position;float ahead=Vector3.Dot(delta,heading),side=Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(Vector3.up,heading)));if(ahead>0&&ahead<distance&&side<hole.radius+1f){distance=ahead;nearest=hole;}}
            if(!nearest.HasValue)return;targetSpeed=Mathf.Min(targetSpeed,limit/3.6f*.45f);float sideSign=Vector3.Dot(nearest.Value.centre-body.position,Vector3.Cross(Vector3.up,heading));offset=Mathf.Clamp(-Mathf.Sign(sideSign)*(nearest.Value.radius+.45f),-1.2f,1.2f);
        }
        bool Leader(Vector3 origin,Vector3 direction,float distance,out RaycastHit nearest)
        {
            nearest=default;float closest=float.MaxValue;
            const float detectionRadius=1.2f;
            int count=Physics.SphereCastNonAlloc(origin,detectionRadius,direction,hits,distance,~0,QueryTriggerInteraction.Ignore);
            RaycastHit[] found=count==hits.Length?Physics.SphereCastAll(origin,detectionRadius,direction,distance,~0,QueryTriggerInteraction.Ignore):hits;
            if(found!=hits)count=found.Length;
            for(int i=0;i<count;i++)
            {
                var hit=found[i];if(hit.transform.IsChildOf(transform))continue;
                var rb=hit.rigidbody;
                bool playerCollider=hit.collider.CompareTag("Player")||rb==playerBody;
                if((rb==null||!IsRoadUser(rb))&&!playerCollider)continue;
                if(hit.distance<closest){closest=hit.distance;nearest=hit;}
            }
            return closest<float.MaxValue;
        }
        static bool IsRoadUser(Rigidbody rb)=>rb.GetComponent<CityTrafficAgent>()!=null||rb.GetComponentInParent<KombiShift2.Traffic.RivalKombiAI>()!=null||rb.GetComponentInParent<KombiShift2.Traffic.KombiTrafficVehicle>()!=null||rb.CompareTag("Player");
        void KinematicMove(Vector3 delta,CityDriverProfile profile)
        {kinematicSpeed=Mathf.MoveTowards(kinematicSpeed,desiredSpeed,profile.acceleration*Time.fixedDeltaTime);Vector3 direction=delta.sqrMagnitude>.01f?delta.normalized:transform.forward;body.MovePosition(body.position+direction*kinematicSpeed*Time.fixedDeltaTime);body.MoveRotation(Quaternion.Slerp(body.rotation,Quaternion.LookRotation(direction),Time.fixedDeltaTime*3f));}
        public void SetDetailedPhysics(bool detailed)
        {
            if(detailed&&!body.isKinematic)return;if(!detailed&&body.isKinematic)return;
            if(detailed){body.isKinematic=false;body.linearVelocity=transform.forward*kinematicSpeed;body.WakeUp();}
            else{kinematicSpeed=body.linearVelocity.magnitude;body.isKinematic=true;}
        }
        void ChooseDestination(int blocked=-1)
        {
            if(Time.time<nextPlan)return;nextPlan=Time.time+.5f;int start=graph.Nearest(body.position,0);if(start<0)return;
            for(int attempt=0;attempt<12;attempt++){int end=UnityEngine.Random.Range(0,graph.Nodes.Length);if(graph.Nodes[end].component!=0||graph.Nodes[end].links.Length<2||Vector3.Distance(body.position,graph.Nodes[end].position)<120)continue;var candidate=graph.FindDirectedPath(start,end,transform.forward,-1,blocked);if(candidate.Count<2)continue;path=candidate;BuildLane();Replans++;return;}desiredSpeed=0;
        }
    }
}
