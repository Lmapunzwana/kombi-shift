using System.Collections.Generic;
using UnityEngine;
namespace KombiShift2.City
{
    public sealed class CityRivalNavigation : MonoBehaviour
    {
        CityRoadNetwork graph;List<int> path=new();int index;Vector3 target;float nextPlan;
        public CityRoadNode Node=>graph!=null&&path.Count>0?graph.Nodes[path[Mathf.Clamp(index,0,path.Count-1)]]:null;
        public bool HasPath=>graph==null||path.Count>=2;
        public void Replan(){target=new Vector3(1e6f,0f,1e6f);nextPlan=0f;}
        public Vector3 Point(Transform service)
        {return Point(service.position,service.forward);}
        Vector3 Point(Vector3 destination,Vector3 heading)
        {
            graph??=CityRoadNetwork.Active;
            if(graph==null)return destination;
            bool destinationChanged=(destination-target).sqrMagnitude>16f;
            bool lostPath=path.Count<2&&Time.time>=nextPlan;
            if(destinationChanged||lostPath)
            {
                var candidate=graph.FindApproachPath(graph.Nearest(transform.position,0),graph.Nearest(destination,0),heading,transform.forward);
                target=destination;nextPlan=Time.time+2f;
                if(candidate.Count>=2){path=candidate;index=1;}
                else {path.Clear();index=0;}
            }
            if(path.Count<2)return transform.position;
            var point=graph.LanePoint(path,index);
            if(Vector3.Distance(transform.position,point)<3.5f && index<path.Count-1){index++;point=graph.LanePoint(path,index);}
            if(index>=path.Count-2&&Vector3.Distance(transform.position,destination)<10)return destination;
            return point;
        }
    }
}
