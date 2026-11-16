using System;
using System.Collections.Generic;
using UnityEngine;

namespace KombiShift2.City
{
    [Serializable] public sealed class CityRoadNode
    {
        public Vector3 position;
        public int[] links;
        public float width=7f, speedLimit=40f;
        public int component;
        public bool junction, allowPassing, roundabout;
    }
    [Serializable] public sealed class CityRoadData {public CityRoadNode[] nodes;public int components;public string provenance;}
    // Baked from the existing road triangles. No generated city or external map data.
    public sealed class CityRoadNetwork : MonoBehaviour
    {
        public static CityRoadNetwork Active {get;private set;}
        public TextAsset network;
        public CityRoadNode[] Nodes {get;private set;}=Array.Empty<CityRoadNode>();
        readonly Dictionary<Vector2Int,List<int>> spatial=new();
        void Awake(){Active=this;Load();}
        void OnDestroy(){if(Active==this)Active=null;}
        public void Load(bool force=false)
        {
            if(Nodes.Length>0&&!force)return;
            if(force){Nodes=Array.Empty<CityRoadNode>();spatial.Clear();}
            var source=network!=null?network:Resources.Load<TextAsset>("CityRoadNetwork");
            if(source==null)return;
            Nodes=JsonUtility.FromJson<CityRoadData>(source.text).nodes;
            for(int i=0;i<Nodes.Length;i++)
            {var key=Cell(Nodes[i].position);if(!spatial.TryGetValue(key,out var list))spatial[key]=list=new List<int>();list.Add(i);}
        }
        static Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/50f),Mathf.FloorToInt(p.z/50f));
        public int Nearest(Vector3 position,int component=-1)
        {
            Load();int best=-1;float distance=float.MaxValue;var cell=Cell(position);
            for(int radius=0;radius<6;radius++)
            {
                for(int x=-radius;x<=radius;x++)for(int z=-radius;z<=radius;z++)
                {
                    if(radius>0 && Mathf.Abs(x)!=radius && Mathf.Abs(z)!=radius)continue;
                    if(!spatial.TryGetValue(cell+new Vector2Int(x,z),out var indices))continue;
                    foreach(int index in indices)
                    {if(component>=0 && Nodes[index].component!=component)continue;float d=(Nodes[index].position-position).sqrMagnitude;if(d<distance){distance=d;best=index;}}
                }
                if(best>=0 && Mathf.Sqrt(distance)<radius*50f)return best;
            }
            return best;
        }
        public List<int> FindPath(int start,int destination,int blocked=-1)
        {
            if(start<0 || destination<0 || start>=Nodes.Length || destination>=Nodes.Length || Nodes[start].component!=Nodes[destination].component)return new List<int>();
            var open=new MinHeap();var costs=new Dictionary<int,float>{{start,0}};var previous=new Dictionary<int,int>();
            open.Push(start,0);
            while(open.Count>0)
            {
                int current=open.Pop();if(current==destination)
                {var path=new List<int>{current};while(previous.TryGetValue(current,out int parent)){current=parent;path.Add(current);}path.Reverse();return path;}
                foreach(int next in Nodes[current].links)
                {
                    if(next==blocked)continue;
                    float cost=costs[current]+Vector3.Distance(Nodes[current].position,Nodes[next].position)/Mathf.Max(3f,Nodes[next].speedLimit/3.6f);
                    if(costs.TryGetValue(next,out float old)&&old<=cost)continue;
                    costs[next]=cost;previous[next]=current;
                    open.Push(next,cost+Vector3.Distance(Nodes[next].position,Nodes[destination].position)/25f);
                }
            }
            return new List<int>();
        }
        public List<int> FindDirectedPath(int start,int destination,Vector3 initialHeading,int requiredBefore=-1,int blocked=-1)
        {
            if(start<0||destination<0||Nodes[start].component!=Nodes[destination].component)return new List<int>();
            int count=Nodes.Length;
            var open=new MinHeap();var costs=new Dictionary<int,float>{{start,0}};var parents=new Dictionary<int,int>();open.Push(start,0);
            while(open.Count>0)
            {
                int state=open.Pop(),current=state%count,previous=state/count-1;
                if(current==destination&&(requiredBefore<0||previous==requiredBefore))
                {
                    var result=new List<int>{current};while(parents.TryGetValue(state,out int parent)){state=parent;result.Add(state%count);}result.Reverse();return result;
                }
                foreach(int next in Nodes[current].links)
                {
                    if(next==previous||next==blocked)continue;
                    Vector3 direction=Nodes[next].position-Nodes[current].position;direction.y=0;
                    if(previous<0&&initialHeading.sqrMagnitude>.1f&&Vector3.Dot(direction.normalized,initialHeading.normalized)<-.35f)continue;
                    if(previous>=0)
                    {var incoming=Nodes[current].position-Nodes[previous].position;if(Vector3.Angle(incoming,direction)>145f)continue;}
                    if(next==destination&&requiredBefore>=0&&current!=requiredBefore)continue;
                    int nextState=(current+1)*count+next;
                    float cost=costs[state]+Vector3.Distance(Nodes[current].position,Nodes[next].position)/Mathf.Max(3,Nodes[next].speedLimit/3.6f);
                    if(costs.TryGetValue(nextState,out float old)&&old<=cost)continue;
                    costs[nextState]=cost;parents[nextState]=state;open.Push(nextState,cost+Vector3.Distance(Nodes[next].position,Nodes[destination].position)/25f);
                }
            }
            return new List<int>();
        }
        public List<int> FindTripPath(CityTripDefinition trip)
        {
            var stops=trip.stopNodes!=null&&trip.stopNodes.Length>1?trip.stopNodes:new[]{trip.originNode,trip.destinationNode};
            var result=new List<int>();Vector3 heading=Vector3.zero;
            for(int i=1;i<stops.Length;i++)
            {
                var leg=FindDirectedPath(stops[i-1],stops[i],heading);if(leg.Count<2)return new List<int>();
                heading=(Nodes[leg[leg.Count-1]].position-Nodes[leg[leg.Count-2]].position).normalized;
                if(result.Count>0)leg.RemoveAt(0);result.AddRange(leg);
            }
            return result;
        }
        public List<int> FindApproachPath(int start,int destination,Vector3 heading,Vector3 initialHeading=default)
        {
            if(start<0||destination<0)return new List<int>();
            if(start==destination&&Vector3.Dot(heading,initialHeading)>.5f)return new List<int>{destination};
            int before=-1;float best=.5f;
            foreach(int candidate in Nodes[destination].links)
            {
                float dot=Vector3.Dot((Nodes[destination].position-Nodes[candidate].position).normalized,heading);
                if(dot>best&&Array.IndexOf(Nodes[candidate].links,destination)>=0){best=dot;before=candidate;}
            }
            return FindDirectedPath(start,destination,initialHeading,before);
        }
        public Vector3 LanePoint(IReadOnlyList<int> path,int index,int lane=0)
        {
            var node=Nodes[path[index]];
            Vector3 direction=Nodes[path[Mathf.Min(index+1,path.Count-1)]].position-Nodes[path[Mathf.Max(0,index-1)]].position;direction.y=0;
            float offset=(node.width>=13f?4.65f:Mathf.Min(node.width*.30f,2.25f))+lane*3.1f;
            return node.position-Vector3.Cross(Vector3.up,direction.normalized)*offset;
        }
        public float Length(IReadOnlyList<int> path){float length=0;for(int i=1;i<path.Count;i++)length+=Vector3.Distance(Nodes[path[i-1]].position,Nodes[path[i]].position);return length;}
        sealed class MinHeap
        {
            readonly List<(int id,float priority)> values=new();public int Count=>values.Count;
            public void Push(int id,float priority){values.Add((id,priority));int i=values.Count-1;while(i>0){int p=(i-1)/2;if(values[p].priority<=priority)break;values[i]=values[p];i=p;}values[i]=(id,priority);}
            public int Pop(){var result=values[0];var last=values[values.Count-1];values.RemoveAt(values.Count-1);if(values.Count==0)return result.id;int i=0;while(i*2+1<values.Count){int c=i*2+1;if(c+1<values.Count&&values[c+1].priority<values[c].priority)c++;if(values[c].priority>=last.priority)break;values[i]=values[c];i=c;}values[i]=last;return result.id;}
        }
    }
}
