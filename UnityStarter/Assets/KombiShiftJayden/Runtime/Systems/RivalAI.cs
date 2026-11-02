using System.Collections.Generic;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class RivalAI:MonoBehaviour
    {
        public enum State{Cruise,ChasePassenger,Block,Flee}
        public State Current{get;private set;}public float Health{get;private set;}=100;
        ShiftGame game;RoadGraph roads;Rigidbody body;List<Vector3> path=new List<Vector3>();int waypoint;float decisionAt,blockedFor,aggression,blockUntil,nextBlock;PassengerAI prey;Vector3 lastPlayer;bool highway;
        public void Configure(ShiftGame owner,RoadGraph graph,float factor,bool wet){game=owner;roads=graph;aggression=factor;highway=wet;body=GetComponent<Rigidbody>();Health=100;Current=State.Cruise;decisionAt=blockedFor=blockUntil=nextBlock=0;Plan(roads.nodes[roads.nodes.Count-1]);}
        void Plan(Vector3 target){path=roads.Path(body.position,target);waypoint=0;while(waypoint<path.Count-1&&Vector3.Distance(body.position,path[waypoint])<6)waypoint++;}
        void FixedUpdate()
        {
            if(game==null||!game.IsPlaying)return;
            Vector3 player=game.PlayerPosition;
            if(Time.time>=decisionAt){
                decisionAt=Time.time+.4f;State next;
                if(Health<30||blockedFor>1.4f)next=State.Flee;
                else if(Time.time<blockUntil)next=State.Block;
                else if(Vector3.Distance(player,body.position)<14*aggression&&Time.time>nextBlock){next=State.Block;blockUntil=Time.time+1.2f;nextBlock=Time.time+8/aggression;}
                else {prey=game.NearestWaiting(body.position);next=prey!=null?State.ChasePassenger:State.Cruise;}
                bool replan=next!=Current||path.Count==0||waypoint>=path.Count;Current=next;
                if(Current==State.ChasePassenger&&prey!=null){Vector3 goal=game.PassengerLocation(prey);if(replan||Vector3.Distance(path[path.Count-1],goal)>2)Plan(goal);}
                else if(replan&&Current==State.Block)Plan(game.RoadPoint(player+game.PlayerForward*7));
                else if(replan&&Current==State.Flee){Vector3 far=roads.nodes[0];foreach(var n in roads.nodes)if(Vector3.Distance(n,player)>Vector3.Distance(far,player))far=n;Plan(far);}
                else if(replan&&Current==State.Cruise)Plan(roads.nodes[(int)(Time.time*.2f)%roads.nodes.Count]);
            }
            bool obstacle=Physics.SphereCast(body.position+Vector3.up*.8f+transform.forward*2.8f,.5f,transform.forward,out var hit,5,~0,QueryTriggerInteraction.Ignore);
            if(obstacle){blockedFor+=Time.fixedDeltaTime;if(hit.collider.GetComponent<KombiDriver>()!=null)Health=Mathf.Max(0,Health-Time.fixedDeltaTime*3);if(Current==State.Flee&&!Physics.SphereCast(body.position+Vector3.up*.8f-transform.forward*2.8f,.5f,-transform.forward,out var rear,3,~0,QueryTriggerInteraction.Ignore))body.MovePosition(body.position-transform.forward*3*Time.fixedDeltaTime);return;}blockedFor=0;
            if(Current==State.ChasePassenger&&prey!=null&&Vector3.Distance(body.position,game.PassengerLocation(prey))<4){game.TakeRivalPassenger(prey);prey=null;path.Clear();return;}
            if(waypoint>=path.Count)return;Vector3 goalPoint=path[waypoint];goalPoint.y=.225f;Vector3 delta=goalPoint-body.position;
            if(delta.magnitude<1.2f){waypoint++;return;}
            float speed=(Current==State.Flee?10:Current==State.Block?6:8)*Mathf.Lerp(1,1.15f,aggression-1);
            body.MovePosition(Vector3.MoveTowards(body.position,goalPoint,speed*Time.fixedDeltaTime));body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(delta.normalized),150*Time.fixedDeltaTime));
        }
        public void ApplyDamage(float damage){Health=Mathf.Max(0,Health-damage);if(Health<30)decisionAt=0;}
        void OnCollisionEnter(Collision collision){ApplyDamage(collision.relativeVelocity.magnitude*2);}
    }
}
