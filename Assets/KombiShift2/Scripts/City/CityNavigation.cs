using System.Collections.Generic;
using System.Linq;
using KombiShift2.Gameplay;
using UnityEngine;
namespace KombiShift2.City
{
    public sealed class CityNavigation : MonoBehaviour
    {
        CityRoadNetwork graph;KombiShiftGameDirector director;float next;Vector3 previousTarget;
        public IReadOnlyList<int> Path=>path;
        readonly List<int> path=new();
        public string Instruction {get;private set;}="Follow the route";
        public float Remaining {get;private set;}
        public int Recalculations {get;private set;}
        void Start(){graph=GetComponent<CityRoadNetwork>();director=FindFirstObjectByType<KombiShiftGameDirector>();}
        void Update()
        {
            if(graph==null||director==null||director.Finished||director.Paused||Time.time<next)return;next=Time.time+1;
            var player=director.Vehicle.transform;Vector3 target=director.CurrentTarget;
            int start=graph.Nearest(player.position,0),end=graph.Nearest(target,0);
            if(start<0||end<0)return;
            if(path.Count==0 || (target-previousTarget).sqrMagnitude>16f || !path.Contains(start))
            {var stop=director.Stations.OrderBy(s=>(s.ServicePoint.position-target).sqrMagnitude).FirstOrDefault();
                path.Clear();path.AddRange(stop!=null?graph.FindApproachPath(start,end,stop.ServicePoint.forward,player.forward):graph.FindPath(start,end));previousTarget=target;Recalculations++;}
            if(path.Count==0){Remaining=0;Instruction="No safe route found. Return to a connected street.";return;}
            int closest=0;float distance=float.MaxValue;
            for(int i=0;i<path.Count;i++){float d=(graph.Nodes[path[i]].position-player.position).sqrMagnitude;if(d<distance){distance=d;closest=i;}}
            Remaining=0;for(int i=closest+1;i<path.Count;i++)Remaining+=Vector3.Distance(graph.Nodes[path[i-1]].position,graph.Nodes[path[i]].position);
            Instruction="Continue straight";
            for(int i=closest+1;i+1<path.Count&&i<closest+12;i++)
            {
                Vector3 a=graph.Nodes[path[i]].position-graph.Nodes[path[i-1]].position,b=graph.Nodes[path[i+1]].position-graph.Nodes[path[i]].position;
                float angle=Vector3.SignedAngle(a,b,Vector3.up);
                if(Mathf.Abs(angle)>30f){Instruction=(angle>0?"Turn right":"Turn left")+" in "+Mathf.RoundToInt(Vector3.Distance(player.position,graph.Nodes[path[i]].position))+" m";break;}
            }
            if(path.Count<2)Instruction="Stop on the left at the service point";
        }
    }
}
