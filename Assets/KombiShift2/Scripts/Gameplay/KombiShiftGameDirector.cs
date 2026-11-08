using System;
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
        public event Action<int> OnFareEarned;
        public event Action<bool> OnPauseChanged;
        public event Action<bool> OnShiftFinished;
        public KombiStage Stage { get; private set; }
        public bool Success => finished && success;
        public int MinimumPassengers => KombiChallenge.MinimumPassengers(Stage);
        public float Duration => shiftSeconds;
        public void SetPaused(bool value)
        {
            if (finished || paused == value) return;
            paused = value; Time.timeScale = paused ? 0f : 1f;
            OnPauseChanged?.Invoke(paused);
        }
        public void ReturnToMenu() { Time.timeScale = 1f; SceneManager.LoadScene("KombiMainMenu"); }
        public void RestartShift() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        public void NextStage() { KombiChallenge.Selected = KombiStage.RushHour; RestartShift(); }
        public RCCP_CarController Vehicle => vehicle;
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
            Time.timeScale = 1f;
            Stage = KombiChallenge.Selected;
            shiftSeconds = KombiChallenge.Seconds(Stage);
            timeRemaining = shiftSeconds;
            cabin = vehicle != null ? vehicle.GetComponent<KombiPassengerCabin>() : null;
            stations = stops == null ? Array.Empty<PassengerStation>() : stops.Select(x => x != null ? x.GetComponent<PassengerStation>() : null).ToArray();
            rivals = FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None);
            for (int i = 0; i < rivals.Length; i++)
                rivals[i].SetChallenge(KombiChallenge.RivalSpeed(Stage), KombiChallenge.RivalDelay(Stage) + i * 6f);
            notice = $"{KombiChallenge.Name(Stage)} — deliver {MinimumPassengers}+ passengers to Mbare.";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !finished) SetPaused(!paused);
            if (Input.GetKeyDown(KeyCode.R)) RestartShift();
            if (paused || finished || vehicle == null || StopCount == 0) return;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f) { Finish(false); return; }
            if (servicing) return;
            // A full vehicle must deliver instead of waiting forever at another pickup.
            if (cabin != null && onboard.Count >= cabin.Seats.Length) stopIndex = StopCount - 1;
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
                person => { onboard.Add(person); passengers++; earnings += 2; OnFareEarned?.Invoke(2); },
                () => !finished && vehicle != null && vehicle.absoluteSpeed < 2f && Vector3.Distance(vehicle.transform.position, station.ServicePoint.position) < stopRadius + 2f);
            servicing = false;
            notice = station.HasWaiting ? "Stop fully to finish boarding." : "Passengers aboard. Continue to the next stop.";
        }

        private IEnumerator DropOff(PassengerStation station)
        {
            servicing = true;
            if (station.ArrivalSeats == null || station.ArrivalSeats.Length == 0)
            { Debug.LogError("Terminal has no arrival seats."); servicing = false; Finish(false); yield break; }
            for (int i = 0; i < onboard.Count; i++)
            {
                var person = onboard[i];
                person.transform.SetPositionAndRotation(cabin.Doorway.position, vehicle.transform.rotation);
                person.LeaveAt(station.ArrivalSeats[i % station.ArrivalSeats.Length]);
                yield return new WaitForSeconds(.6f);
            }
            // Wait for visible delivery, not only the staggered exit calls.
            while (!finished && onboard.Exists(person => person != null && person.State != PassengerState.Delivered)) yield return null;
            servicing = false;
            if (!finished) Finish(passengers >= MinimumPassengers);
        }

        private void Finish(bool completed)
        {
            if (finished) return;
            success = completed; finished = true;
            if (success)
            {
                earnings += Mathf.RoundToInt(timeRemaining * .08f);
                KombiChallenge.SaveBest(Stage, earnings);
            }
            if (vehicle != null) RCCP.SetControl(vehicle, false);
            foreach (var rival in rivals) if (rival != null) rival.StopForShiftEnd();
            Time.timeScale = 0f;
            OnShiftFinished?.Invoke(success);
        }

        private void OnGUI()
        {
            if (!paused && !finished) return;
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            body ??= new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title.normal.textColor = body.normal.textColor = Color.white;
            float width = Mathf.Min(440f, Screen.width - 24f);
            Rect panel = new(Screen.width * .5f - width * .5f, Screen.height * .5f - 180f, width, 360f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 12, panel.y + 22, width - 24, 42), paused ? "SHIFT PAUSED" : success ? "SHIFT COMPLETE" : "SHIFT FAILED", title);
            string message = paused ? $"{KombiChallenge.Name(Stage)}\nDeliver {MinimumPassengers}+ passengers to Mbare.\nEscape — resume    R — restart" : $"{KombiChallenge.Name(Stage)}\nPassengers delivered: {onboard.Count(x => x != null && x.State == PassengerState.Delivered)} / target {MinimumPassengers}\nRivals collected: {RivalPassengers}\nEarnings: ${earnings}   Best: ${KombiChallenge.Best(Stage)}\n{(success ? "Target reached." : "Time expired or delivery target missed.")}";
            GUI.Label(new Rect(panel.x + 20, panel.y + 78, width - 40, 155), message, body);
            if (paused)
            {
                GUI.Label(new Rect(panel.x + 20, panel.y + 207, width - 40, 24), "MASTER VOLUME", body);
                AudioListener.volume = GUI.HorizontalSlider(new Rect(panel.x + 30, panel.y + 238, width - 60, 24), AudioListener.volume, 0f, 1f);
                PlayerPrefs.SetFloat("KombiShift.MasterVolume", AudioListener.volume);
            }
            if (GUI.Button(new Rect(panel.x + 16, panel.y + 278, width / 2 - 24, 32), paused ? "RESUME" : "REPLAY"))
            { if (paused) SetPaused(false); else RestartShift(); }
            if (GUI.Button(new Rect(panel.center.x + 8, panel.y + 278, width / 2 - 24, 32), "MAIN MENU")) ReturnToMenu();
            if (success && Stage == KombiStage.Morning && GUI.Button(new Rect(panel.x + 16, panel.y + 318, width - 32, 30), "NEXT: RUSH HOUR")) NextStage();
        }
    }
}
