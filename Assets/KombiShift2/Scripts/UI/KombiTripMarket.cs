using System;
using System.Collections.Generic;
using KombiShift2.City;
using KombiShift2.Gameplay;
using UnityEngine;
namespace KombiShift2.UI
{
    public sealed class KombiTripMarket : MonoBehaviour
    {
        public bool IsOpen {get;private set;}
        public event Action StartRequested;
        CityRoadNetwork graph;CityTripDefinition[] trips;float[] distances;List<int>[] paths;int selected;List<int> route=new();Vector2 scroll,pan;float zoom=1;string problem;
        CityTripPlan[] plans;
        readonly KombiShiftConfiguration[] configurations=new KombiShiftConfiguration[2];
        float[,] estimatedGross,averageFares;
        int StageIndex=>KombiChallenge.Selected==KombiStage.RushHour?1:0;
        GUIStyle heading,body,small,cardStyle,startStyle;Texture2D pixel,topology;Bounds mapBounds;Color32[] baseMap,routeMap;
        public void Open()
        {
            if(graph==null){var go=new GameObject("Trip market road graph");go.transform.SetParent(transform);graph=go.AddComponent<CityRoadNetwork>();graph.Load();}
            if(topology==null)BakeMap();
            trips=CityTripCatalog.Load().trips;distances=new float[trips.Length];paths=new List<int>[trips.Length];
            configurations[0]=new KombiShiftConfiguration(KombiStage.Morning);
            configurations[1]=new KombiShiftConfiguration(KombiStage.RushHour);
            plans=new CityTripPlan[trips.Length];estimatedGross=new float[2,trips.Length];averageFares=new float[2,trips.Length];
            for(int i=0;i<trips.Length;i++)
            {
                plans[i]=graph.PlanTrip(trips[i]);paths[i]=plans[i]!=null?new List<int>(plans[i].Path):new List<int>();distances[i]=plans[i]?.Distance??0f;
                for(int stage=0;stage<2;stage++)
                {
                    estimatedGross[stage,i]=plans[i]?.ExpectedGross(configurations[stage])??0f;
                    int demand=configurations[stage].TripDemand(plans[i]?.StopCount??0);
                    averageFares[stage,i]=demand>0?estimatedGross[stage,i]/demand:0f;
                }
            }
            IsOpen=true;Select(Mathf.Clamp(selected,0,Math.Max(0,trips.Length-1)));
        }
        public bool SelectTrip(string id)
        {if(!IsOpen)Open();int index=Array.FindIndex(trips,t=>t.id==id);if(index<0)return false;Select(index);return problem==null;}
        public bool StartSelected()
        {
            if(!IsOpen||problem!=null||trips==null||trips.Length==0)return false;
            CityTripSelection.Selected=trips[selected];CityTripSelection.Distance=distances[selected];IsOpen=false;StartRequested?.Invoke();return true;
        }
        void Select(int index)
        {
            selected=index;route=new List<int>();problem=null;
            if(trips.Length==0){problem="City routes have not been baked. Return to the title.";return;}
            var trip=trips[index];route=paths[index];
            if(route.Count<2)problem="This route is disconnected or has invalid fare settings and cannot be started.";
            BakeSelectedRoute();
        }
        void Update(){if(IsOpen&&Input.GetKeyDown(KeyCode.Escape))IsOpen=false;if(IsOpen&&Input.GetKeyDown(KeyCode.F2))ChangeStage();}
        void ChangeStage(){KombiChallenge.Selected=KombiChallenge.Selected==KombiStage.Morning?KombiStage.RushHour:KombiStage.Morning;}
        void OnGUI()
        {
            if(!IsOpen)return;
            if(pixel==null){pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();heading=new GUIStyle(GUI.skin.label){fontSize=28,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};small=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};cardStyle=new GUIStyle(GUI.skin.button){fontSize=18,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(14,10,6,6)};startStyle=new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};startStyle.normal.textColor=new Color(.06f,.09f,.1f);}
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);var matrix=GUI.matrix;Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.045f,.055f));GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-800*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            Fill(new Rect(0,0,1280,800),new Color(.025f,.045f,.055f));GUI.Label(new Rect(30,22,950,45),"TRIP MARKET  /  "+KombiChallenge.Name(KombiChallenge.Selected),heading);
            GUI.Label(new Rect(30,65,1120,35),"Fictional Harare routes mapped to reachable streets in the existing city. Fares are game values.",small);
            if(GUI.Button(new Rect(900,24,220,38),"F2 / CHANGE STAGE"))ChangeStage();
            if(GUI.Button(new Rect(1140,24,110,38),"BACK"))IsOpen=false;
            scroll=GUI.BeginScrollView(new Rect(25,113,355,487),scroll,new Rect(0,0,330,(trips?.Length??0)*90));
            for(int i=0;i<(trips?.Length??0);i++)
            {
                var trip=trips[i];float distance=distances[i];float fare=averageFares[StageIndex,i];
                GUI.backgroundColor=i==selected?new Color(.95f,.65f,.2f):new Color(.2f,.32f,.36f);
                if(GUI.Button(new Rect(0,i*90,325,82),trip.origin+" → "+trip.destination+"\n"+(distance/1000f).ToString("0.0")+" km  |  US$"+fare.ToString("0.00")+" average\nPotential gross US$"+estimatedGross[StageIndex,i].ToString("0.00"),cardStyle))Select(i);
            }
            GUI.EndScrollView();GUI.backgroundColor=Color.white;
            Rect map=new(405,113,845,487);Fill(map,new Color(.065f,.1f,.12f));
            if(map.Contains(Event.current.mousePosition))
            {
                if(Event.current.type==EventType.ScrollWheel){zoom=Mathf.Clamp(zoom*Mathf.Exp(-Event.current.delta.y*.08f),.7f,5f);Event.current.Use();}
                if(Event.current.type==EventType.MouseDrag){pan+=Event.current.delta;Event.current.Use();}
            }
            DrawMap(map);
            GUI.Label(new Rect(410,602,830,22),"Scroll to zoom · drag to pan · cyan origin · green terminal · white stops",small);
            if(trips!=null&&trips.Length>0)
            {
                var trip=trips[selected];float distance=distances[selected],fare=averageFares[StageIndex,selected];
                GUI.Label(new Rect(30,626,900,40),trip.origin+" → "+trip.destination,heading);
                GUI.Label(new Rect(30,674,900,80),$"{distance/1000f:0.0} km  ·  Shift allowance {Mathf.CeilToInt(configurations[StageIndex].TripDuration(distance,trip.stopNodes?.Length??2)/60f)} min  ·  Demand {configurations[StageIndex].TripDemand(trip.stopNodes?.Length??2)}\nAverage fare US${fare:0.00}  ·  Potential gross US${estimatedGross[StageIndex,selected]:0.00}\nDifficulty {trip.difficulty}/5  ·  {(KombiChallenge.Selected==KombiStage.RushHour?"Heavy traffic / competitive rivals":"Moderate traffic / longer allowance")}",body);
                GUI.enabled=problem==null;
                var startRect=new Rect(990,656,260,84);Fill(startRect,new Color(.96f,.68f,.23f));
                if(GUI.Button(startRect,"START TRIP",startStyle)){StartSelected();}
                GUI.enabled=true;
            }
            if(problem!=null)GUI.Label(new Rect(410,570,820,38),problem,body);
            GUI.matrix=matrix;
        }
        void DrawMap(Rect rect)
        {
            if(graph.Nodes.Length==0)return;
            Bounds bounds=mapBounds;
            float factor=Mathf.Min((rect.width-35)/Mathf.Max(1,bounds.size.x),(rect.height-35)/Mathf.Max(1,bounds.size.z))*zoom;
            GUI.BeginGroup(rect);
            GUI.DrawTexture(new Rect(rect.width*.5f+pan.x-bounds.size.x*factor*.5f,rect.height*.5f+pan.y-bounds.size.z*factor*.5f,bounds.size.x*factor,bounds.size.z*factor),topology);
            GUI.EndGroup();
        }
        void BakeMap()
        {
            if(graph.Nodes.Length==0)return;
            mapBounds=new Bounds(graph.Nodes[0].position,Vector3.zero);foreach(var node in graph.Nodes)mapBounds.Encapsulate(node.position);
            const int size=1024;var pixels=new Color32[size*size];var background=new Color32(17,26,31,255);for(int i=0;i<pixels.Length;i++)pixels[i]=background;
            Vector2Int Map(Vector3 p)=>new(Mathf.RoundToInt((p.x-mapBounds.min.x)/mapBounds.size.x*(size-1)),Mathf.RoundToInt((p.z-mapBounds.min.z)/mapBounds.size.z*(size-1)));
            for(int i=0;i<graph.Nodes.Length;i++)foreach(int next in graph.Nodes[i].links)
            {
                if(next<i)continue;var a=Map(graph.Nodes[i].position);var b=Map(graph.Nodes[next].position);int steps=Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));
                for(int t=0;t<=steps;t++){float blend=steps==0?0:(float)t/steps;int x=Mathf.RoundToInt(Mathf.Lerp(a.x,b.x,blend)),y=Mathf.RoundToInt(Mathf.Lerp(a.y,b.y,blend));for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(x+dx>=0&&x+dx<size&&y+dy>=0&&y+dy<size)pixels[(y+dy)*size+x+dx]=new Color32(98,129,138,255);}
            }
            baseMap=pixels;routeMap=new Color32[pixels.Length];
            topology=new Texture2D(size,size,TextureFormat.RGB24,false);topology.SetPixels32(pixels);topology.Apply();
        }
        void BakeSelectedRoute()
        {
            if(topology==null||baseMap==null)return;Array.Copy(baseMap,routeMap,baseMap.Length);const int size=1024;
            Vector2Int Map(Vector3 p)=>new(Mathf.RoundToInt((p.x-mapBounds.min.x)/mapBounds.size.x*(size-1)),Mathf.RoundToInt((p.z-mapBounds.min.z)/mapBounds.size.z*(size-1)));
            void Dot(Vector2Int p,int radius,Color32 color){for(int y=-radius;y<=radius;y++)for(int x=-radius;x<=radius;x++){int px=p.x+x,py=p.y+y;if(px>=0&&px<size&&py>=0&&py<size)routeMap[py*size+px]=color;}}
            for(int i=1;i<route.Count;i++)
            {
                var a=Map(graph.Nodes[route[i-1]].position);var b=Map(graph.Nodes[route[i]].position);int steps=Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));
                for(int t=0;t<=steps;t++){float blend=steps==0?0:(float)t/steps;Dot(new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(a.x,b.x,blend)),Mathf.RoundToInt(Mathf.Lerp(a.y,b.y,blend))),2,new Color32(255,190,65,255));}
            }
            if(route.Count>0)
            {
                foreach(int stop in trips[selected].stopNodes??Array.Empty<int>())Dot(Map(graph.Nodes[stop].position),4,new Color32(235,240,240,255));
                Dot(Map(graph.Nodes[route[0]].position),7,new Color32(50,215,230,255));Dot(Map(graph.Nodes[route[route.Count-1]].position),7,new Color32(50,235,105,255));
            }
            topology.SetPixels32(routeMap);topology.Apply();
        }
        void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,pixel);GUI.color=Color.white;}
        void OnDestroy(){if(pixel!=null)Destroy(pixel);if(topology!=null)Destroy(topology);}
    }
}
