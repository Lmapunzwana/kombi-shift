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
        [SerializeField] private List<PassengerTrip> trips = new();
        [SerializeField] private float requestRadius = 90f, missedStopPenalty = 20f;
        [SerializeField] private int missedStopXP = 10;
        private int delivered;
        private float nextTripSample;
        private AudioSource serviceAudio;
        private AudioClip requestTone;
        public IReadOnlyList<PassengerTrip> Trips => trips;
        public KombiServiceQuality Quality { get; private set; }
        public KombiVehicleCondition Condition { get; private set; }
        public KombiTrafficLaw Law { get; private set; }
        public float Happiness => trips.Where(x => x.state != TripState.Completed).Select(x => x.happiness).DefaultIfEmpty(100f).Average();
        public int Delivered => delivered;
        public string DropRequest => trips.Any(x => x.requested && x.state != TripState.Completed) ? "STOP REQUESTED — " + stations[trips.First(x => x.requested && x.state != TripState.Completed).destination].name : "";
        public bool AtServicePoint => stations != null && stations.Any(x => x != null && Vector3.Distance(vehicle.transform.position, x.ServicePoint.position) < stopRadius);
        public bool TrySpend(int amount) { if (amount < 0 || earnings < amount) return false; earnings -= amount; return true; }
        public void DeductFine(int amount) => earnings -= Mathf.Max(0, amount);
        public event Action<PassengerTrip> TripChanged;
        public void Announce(string message) { notice = message; }

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
        public int Passengers => onboard.Count;
        public int Earnings => earnings;
        public float TimeRemaining => timeRemaining;
        public bool Paused => paused;
        public bool Finished => finished;
        public bool Servicing => servicing;
        public PassengerStation[] Stations => stations;
        public RivalKombiAI[] Rivals => rivals;
        public int RivalPassengers => rivals == null ? 0 : rivals.Sum(x => x != null ? x.PassengersTaken : 0);
        public string Notice => servicing ? (trips.Any(x => x.state == TripState.Alighting) ? "ALIGHTING — KEEP THE KOMBI STOPPED" : "BOARDING — KEEP THE KOMBI STOPPED") : notice;
        public Vector3 CurrentTarget {
            get {
                var request = trips.Where(x => x.requested && x.state != TripState.Completed).OrderBy(x => Vector3.SqrMagnitude(stations[x.destination].ServicePoint.position - vehicle.transform.position)).FirstOrDefault();
                return request != null ? stations[request.destination].ServicePoint.position : StopCount == 0 ? transform.position : stops[Mathf.Min(stopIndex, StopCount - 1)].position;
            }
        }
        public void Configure(RCCP_CarController player, Transform[] routeStops) { vehicle = player; stops = routeStops; }

        private void Start()
        {
            Time.timeScale = 1f;
            Stage = KombiChallenge.Selected;
            shiftSeconds = KombiChallenge.Seconds(Stage);
            timeRemaining = shiftSeconds;
            var stageSettings = KombiStageSettings.Load(Stage);
            var traffic = FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None);
            Array.Sort(traffic, (a,b) => string.CompareOrdinal(a.name,b.name));
            float fraction = stageSettings != null ? stageSettings.trafficFraction : Stage == KombiStage.Morning ? .6f : 1f;
            for (int i = Mathf.CeilToInt(traffic.Length * fraction); i < traffic.Length; i++) traffic[i].gameObject.SetActive(false);
            cabin = vehicle != null ? vehicle.GetComponent<KombiPassengerCabin>() : null;
            stations = stops == null ? Array.Empty<PassengerStation>() : stops.Select(x => x != null ? x.GetComponent<PassengerStation>() : null).ToArray();
            rivals = FindObjectsByType<RivalKombiAI>(FindObjectsSortMode.None);
            for (int i = 0; i < rivals.Length; i++)
            {
                rivals[i].SetPersonality(Stage == KombiStage.RushHour ? (i % 2 == 0 ? RivalPersonality.Aggressive : RivalPersonality.Competitive) : (i % 2 == 0 ? RivalPersonality.Conservative : RivalPersonality.Competitive), i % 2 == 0 ? new Color(.95f,.35f,.22f) : new Color(.35f,.6f,1f));
                rivals[i].ActivityChanged += (rival, activity) => Announce(rival.name + ": " + activity);
                rivals[i].SetChallenge(KombiChallenge.RivalSpeed(Stage), KombiChallenge.RivalDelay(Stage) + i * 6f);
            }
            Quality = vehicle.gameObject.AddComponent<KombiServiceQuality>(); Quality.Configure(this);
            Condition = vehicle.gameObject.AddComponent<KombiVehicleCondition>(); Condition.Configure(this);
            Law = vehicle.gameObject.AddComponent<KombiTrafficLaw>(); Law.Configure(this);
            serviceAudio = gameObject.AddComponent<AudioSource>(); serviceAudio.spatialBlend = 0f;
            requestTone = AudioClip.Create("Passenger request bell", 11025, 1, 22050, false);
            var samples = new float[11025];
            for (int n = 0; n < samples.Length; n++) samples[n] = Mathf.Sin(n * 2f * Mathf.PI * 660f / 22050f) * .12f * (1f - n / (float)samples.Length);
            requestTone.SetData(samples, 0);
            Quality.Changed += (message, xp) => { notice = message + $"  XP {xp:+0;-0;0}"; serviceAudio.PlayOneShot(requestTone, .45f); };
            notice = $"{KombiChallenge.Name(Stage)} — deliver {MinimumPassengers}+ passengers to Mbare.";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !finished) SetPaused(!paused);
            if (Input.GetKeyDown(KeyCode.R)) RestartShift();
            if (paused || finished || vehicle == null || StopCount == 0) return;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            if (timeRemaining <= 0f) { Finish(false); return; }
            if (Input.GetKeyDown(KeyCode.T)) notice = Condition.Repair() ? "Vehicle repaired." : $"Repair at a stop, below 2 km/h: ${Condition.RepairPrice}";
            if (Time.time >= nextTripSample) { SampleTrips(); nextTripSample = Time.time + .5f; }
            if (servicing) return;
            // Drop-off service is independent of pickup progression and reservations.
            foreach (var drop in stations)
            {
                if (drop == null || drop.Terminal || !trips.Any(x => x.state != TripState.Completed && (stations[x.destination] == drop || x.missed))) continue;
                if (vehicle.absoluteSpeed < 2f && Vector3.Distance(vehicle.transform.position, drop.ServicePoint.position) < stopRadius)
                { StartCoroutine(AlightAt(drop, false)); return; }
            }
            // A full vehicle must deliver instead of waiting forever at another pickup.
            if (cabin != null && onboard.Count >= cabin.Seats.Length) stopIndex = StopCount - 1;
            while (stopIndex < StopCount - 1 && stations[stopIndex] != null && !stations[stopIndex].HasWaiting && !stations[stopIndex].Reserved && !trips.Any(x => x.destination == stopIndex && x.state != TripState.Completed))
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
                person => {
                    onboard.Add(person); passengers++;
                    int origin = Array.IndexOf(stations, station);
                    // Alternate intermediate and terminal trips, always downstream.
                    int destination = passengers % 2 == 0 ? StopCount - 1 : Mathf.Min(origin + 1, StopCount - 1);
                    while (destination < StopCount - 1 && stations[destination] == null) destination++;
                    var trip = new PassengerTrip { fare = KombiStageSettings.Load(Stage) != null ? KombiStageSettings.Load(Stage).fare : 2, passenger = person, boardingStation = origin, destination = destination, boardedAt = Time.time };
                    trips.Add(trip); TripChanged?.Invoke(trip);
                },
                () => !finished && vehicle != null && vehicle.absoluteSpeed < 2f && Vector3.Distance(vehicle.transform.position, station.ServicePoint.position) < stopRadius + 2f);
            servicing = false;
            notice = station.HasWaiting ? "Stop fully to finish boarding." : "Passengers aboard. Continue to the next stop.";
        }

        private void SampleTrips()
        {
            foreach (var trip in trips)
            {
                if (trip.state == TripState.Completed || trip.state == TripState.Alighting) continue;
                float distance = Vector3.Distance(vehicle.transform.position, stations[trip.destination].ServicePoint.position);
                if (trip.Request(distance, requestRadius))
                { notice = "Passenger: Driver, next stop! " + stations[trip.destination].name; serviceAudio.PlayOneShot(requestTone); TripChanged?.Invoke(trip); }
                if (trip.CheckMissed(distance, requestRadius))
                { Quality.Apply("missed:" + trip.passenger.GetEntityId(), "Passenger: You missed my stop! Stop at a nearby station.", 0f, -missedStopXP, 0f); trip.Adjust(20f - missedStopPenalty); TripChanged?.Invoke(trip); }
                if (Time.time - trip.boardedAt > trip.patienceSeconds)
                    Quality.Apply("waiting", "Passenger: We have been waiting too long.", -3f, -1, 30f);
            }
        }

        private IEnumerator DropOff(PassengerStation station)
        {
            yield return AlightAt(station, true);
            if (!finished && onboard.Count == 0) Finish(delivered >= MinimumPassengers);
        }

        private IEnumerator AlightAt(PassengerStation station, bool terminal)
        {
            servicing = true;
            var leaving = trips.Where(x => x.state != TripState.Completed && (terminal || stations[x.destination] == station || x.missed)).ToArray();
            foreach (var trip in leaving)
            {
                if (finished || vehicle.absoluteSpeed >= 2f) break;
                var person = trip.passenger;
                // Use the imported alighting animation and real destination seats.
                Transform arrival = null;
                if (station.ArrivalSeats != null && station.ArrivalSeats.Length > 0)
                    arrival = station.ArrivalSeats[delivered % station.ArrivalSeats.Length];
                if (arrival == null)
                {
                    var point = new GameObject("Passenger drop-off " + delivered).transform;
                    point.SetParent(station.transform); point.position = station.ServicePoint.position + station.ServicePoint.right * -4f + station.ServicePoint.forward * (delivered % 5);
                    arrival = point;
                }
                person.transform.SetPositionAndRotation(cabin.Doorway.position, vehicle.transform.rotation);
                trip.state = TripState.Alighting; person.LeaveAt(arrival);
                while (!finished && person != null && person.State != PassengerState.Delivered) yield return null;
                if (finished) break;
                trip.state = TripState.Completed; trip.Adjust(trip.missed ? 0f : 5f);
                onboard.Remove(person); delivered++; earnings += trip.fare; OnFareEarned?.Invoke(trip.fare);
                Quality.Apply("delivery:" + person.GetEntityId(), trip.missed ? "Passenger delivered at a recovery stop." : "Passenger: Thank you for the safe arrival!", 0f, trip.missed ? 2 : 10, 0f);
                TripChanged?.Invoke(trip);
            }
            servicing = false;
            notice = "Drop-off complete. Continue the shift. T repairs at stops.";
        }

        private void OnDestroy() { if (requestTone != null) Destroy(requestTone); }

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
            string message = paused ? $"{KombiChallenge.Name(Stage)}\nDeliver {MinimumPassengers}+ passengers to Mbare.\nEscape — resume    R — restart" : $"{KombiChallenge.Name(Stage)}\nPassengers delivered: {delivered} / target {MinimumPassengers}\nRivals collected: {RivalPassengers}\nEarnings: ${earnings}   Best: ${KombiChallenge.Best(Stage)}\n{(success ? "Target reached." : "Time expired or delivery target missed.")}";
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
