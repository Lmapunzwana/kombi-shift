using System;
using KombiShift2.City;
using BezierSolution;
using KombiShift2.Gameplay;
using UnityEngine;

namespace KombiShift2.UI
{
    public sealed class KombiNavigationHud : MonoBehaviour
    {
        [SerializeField] private RCCP_CarController vehicle;
        [SerializeField] private Transform[] routePoints;
        [SerializeField] private KombiShiftGameDirector gameDirector;
        [SerializeField, Range(.12f,.65f)] private float mapMetresToPixels = .42f;
        private const int MapSize = 256;
        private Texture2D pixel, map;
        private Color32[] background, pixels;
        private Vector3[][] roads;
        private float nextMapUpdate;
        private GUIStyle label, small, number, heading;
        KombiSpeedometer speedometer;
        public Transform[] RoutePoints => routePoints;
        public RCCP_CarController Vehicle => vehicle;
        public KombiShiftGameDirector Director => gameDirector;
        public struct Layout { public Rect Map, Speed, Objective, Stats; }
        public static Layout GetLayout(float width, float height) => new()
        {
            Map = new Rect(18, height - 254, 208, 208),
            Speed = new Rect(width - 146, height - 102, 128, 84),
            Objective = new Rect(18, 18, Mathf.Min(370, width - 260), 78),
            Stats = new Rect(width - 218, 18, 200, 62)
        };
        public void SetVehicle(RCCP_CarController value) { vehicle = value; if(speedometer!=null)speedometer.Configure(vehicle,gameDirector); }
        public void Configure(RCCP_CarController value, Transform[] points, KombiShiftGameDirector director)
        { vehicle = value; routePoints = points; gameDirector = director; if(speedometer!=null)speedometer.Configure(vehicle,gameDirector); }

        private void Awake()
        {
            pixel = new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
            map = new Texture2D(MapSize,MapSize,TextureFormat.RGBA32,false) { filterMode = FilterMode.Bilinear };
            background = new Color32[MapSize*MapSize]; pixels = new Color32[background.Length];
            for (int y=0;y<MapSize;y++) for (int x=0;x<MapSize;x++)
            {
                float radius = Vector2.Distance(new Vector2(x,y),new Vector2(MapSize*.5f,MapSize*.5f));
                background[y*MapSize+x] = radius > 127f ? new Color32(0,0,0,0) : radius > 123f ? new Color32(211,220,223,255) : new Color32(13,23,29,255);
            }
            var splines = FindObjectsByType<BezierSpline>(FindObjectsSortMode.None);
            roads = new Vector3[splines.Length][];
            for (int s=0;s<splines.Length;s++)
            {
                roads[s] = new Vector3[65];
                for (int i=0;i<65;i++) roads[s][i] = splines[s].GetPoint(i/64f);
            }
        }

        private void Start()
        {
            speedometer=GetComponent<KombiSpeedometer>()??gameObject.AddComponent<KombiSpeedometer>();
            speedometer.Configure(vehicle,gameDirector);
        }

        private void Update()
        {
            if (vehicle == null || (gameDirector != null && (gameDirector.Paused || gameDirector.Finished)) || Time.unscaledTime < nextMapUpdate) return;
            nextMapUpdate = Time.unscaledTime + .15f;
            Array.Copy(background,pixels,pixels.Length);
            var city=CityRoadNetwork.Active;
            if(city!=null)
            {
                for(int n=0;n<city.Nodes.Length;n++)
                {
                    var node=city.Nodes[n];if((node.position-vehicle.transform.position).sqrMagnitude>220000)continue;
                    foreach(int next in node.links)if(next>n)Line(ToMap(node.position),ToMap(city.Nodes[next].position),new Color32(90,116,125,255),2);
                }
                var guidance=city.GetComponent<CityNavigation>();
                if(guidance!=null)for(int n=1;n<guidance.Path.Count;n++)Line(ToMap(city.Nodes[guidance.Path[n-1]].position),ToMap(city.Nodes[guidance.Path[n]].position),new Color32(255,208,78,255),3);
            }
            foreach (var road in roads)
                for (int i=1;i<road.Length;i++) Line(ToMap(road[i-1]),ToMap(road[i]),new Color32(126,151,162,255),2);
            if (!CityTripSession.Active && routePoints != null && routePoints.Length > 1 && gameDirector != null)
            {
                int from=NearestRoutePoint(vehicle.transform.position),to=NearestRoutePoint(gameDirector.CurrentTarget);
                int low=Mathf.Min(from,to),high=Mathf.Max(from,to);
                for (int i=low+1;i<=high;i++)
                {
                    Line(ToMap(routePoints[i-1].position),ToMap(routePoints[i].position),new Color32(30,29,18,255),5);
                }
                for (int i=low+1;i<=high;i++)
                    Line(ToMap(routePoints[i-1].position),ToMap(routePoints[i].position),new Color32(255,208,78,255),3);
            }
            if (gameDirector != null)
            {
                if (gameDirector.Stations != null) foreach (var stop in gameDirector.Stations)
                    if (stop != null && (stop.HasWaiting || stop.Terminal)) Ring(ToMap(stop.ServicePoint.position),5,new Color32(255,210,82,255));
                if (gameDirector.Rivals != null) foreach (var rival in gameDirector.Rivals)
                    if (rival != null) { Dot(ToMap(rival.transform.position),6,new Color32(18,20,24,255));Dot(ToMap(rival.transform.position),4,(Color32)rival.MarkerColor); }
            }
            if(gameDirector!=null)
            {
                Vector2 target=ToMap(gameDirector.CurrentTarget);
                Vector2 delta=target-Vector2.one*(MapSize*.5f);
                if(delta.magnitude>110f) target=Vector2.one*(MapSize*.5f)+delta.normalized*110f;
                Ring(target,8,new Color32(255,212,86,255));
            }
            Dot(new Vector2(MapSize*.5f,MapSize*.5f),7,new Color32(246,250,251,255));
            Dot(new Vector2(MapSize*.5f,MapSize*.5f),4,new Color32(54,202,216,255));
            map.SetPixels32(pixels); map.Apply(false);
        }
        private Vector2 ToMap(Vector3 world)
        {
            Vector3 relative = world - vehicle.transform.position;
            return new Vector2(MapSize*.5f+Vector3.Dot(relative,vehicle.transform.right)*mapMetresToPixels, MapSize*.5f+Vector3.Dot(relative,vehicle.transform.forward)*mapMetresToPixels);
        }
        private void Put(int x,int y,Color32 color)
        {
            if (x<0||y<0||x>=MapSize||y>=MapSize) return;
            float dx=x-MapSize*.5f,dy=y-MapSize*.5f;
            if (dx*dx+dy*dy < 121f*121f) pixels[y*MapSize+x]=color;
        }
        private int NearestRoutePoint(Vector3 position)
        {
            int nearest=0;float best=float.MaxValue;
            for(int i=0;i<routePoints.Length;i++)
            { float distance=(routePoints[i].position-position).sqrMagnitude;if(distance<best){best=distance;nearest=i;} }
            return nearest;
        }
        private void Ring(Vector2 point,int radius,Color32 color)
        {
            Dot(point,radius+2,new Color32(10,16,20,255));Dot(point,radius,color);Dot(point,Mathf.Max(1,radius-3),new Color32(15,24,30,255));
        }
        private void Dot(Vector2 p,int radius,Color32 color)
        {
            for (int x=-radius;x<=radius;x++) for (int y=-radius;y<=radius;y++)
                if (x*x+y*y<=radius*radius) Put(Mathf.RoundToInt(p.x)+x,Mathf.RoundToInt(p.y)+y,color);
        }
        private void Line(Vector2 a,Vector2 b,Color32 color,int width)
        {
            int count=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(a,b)),1,512);
            for (int i=0;i<=count;i++) Dot(Vector2.Lerp(a,b,i/(float)count),width,color);
        }

        private void OnGUI()
        {
            if (vehicle == null || gameDirector == null || gameDirector.Paused || gameDirector.Finished) return;
            if (label == null)
            {
                label = Style(16,TextAnchor.MiddleLeft,Color.white);
                small = Style(14,TextAnchor.MiddleLeft,new Color(.77f,.82f,.81f));
                number = Style(36,TextAnchor.MiddleCenter,Color.white);
                heading = Style(15,TextAnchor.MiddleLeft,new Color(.96f,.76f,.36f));
                heading.fontStyle = FontStyle.Bold;
            }
            Rect safe=Screen.safeArea;
            float scale=Mathf.Min(1f,safe.width/900f,safe.height/720f);
            Matrix4x4 previous=GUI.matrix;
            Color previousColor=GUI.color;
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x,Screen.height-safe.yMax),Quaternion.identity,Vector3.one*scale);
            float width=safe.width/scale,height=safe.height/scale;
            Layout layout=GetLayout(width,height);
            Panel(layout.Objective); Panel(layout.Stats);
            int current=Mathf.Min(gameDirector.CurrentStop+1,gameDirector.StopCount);
            string objective=current==gameDirector.StopCount ? "MBARE DROP-OFF" : $"PICKUP {current} / {gameDirector.StopCount-1}";
            GUI.Label(new Rect(30,23,layout.Objective.width-24,24),objective,heading);
            GUI.Label(new Rect(30,47,layout.Objective.width-24,22),$"{gameDirector.Passengers} aboard   ·   Rivals collected {gameDirector.RivalPassengers}",label);
            GUI.Label(new Rect(30,70,layout.Objective.width-24,20),gameDirector.Notice,small);
            int seconds=Mathf.CeilToInt(gameDirector.TimeRemaining);
            GUI.Label(new Rect(layout.Stats.x+14,23,180,24),$"${gameDirector.Earnings:0.00} net    {seconds/60:00}:{seconds%60:00}",label);
            GUI.Label(new Rect(layout.Stats.x+14,48,180,22),$"{KombiChallenge.Name(gameDirector.Stage)} / {gameDirector.MinimumPassengers}+",small);
            GUI.color=Color.white; GUI.DrawTexture(layout.Map,map);
            var guidance=CityRoadNetwork.Active!=null?CityRoadNetwork.Active.GetComponent<CityNavigation>():null;
            if(guidance!=null)GUI.Label(new Rect(245,Screen.height-65,420,45),guidance.Instruction+"  /  "+Mathf.RoundToInt(guidance.Remaining)+" m",small);
            float distance=Vector3.Distance(vehicle.transform.position,gameDirector.CurrentTarget);
            GUI.Label(new Rect(24,layout.Map.yMax+2,208,22),$"NEXT STOP   {distance:0} m",heading);
            GUI.color=new Color(.21f,.79f,.85f);GUI.DrawTexture(new Rect(24,layout.Map.yMax+27,6,6),pixel);GUI.color=Color.white;
            GUI.Label(new Rect(34,layout.Map.yMax+20,43,20),"YOU",small);
            GUI.color=new Color(1f,.82f,.32f);GUI.DrawTexture(new Rect(80,layout.Map.yMax+27,6,6),pixel);GUI.color=Color.white;
            GUI.Label(new Rect(90,layout.Map.yMax+20,46,20),"STOP",small);
            GUI.color=new Color(1f,.44f,.39f);GUI.DrawTexture(new Rect(143,layout.Map.yMax+27,6,6),pixel);GUI.color=Color.white;
            GUI.Label(new Rect(153,layout.Map.yMax+20,60,20),"RIVAL",small);GUI.color=Color.white;
            // Secondary status remains grouped below the existing time/earnings panel.
            Rect condition = new Rect(layout.Stats.x, 88, 200, 112);
            Panel(condition);
            GUI.Label(new Rect(condition.x+12,92,180,22), $"Condition {gameDirector.Condition.Health:0}%   Limit {gameDirector.Law.SpeedLimit:0}", small);
            GUI.Label(new Rect(condition.x+12,116,180,22), $"XP {gameDirector.Quality.XP}   Happiness {gameDirector.Happiness:0}%", small);
            GUI.Label(new Rect(condition.x+12,140,180,22), $"Delivered {gameDirector.Delivered}   Fines ${gameDirector.Law.TotalFines}", small);
            GUI.Label(new Rect(condition.x+12,164,180,22), $"T: repair at stop (${gameDirector.Condition.RepairPrice})", small);
            if (!string.IsNullOrEmpty(gameDirector.DropRequest))
            {
                Panel(new Rect(18,104,Mathf.Min(460,width-260),48));
                GUI.Label(new Rect(30,106,Mathf.Min(436,width-284),44), gameDirector.DropRequest, heading);
            }
            if (gameDirector.Rivals != null)
            {
                float rivalY = 166f;
                foreach (var rival in gameDirector.Rivals)
                {
                    if (rival == null) continue;
                    GUI.color = rival.MarkerColor;
                    string claim = rival.Target == null ? rival.State.ToString() : $"{rival.Target.name} {Vector3.Distance(rival.transform.position,rival.Target.ServicePoint.position):0}m";
                    GUI.Label(new Rect(24,rivalY,Mathf.Min(560,width-260),22), $"{rival.name.Replace("Rival Kombi — ","")}: {rival.PassengersTaken} | {claim}", small);
                    rivalY += 24f;
                }
            }
            GUI.matrix=previous;GUI.color=previousColor;
        }
        private static GUIStyle Style(int size,TextAnchor alignment,Color color)
        { var style=new GUIStyle(GUI.skin.label) { fontSize=size,alignment=alignment };style.normal.textColor=color;return style; }
        private void Panel(Rect rect)
        { GUI.color=new Color(.045f,.06f,.065f,.88f);GUI.DrawTexture(rect,pixel);GUI.color=Color.white; }
        private void OnDestroy() { if (pixel!=null) Destroy(pixel);if (map!=null) Destroy(map); }
    }
}
