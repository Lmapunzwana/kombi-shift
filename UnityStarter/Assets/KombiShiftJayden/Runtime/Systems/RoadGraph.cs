using System.Collections.Generic;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class RoadGraph
    {
        public readonly List<Vector3> nodes=new List<Vector3>();readonly List<List<int>> links=new List<List<int>>();
        public RoadGraph(bool highway)
        {
            float[] zs=highway?new[]{-20f,45,185,325,465,530}:new[]{-20f,25,85,145,205,250};
            int streets=highway?1:2;
            for(int x=0;x<streets;x++)foreach(float z in zs){nodes.Add(new Vector3(x*40,.225f,z));links.Add(new List<int>());}
            for(int s=0;s<streets;s++)for(int i=0;i<zs.Length-1;i++)Connect(s*zs.Length+i,s*zs.Length+i+1);
            if(!highway)for(int i=1;i<zs.Length;i++)Connect(i,zs.Length+i);
        }
        void Connect(int a,int b){links[a].Add(b);links[b].Add(a);}
        int Closest(Vector3 p){int best=0;float d=float.MaxValue;for(int i=0;i<nodes.Count;i++){float n=Vector3.Distance(p,nodes[i]);if(n<d){d=n;best=i;}}return best;}
        public List<Vector3> Path(Vector3 from,Vector3 to)
        {
            int a=Closest(from),b=Closest(to),n=nodes.Count;var dist=new float[n];var prev=new int[n];var used=new bool[n];for(int i=0;i<n;i++){dist[i]=float.MaxValue;prev[i]=-1;}dist[a]=0;
            for(int step=0;step<n;step++){int u=-1;for(int i=0;i<n;i++)if(!used[i]&&(u<0||dist[i]<dist[u]))u=i;if(u<0||dist[u]==float.MaxValue)break;used[u]=true;foreach(int v in links[u]){float d=dist[u]+Vector3.Distance(nodes[u],nodes[v]);if(d<dist[v]){dist[v]=d;prev[v]=u;}}}
            var path=new List<Vector3>();for(int at=b;at>=0;at=prev[at]){path.Add(nodes[at]);if(at==a)break;}path.Reverse();path.Add(new Vector3(to.x,.225f,to.z));return path;
        }
        public float Distance(Vector3 from,Vector3 to){var p=Path(from,to);float sum=0;Vector3 last=from;foreach(var v in p){sum+=Vector3.Distance(last,v);last=v;}return sum;}
    }
}
