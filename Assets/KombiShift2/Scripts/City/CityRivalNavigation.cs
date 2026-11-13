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
            int start=graph.Nearest(transform.position,0),end=graph.Nearest(destination,0);
            bool destinationChanged=(destination-target).sqrMagnitude>16f;
            bool lostPath=path.Count<2;
            bool shouldPlan=(destinationChanged||lostPath)&&Time.time>=nextPlan;
            if(shouldPlan)
            {
                nextPlan=Time.time+1f;
                List<int> candidate=graph.FindApproachPath(start,end,heading,transform.forward);
                // A strict approach path can fail at intersections when the vehicle is
                // already turning. Fall back to a directed route, then an unrestricted
                // connected road path rather than leaving the rival stranded.
                if(candidate.Count<2)candidate=graph.FindDirectedPath(start,end,transform.forward);
                if(candidate.Count<2)candidate=graph.FindPath(start,end);
                if(candidate.Count>=2)
                {
                    path=candidate;
                    index=path.Count>2?1:0;
                }
                // Keep following the previous valid path if a temporary replan fails.
                target=destination;
            }
            if(path.Count<2)return destination;
            index=Mathf.Clamp(index,0,path.Count-1);
            // Advance past nodes that the kombi has already passed, including after a
            // sharp corner where the nearest node may no longer be directly ahead.
            while(index<path.Count-1&&Vector3.Distance(transform.position,graph.LanePoint(path,index))<5f)index++;
            Vector3 point=graph.LanePoint(path,index);
            if(index>=path.Count-2&&Vector3.Distance(transform.position,destination)<12f)return destination;
            return point;
        }
    }
}
