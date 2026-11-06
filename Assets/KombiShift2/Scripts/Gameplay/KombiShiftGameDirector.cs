using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KombiShift2.Gameplay
{
    public sealed class KombiShiftGameDirector : MonoBehaviour
    {
        [SerializeField] private RCCP_CarController vehicle;
        [SerializeField] private Transform[] stops;
        [SerializeField] private float stopRadius = 10f;
        [SerializeField] private float shiftSeconds = 600f;
        private int stopIndex;
        private int passengers;
        private int earnings;
        private float timeRemaining;
        private bool finished;
        private bool paused;
        private GUIStyle title;
        private GUIStyle body;

        public int CurrentStop => stopIndex;
        public int StopCount => stops?.Length ?? 0;
        public int Passengers => passengers;
        public int Earnings => earnings;
        public float TimeRemaining => timeRemaining;
        public Vector3 CurrentTarget => StopCount == 0 ? transform.position : stops[Mathf.Min(stopIndex, StopCount - 1)].position;

        public void Configure(RCCP_CarController player, Transform[] routeStops)
        {
            vehicle = player;
            stops = routeStops;
        }

        private void Start() => timeRemaining = shiftSeconds;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !finished)
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
            if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if (paused || finished || vehicle == null || StopCount == 0) return;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f) Finish(false);

            Transform target = stops[stopIndex];
            float distance = Vector3.Distance(vehicle.transform.position, target.position);
            if (distance <= stopRadius && vehicle.absoluteSpeed < 8f)
            {
                bool pickup = stopIndex < StopCount - 1;
                if (pickup)
                {
                    int boarding = stopIndex == 0 ? 6 : 3;
                    passengers += boarding;
                    earnings += boarding * 2;
                }
                target.gameObject.SetActive(false);
                stopIndex++;
                if (stopIndex >= StopCount) Finish(true);
            }
        }

        private void Finish(bool success)
        {
            finished = true;
            if (success) earnings += Mathf.RoundToInt(timeRemaining * .08f);
            RCCP.SetControl(vehicle, false);
        }

        private void OnGUI()
        {
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            body ??= new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title.normal.textColor = body.normal.textColor = Color.white;
            if (paused)
            {
                DrawPanel("SHIFT PAUSED", "Escape — resume\nR — restart shift\nC — change camera\nL — lights   V — wipers", false);
            }
            else if (finished)
            {
                DrawPanel(timeRemaining > 0f ? "SHIFT COMPLETE" : "SHIFT FAILED",
                    $"Passengers carried: {passengers}\nEarnings: ${earnings}\nTime remaining: {Mathf.CeilToInt(timeRemaining)} sec\n\nR — drive again   Esc — pause", true);
            }
            else
            {
                GUIStyle objective = new(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
                objective.normal.textColor = Color.white;
                string name = stopIndex == StopCount - 1 ? "MBARE DROP-OFF" : stopIndex == 0 ? "CBD PICKUP" : $"ROADSIDE PICKUP {stopIndex + 1}";
                GUI.Box(new Rect(18, 18, 360, 74), GUIContent.none);
                GUI.Label(new Rect(32, 25, 330, 26), name, objective);
                GUI.Label(new Rect(32, 51, 330, 25), $"Stop below 8 km/h  •  {passengers} passengers  •  ${earnings}");
            }
        }

        private void DrawPanel(string heading, string message, bool showMenu)
        {
            Rect panel = new(Screen.width * .5f - 230f, Screen.height * .5f - 150f, 460f, 300f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20, panel.y + 25, panel.width - 40, 45), heading, title);
            GUI.Label(new Rect(panel.x + 30, panel.y + 80, panel.width - 60, 150), message, body);
            if (showMenu && GUI.Button(new Rect(panel.x + 130, panel.y + 245, 200, 38), "MAIN MENU"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("KombiMainMenu");
            }
        }
    }
}
