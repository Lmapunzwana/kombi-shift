using System.Linq;
using BezierSolution;
using UnityEngine;

namespace KombiShift2.UI
{
    public sealed class KombiNavigationHud : MonoBehaviour
    {
        [SerializeField] private RCCP_CarController vehicle;
        private BezierSpline[] routes;
        private Texture2D pixel;

        public void SetVehicle(RCCP_CarController value) => vehicle = value;

        private void Awake()
        {
            routes = FindObjectsByType<BezierSpline>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private void OnGUI()
        {
            if (vehicle == null || routes == null || routes.Length == 0) return;
            Rect panel = new(Screen.width - 330f, Screen.height - 300f, 310f, 280f);
            GUI.color = new Color(0.035f, 0.045f, 0.055f, 0.94f);
            GUI.DrawTexture(panel, pixel);
            GUI.color = Color.white;

            (BezierSpline route, float t) = FindNearestRoute();
            float targetT = Mathf.Min(1f, t + 0.055f);
            Vector3 target = route.GetPoint(targetT);
            Vector3 localTarget = vehicle.transform.InverseTransformPoint(target);
            float turnAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            string instruction = Mathf.Abs(turnAngle) < 18f ? "CONTINUE" : turnAngle < 0f ? "TURN LEFT" : "TURN RIGHT";
            float remaining = Mathf.Max(0f, (1f - t) * route.length);

            GUIStyle instructionStyle = new(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            instructionStyle.normal.textColor = new Color(1f, 0.78f, 0.12f);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 190f, 32f), instruction, instructionStyle);
            GUI.Label(new Rect(panel.x + 210f, panel.y + 13f, 82f, 28f), $"{remaining:0} m");

            Rect map = new(panel.x + 15f, panel.y + 50f, 280f, 155f);
            GUI.color = new Color(0.10f, 0.12f, 0.14f);
            GUI.DrawTexture(map, pixel);
            GUI.color = Color.white;
            DrawRouteMap(map);

            GUIStyle speedStyle = new(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            speedStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(panel.x + 105f, panel.y + 210f, 120f, 52f), $"{vehicle.absoluteSpeed:0}", speedStyle);
            GUIStyle unitStyle = new(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.LowerLeft };
            unitStyle.normal.textColor = new Color(.75f, .8f, .82f);
            GUI.Label(new Rect(panel.x + 230f, panel.y + 222f, 62f, 30f), "km/h", unitStyle);
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
            foreach (BezierSpline route in routes.OrderBy(route => route.name))
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
    }
}
