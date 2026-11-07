using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KombiShift2.Traffic;
using KombiShift2.Vehicle;
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
        private bool success;
        private bool paused;
        private bool servicing;
        private string notice;
        private PassengerStation[] stations;
        private RivalKombiAI[] rivals;
        private KombiPassengerCabin cabin;
        private readonly List<StationPassenger> onboard = new();
        private GUIStyle title, body;
        public int CurrentStop => stopIndex;
        public int StopCount => stops?.Length ?? 0;
        public int Passengers => passengers;
        public int Earnings => earnings;
        public float TimeRemaining => timeRemaining;
        public bool Paused => paused;
        public bool Finished => finished;
        public bool Servicing => servicing;
        public PassengerStation[] Stations => stations;
        public RivalKombiAI[] Rivals => rivals;
        public int RivalPassengers => rivals == null ? 0 : rivals.Sum(x => x != null ? x.PassengersTaken : 0);
        public string Notice => servicing ? "BOARDING — KEEP THE KOMBI STOPPED" : notice;
        public Vector3 CurrentTarget => StopCount == 0 ? transform.position : stops[Mathf.Min(stopIndex, StopCount - 1)].position;
        public void Configure(RCCP_CarController player, Transform[] routeStops) { vehicle = player; stops = routeStops; }

        private void Start()
        {
            Time.timeScale = 1f; timeRemaining = shiftSeconds;
            cabin = vehicle != null ? vehicle.GetComponent<KombiPassengerCabin>() : null;
            stations = stops.Select(x => x.GetComponent<PassengerStation>()).ToArray();
            rivals = FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None);
            notice = "Reach the stop before the rival kombis.";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !finished) { paused = !paused; Time.timeScale = paused ? 0f : 1f; }
            if (Input.GetKeyDown(KeyCode.R)) { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
            if (paused || finished || vehicle == null || StopCount == 0) return;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f) { Finish(false); return; }
            if (servicing) return;
            while (stopIndex < StopCount - 1 && stations[stopIndex] != null && !stations[stopIndex].HasWaiting && !stations[stopIndex].Reserved)
            {
                stopIndex++;
                notice = "Next stop — rivals can take waiting passengers.";
            }
            var station = stations[stopIndex];
            if (station == null || cabin == null) return;
            Vector3 distance = vehicle.transform.position - station.ServicePoint.position; distance.y = 0f;
            if (distance.magnitude > stopRadius || vehicle.absoluteSpeed >= 2f) return;
            if (station.Terminal) { StartCoroutine(DropOff(station)); return; }
            if (station.Reserved) { notice = "A rival reached this stop first."; return; }
            if (onboard.Count < cabin.Seats.Length && station.HasWaiting) StartCoroutine(Board(station));
        }

        private IEnumerator Board(PassengerStation station)
        {
            servicing = true;
            yield return station.Board(vehicle.transform, cabin.Doorway, cabin.Seats, onboard.Count,
                person => { onboard.Add(person); passengers++; earnings += 2; },
                () => !finished && vehicle != null && vehicle.absoluteSpeed < 2f && Vector3.Distance(vehicle.transform.position, station.ServicePoint.position) < stopRadius + 2f);
            servicing = false;
            notice = station.HasWaiting ? "Stop fully to finish boarding." : "Passengers aboard. Continue to the next stop.";
        }

        private IEnumerator DropOff(PassengerStation station)
        {
            servicing = true;
            for (int i = 0; i < onboard.Count; i++)
            {
                var person = onboard[i];
                person.transform.SetPositionAndRotation(cabin.Doorway.position, vehicle.transform.rotation);
                person.LeaveAt(station.ArrivalSeats[i % station.ArrivalSeats.Length]);
                yield return new WaitForSeconds(.6f);
            }
            servicing = false;
            Finish(passengers > 0);
        }

        private void Finish(bool completed)
        {
            success = completed; finished = true;
            if (success) earnings += Mathf.RoundToInt(timeRemaining * .08f);
            if (vehicle != null) RCCP.SetControl(vehicle, false);
        }

        private void OnGUI()
        {
            if (!paused && !finished) return;
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            body ??= new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title.normal.textColor = body.normal.textColor = Color.white;
            float width = Mathf.Min(440f, Screen.width - 24f);
            Rect panel = new(Screen.width * .5f - width * .5f, Screen.height * .5f - 150f, width, 300f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 12, panel.y + 22, width - 24, 42), paused ? "SHIFT PAUSED" : success ? "SHIFT COMPLETE" : "SHIFT FAILED", title);
            string message = paused ? "Escape — resume\nR — restart\nC — camera   L — lights   V — wipers" : $"Passengers carried: {passengers}\nRivals collected: {RivalPassengers}\nEarnings: ${earnings}\n{(passengers == 0 ? "No passengers collected." : "")}\nR — drive again";
            GUI.Label(new Rect(panel.x + 20, panel.y + 78, width - 40, 155), message, body);
            if (finished && GUI.Button(new Rect(panel.center.x - 100, panel.y + 245, 200, 36), "MAIN MENU"))
            { Time.timeScale = 1f; SceneManager.LoadScene("KombiMainMenu"); }
        }
    }
}
