using System.Linq;
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
        private BezierSpline[] routes;
        private Texture2D pixel;

        public void SetVehicle(RCCP_CarController value) => vehicle = value;
        public void Configure(RCCP_CarController value, Transform[] points, KombiShiftGameDirector director)
        {
            vehicle = value;
            routePoints = points;
            gameDirector = director;
        }

        private void Awake()
        {
            routes = FindObjectsByType<BezierSpline>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private void OnGUI()
        {
            if (vehicle == null || ((routePoints == null || routePoints.Length < 2) && (routes == null || routes.Length == 0))) return;
            Rect panel = new(Screen.width - 400f, Screen.height - 350f, 380f, 330f);
            GUI.color = new Color(0.035f, 0.045f, 0.055f, 0.94f);
            GUI.DrawTexture(panel, pixel);
            GUI.color = Color.white;

            Vector3 target;
            float remaining;
            if (routePoints != null && routePoints.Length >= 2)
            {
                target = gameDirector != null ? gameDirector.CurrentTarget : routePoints[FindNearestPointIndex()].position;
                remaining = Vector3.Distance(vehicle.transform.position, target);
            }
            else
            {
                (BezierSpline route, float t) = FindNearestRoute();
                target = route.GetPoint(Mathf.Min(1f, t + 0.055f));
                remaining = Mathf.Max(0f, (1f - t) * route.length);
            }
            Vector3 localTarget = vehicle.transform.InverseTransformPoint(target);
            float turnAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            string instruction = Mathf.Abs(turnAngle) < 18f ? "CONTINUE" : turnAngle < 0f ? "TURN LEFT" : "TURN RIGHT";

            GUIStyle instructionStyle = new(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            instructionStyle.normal.textColor = new Color(1f, 0.78f, 0.12f);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 230f, 32f), instruction, instructionStyle);
            GUI.Label(new Rect(panel.x + 275f, panel.y + 13f, 86f, 28f), $"{remaining:0} m");

            Rect map = new(panel.x + 15f, panel.y + 50f, 350f, 185f);
            GUI.color = new Color(0.10f, 0.12f, 0.14f);
            GUI.DrawTexture(map, pixel);
            GUI.color = Color.white;
            DrawRouteMap(map);

            GUIStyle speedStyle = new(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            speedStyle.normal.textColor = Color.white;
            DrawSpeedArc(new Vector2(panel.x + 282f, panel.y + 281f), 52f, Mathf.Clamp01(vehicle.absoluteSpeed / 180f));
            GUI.Label(new Rect(panel.x + 205f, panel.y + 245f, 120f, 52f), $"{vehicle.absoluteSpeed:0}", speedStyle);
            GUIStyle unitStyle = new(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.LowerLeft };
            unitStyle.normal.textColor = new Color(.75f, .8f, .82f);
            GUI.Label(new Rect(panel.x + 326f, panel.y + 257f, 48f, 30f), "km/h", unitStyle);
            string gear = vehicle.direction < 0 ? "R" : vehicle.absoluteSpeed < 1f ? "N" : Mathf.Max(1, vehicle.currentGear).ToString();
            GUIStyle gearStyle = new(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            gearStyle.normal.textColor = new Color(1f, .67f, .08f);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 249f, 80f, 42f), $"G {gear}", gearStyle);
            string trip = gameDirector != null ? $"STOP {Mathf.Min(gameDirector.CurrentStop + 1, gameDirector.StopCount)}/{gameDirector.StopCount}   PAX {gameDirector.Passengers}   {Mathf.CeilToInt(gameDirector.TimeRemaining / 60f)} MIN" : "ON ROUTE";
            GUI.Label(new Rect(panel.x + 18f, panel.y + 292f, 210f, 22f), trip, unitStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 310f, 350f, 18f), "C CAMERA   L LIGHTS   V WIPERS   MAX 180", unitStyle);
        }

        private (BezierSpline route, float t) FindNearestRoute()
        {
            BezierSpline best = routes[0];
            float bestT = 0f;
            float bestDistance = float.MaxValue;
            foreach (BezierSpline route in routes)
            {
                for (int i = 0; i <= 80; i++)
                {
                    float t = i / 80f;
                    float distance = (route.GetPoint(t) - vehicle.transform.position).sqrMagnitude;
                    if (distance >= bestDistance) continue;
                    best = route;
                    bestT = t;
                    bestDistance = distance;
                }
            }
            return (best, bestT);
        }

        private void DrawRouteMap(Rect map)
        {
            Vector2 center = map.center;
            Vector2 previous = default;
            bool hasPrevious = false;
            if (routePoints != null && routePoints.Length >= 2)
            {
                int nearest = FindNearestPointIndex();
                for (int i = Mathf.Max(0, nearest - 1); i < routePoints.Length; i++)
                {
                    Vector3 local = vehicle.transform.InverseTransformPoint(routePoints[i].position);
                    Vector2 point = center + new Vector2(local.x, -local.z) * .32f;
                    if (hasPrevious && map.Contains(point) && map.Contains(previous)) DrawLine(previous, point, new Color(1f, .66f, .05f), 5f);
                    previous = point;
                    hasPrevious = true;
                }
                if (gameDirector != null)
                {
                    Vector3 targetLocal = vehicle.transform.InverseTransformPoint(gameDirector.CurrentTarget);
                    Vector2 marker = center + new Vector2(targetLocal.x, -targetLocal.z) * .32f;
                    if (map.Contains(marker))
                    {
                        GUI.color = new Color(1f, .24f, .05f);
                        GUI.DrawTexture(new Rect(marker.x - 7f, marker.y - 7f, 14f, 14f), pixel);
                        GUI.color = Color.white;
                    }
                }
            }
            else foreach (BezierSpline route in routes.OrderBy(route => route.name))
            {
                for (int i = 0; i <= 50; i++)
                {
                    Vector3 local = vehicle.transform.InverseTransformPoint(route.GetPoint(i / 50f));
                    Vector2 point = center + new Vector2(local.x, -local.z) * 0.32f;
                    if (hasPrevious && map.Contains(point) && map.Contains(previous)) DrawLine(previous, point, new Color(1f, .66f, .05f), 5f);
                    previous = point;
                    hasPrevious = true;
                }
                hasPrevious = false;
            }
            DrawLine(center + new Vector2(0f, 9f), center, Color.white, 7f);
            DrawLine(center, center + new Vector2(-6f, 8f), Color.white, 5f);
            DrawLine(center, center + new Vector2(6f, 8f), Color.white, 5f);
        }

        private int FindNearestPointIndex()
        {
            int best = 0;
            float distance = float.MaxValue;
            for (int i = 0; i < routePoints.Length; i++)
            {
                float candidate = (routePoints[i].position - vehicle.transform.position).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                best = i;
            }
            return Mathf.Min(best + 1, routePoints.Length - 1);
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 matrix = GUI.matrix;
            GUI.color = color;
            float angle = Vector3.Angle(b - a, Vector2.right);
            if (a.y > b.y) angle = -angle;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width * .5f, Vector2.Distance(a, b), width), pixel);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        private void DrawSpeedArc(Vector2 center, float radius, float speed01)
        {
            for (int i = 0; i <= 12; i++)
            {
                float angle = Mathf.Lerp(140f, 400f, i / 12f) * Mathf.Deg2Rad;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                DrawLine(center + direction * (radius - 7f), center + direction * radius, i / 12f <= speed01 ? new Color(1f, .55f, .05f) : new Color(.3f, .34f, .37f), 3f);
            }
            float needleAngle = Mathf.Lerp(140f, 400f, speed01) * Mathf.Deg2Rad;
            Vector2 needle = new(Mathf.Cos(needleAngle), Mathf.Sin(needleAngle));
            DrawLine(center, center + needle * (radius - 11f), new Color(1f, .25f, .06f), 3f);
        }
    }
}
