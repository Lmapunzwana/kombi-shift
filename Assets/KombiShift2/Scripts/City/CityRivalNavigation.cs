using System.Collections.Generic;
using UnityEngine;
namespace KombiShift2.City
{
    public sealed class CityRivalNavigation : MonoBehaviour
    {
        CityRoadNetwork graph;List<int> path=new();int index;Vector3 target;float nextPlan;
        public CityRoadNode Node=>graph!=null&&path.Count>0?graph.Nodes[path[Mathf.Clamp(index,0,path.Count-1)]]:null;
        public Vector3 Point(Transform service)
        {return Point(service.position,service.forward);}
        Vector3 Point(Vector3 destination,Vector3 heading)
        {
            graph??=CityRoadNetwork.Active;
            if(graph==null)return destination;
            if(path.Count==0 || (destination-target).sqrMagnitude>16 || Time.time>nextPlan&&Vector3.Distance(transform.position,graph.LanePoint(path,Mathf.Clamp(index,0,path.Count-1)))>20)
            {
                path=graph.FindApproachPath(graph.Nearest(transform.position,0),graph.Nearest(destination,0),heading,transform.forward);index=Mathf.Min(1,path.Count-1);target=destination;nextPlan=Time.time+2;
            }
            if(path.Count<2)return destination;
            var point=graph.LanePoint(path,index);
            if(Vector3.Distance(transform.position,point)<3.5f && index<path.Count-1){index++;point=graph.LanePoint(path,index);}
            if(index>=path.Count-2&&Vector3.Distance(transform.position,destination)<10)return destination;
            return point;
        }
    }
}
