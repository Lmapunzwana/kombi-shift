using System;
using KombiShift2.City;
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
        private float earnings;
        public float GrossEarnings {get;private set;}
        public int RepairExpenses {get;private set;}
        private float timeRemaining;
        private bool finished;
        private bool success;
        private bool crashed;
        private bool paused;
        private bool servicing;
        private string notice;
        private PassengerStation[] stations;
        private RivalKombiAI[] rivals;
        private KombiPassengerCabin cabin;
        private readonly List<StationPassenger> onboard = new();
        private GUIStyle title, body, resultText, mapLabel;
        private Transform[] routePath;
        private readonly List<Vector3> driven = new();
        private float nextTrailSample, playerFlipped, playerStuck;
        private Texture2D mapPixel;
        public void SetRoutePath(Transform[] points) { routePath = points; }
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
        public bool TrySpend(int amount) { if (amount < 0 || earnings < amount) return false; earnings -= amount; RepairExpenses += amount; return true; }
        public void DeductFine(int amount) => earnings -= Mathf.Max(0, amount);
        public event Action<PassengerTrip> TripChanged;
        public void Announce(string message) { notice = message; }

        public event Action<float> OnFareEarned;
        public event Action<bool> OnPauseChanged;
        public event Action<bool> OnShiftFinished;
        public KombiStage Stage { get; private set; }
        public bool Success => finished && success;
        public int MinimumPassengers => CityTripSession.Active ? CityTripSelection.DeliveryTarget : KombiChallenge.MinimumPassengers(Stage);
        public float Duration => shiftSeconds;
        public float StoppingRadius => stopRadius;
        public void SetPaused(bool value)
        {
            if (finished || paused == value) return;
            paused = value; Time.timeScale = paused ? 0f : 1f;
            OnPauseChanged?.Invoke(paused);
        }
        public void Crash()
        {
            if (finished) return;
            crashed = true;
            Finish(false);
        }
        public void ReturnToMenu() { Time.timeScale = 1f; SceneManager.LoadScene("KombiMainMenu"); }
        public void RestartShift() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        // Next: back to the route map so the player chooses the next trip (Morning success moves on to Rush Hour).
        public void NextStage() { if (success && Stage == KombiStage.Morning) KombiChallenge.Selected = KombiStage.RushHour; KombiShift2.UI.KombiMainMenu.OpenMarketOnLoad = true; ReturnToMenu(); }
        public static float LoadRequestedAt = -1f;
        public RCCP_CarController Vehicle => vehicle;
        public int CurrentStop => stopIndex;
        public int StopCount => stops?.Length ?? 0;
        public int Passengers => onboard.Count;
        public float Earnings => earnings;
        public float TimeRemaining => timeRemaining;
        public bool Paused => paused;
        public bool Finished => finished;
        public bool Servicing => servicing;
        public bool Alighting => servicing && trips.Any(t=>t.state==TripState.Alighting);
        public PassengerStation[] Stations => stations;
        public RivalKombiAI[] Rivals => rivals;
        public bool RaceStarted { get; private set; }
        public int RivalsFinished => rivals == null ? 0 : rivals.Count(x => x != null && x.FinishedTrip);
        public int RivalPassengers =>rivals == null ? 0 : rivals.Sum(x => x != null ? x.PassengersTaken : 0);
        public string Notice => servicing ? (trips.Any(x => x.state == TripState.Alighting) ? "ALIGHTING — KEEP THE KOMBI STOPPED" : "BOARDING — KEEP THE KOMBI STOPPED") : notice;
        public Vector3 CurrentTarget {
            get {
                var request = trips.Where(x => x.requested && x.state != TripState.Completed).OrderBy(x => Vector3.SqrMagnitude(stations[x.destination].ServicePoint.position - vehicle.transform.position)).FirstOrDefault();
                if (request != null) return stations[request.destination].ServicePoint.position;
                if (StopCount == 0) return transform.position;
                int index = Mathf.Min(stopIndex, StopCount - 1);
                return stations != null && index < stations.Length && stations[index] != null
                    ? stations[index].ServicePoint.position : stops[index].position;
            }
        }
        public void Configure(RCCP_CarController player, Transform[] routeStops, float serviceRadius = -1f) { vehicle = player; stops = routeStops; if(serviceRadius>0){stopRadius=serviceRadius;requestRadius=250f;} }
        float FareForTrip(int origin,int destination)
        {
            if(!CityTripSession.Active)return KombiStageSettings.Load(Stage)!=null?KombiStageSettings.Load(Stage).fare:2;
            var route=CityTripSelection.Selected;var graph=CityRoadNetwork.Active;
            float distance=graph.Length(graph.FindPath(route.stopNodes[origin],route.stopNodes[destination]));
            return route.Fare(distance,Stage);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Stage = KombiChallenge.Selected;
            int treesRemoved = CityPresentationCleanup.Apply(CityRoadNetwork.Active);
            if (LoadRequestedAt >= 0f) { Debug.Log($"KOMBI_LOAD shift ready in {Time.realtimeSinceStartup - LoadRequestedAt:0.00} s (road trees hidden: {treesRemoved})"); LoadRequestedAt = -1f; }
            shiftSeconds = CityTripSession.Active ? CityTripSession.SelectedDuration : KombiChallenge.Seconds(Stage);
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
                rivals[i].SetPersonality(Stage==KombiStage.Morning ? (i%2==0?RivalPersonality.Aggressive:RivalPersonality.Competitive) : RivalPersonality.Aggressive, i % 2 == 0 ? new Color(.95f,.35f,.22f) : new Color(.35f,.6f,1f));
                rivals[i].ActivityChanged += (rival, activity) => Announce(rival.name + ": " + activity);
                rivals[i].SetChallenge(KombiChallenge.RivalSpeed(Stage), KombiChallenge.RivalDelay(Stage));
            }
            Quality = vehicle.GetComponent<KombiServiceQuality>() ?? vehicle.gameObject.AddComponent<KombiServiceQuality>(); Quality.Configure(this);
            Condition = vehicle.GetComponent<KombiVehicleCondition>() ?? vehicle.gameObject.AddComponent<KombiVehicleCondition>(); Condition.Configure(this);
            Law = vehicle.GetComponent<KombiTrafficLaw>() ?? vehicle.gameObject.AddComponent<KombiTrafficLaw>(); Law.Configure(this);
            serviceAudio = gameObject.AddComponent<AudioSource>(); serviceAudio.spatialBlend = 0f;
            requestTone = AudioClip.Create("Passenger request bell", 11025, 1, 22050, false);
            var samples = new float[11025];
            for (int n = 0; n < samples.Length; n++) samples[n] = Mathf.Sin(n * 2f * Mathf.PI * 660f / 22050f) * .12f * (1f - n / (float)samples.Length);
            requestTone.SetData(samples, 0);
            Quality.Changed += (message, xp) => { notice = message + $"  XP {xp:+0;-0;0}"; serviceAudio.PlayOneShot(requestTone, .45f); };
            notice = $"{KombiChallenge.Name(Stage)} — deliver {MinimumPassengers}+ passengers to {(CityTripSession.Active?CityTripSelection.Selected.destination:"Mbare")}. Press W to start: the rivals go when you go.";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !finished) SetPaused(!paused);
            if (Input.GetKeyDown(KeyCode.R)) RestartShift();
            if (paused || finished || vehicle == null || StopCount == 0) return;
            // Like a race start: the clock and the rivals wait for the player's first throttle press.
            if (!RaceStarted)
            {
                bool throttle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                if (!throttle && vehicle.absoluteSpeed < 1.5f && !Application.isBatchMode) return;
                RaceStarted = true;
                notice = "GO! The rivals are racing you to the passengers.";
            }
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            // Recovery: upside down for 2 s, or pressing W for 5 s without moving -> back onto the road.
            playerFlipped = CityRoadRecovery.IsFlipped(vehicle.transform) ? playerFlipped + Time.deltaTime : 0f;
            bool pushing = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            playerStuck = pushing && !servicing && vehicle.absoluteSpeed < 1f ? playerStuck + Time.deltaTime : 0f;
            if ((playerFlipped > 2f || playerStuck > 5f) && CityRoadRecovery.Place(vehicle.GetComponent<Rigidbody>(), vehicle.transform.forward))
            { playerFlipped = playerStuck = 0f; notice = "Kombi recovered back onto the road."; }
            // Record the path actually driven for the end-of-shift route map.
            if (Time.time >= nextTrailSample)
            {
                nextTrailSample = Time.time + .5f;
                Vector3 here = vehicle.transform.position;
                if (driven.Count == 0 || (driven[^1] - here).sqrMagnitude > 9f) driven.Add(here);
            }
            if (timeRemaining <= 0f) { Finish(false); return; }
            if (Input.GetKeyDown(KeyCode.T)) notice = Condition.Repair() ? "Vehicle repaired." : $"Repair at a stop, below 2 km/h: ${Condition.RepairPrice}";
            if (Time.time >= nextTripSample) { SampleTrips(); nextTripSample = Time.time + .5f; }
            if (servicing) return;
            // The selected terminal remains a valid destination even after skipped pickups.
            if(CityTripSession.Active)
            {
                var terminal=stations.FirstOrDefault(s=>s!=null&&s.Terminal);
                if(terminal!=null&&vehicle.absoluteSpeed<2f&&terminal.MatchesApproach(vehicle.transform)&&Vector3.Distance(vehicle.transform.position,terminal.ServicePoint.position)<stopRadius)
                {StartCoroutine(DropOff(terminal));return;}
            }
            // Drop-off service is independent of pickup progression and reservations.
            foreach (var drop in stations)
            {
                if (drop == null || drop.Terminal || !trips.Any(x => x.state != TripState.Completed && (stations[x.destination] == drop || x.missed))) continue;
                if (vehicle.absoluteSpeed < 2f && drop.MatchesApproach(vehicle.transform) && Vector3.Distance(vehicle.transform.position, drop.ServicePoint.position) < stopRadius)
                { StartCoroutine(AlightAt(drop, false)); return; }
            }
            // A full vehicle must deliver instead of waiting forever at another pickup.
            if (cabin != null && onboard.Count >= cabin.Seats.Length)
                stopIndex = CityTripSession.Active ? trips.Where(t=>t.state!=TripState.Completed).Select(t=>t.destination).DefaultIfEmpty(StopCount-1).Min() : StopCount - 1;
            while (stopIndex < StopCount - 1 && stations[stopIndex] != null && !stations[stopIndex].HasWaiting && !stations[stopIndex].Reserved && !trips.Any(x => x.destination == stopIndex && x.state != TripState.Completed))
            {
                stopIndex++;
                notice = "Next stop — rivals can take waiting passengers.";
            }
            var station = stations[stopIndex];
            if (station == null || cabin == null) return;
            Vector3 distance = vehicle.transform.position - station.ServicePoint.position; distance.y = 0f;
            if (distance.magnitude > stopRadius || vehicle.absoluteSpeed >= 2f) return;
            if (!station.MatchesApproach(vehicle.transform)) { notice="Use the left lane and face the stop direction to serve passengers."; return; }
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
                    var trip = new PassengerTrip { fare = FareForTrip(origin,destination), passenger = person, boardingStation = origin, destination = destination, boardedAt = Time.time };
                    trips.Add(trip); TripChanged?.Invoke(trip);
                },
                () => !finished && vehicle != null && station.MatchesApproach(vehicle.transform) && vehicle.absoluteSpeed < 2f && Vector3.Distance(vehicle.transform.position, station.ServicePoint.position) < stopRadius + 2f);
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
                { notice = $"Passenger requests drop-off: {stations[trip.destination].name} — {Mathf.RoundToInt(distance)} m"; serviceAudio.PlayOneShot(requestTone); TripChanged?.Invoke(trip); }
                if (trip.CheckMissed(distance, CityTripSession.Active?45f:requestRadius))
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
            var leaving = trips.Where(x => x.state != TripState.Completed && (terminal || stations[x.destination] == station || x.missed)).OrderBy(x=>Vector3.Distance(x.passenger.transform.position,cabin.Doorway.position)).ToArray();
            int departure=0;
            foreach(var trip in leaving)
            {
                if(finished || vehicle.absoluteSpeed>=2f)break;
                var arrival=station.ClaimArrival(trip.passenger,out bool seatedAtDestination);
                trip.state=TripState.Alighting;
                trip.passenger.LeaveAt(arrival,seatedAtDestination,cabin.Doorway,departure++*.55f);
            }
            foreach (var trip in leaving)
            {
                if(trip.state!=TripState.Alighting)continue;
                var person = trip.passenger;
                while (!finished && person != null && person.State != PassengerState.Delivered) yield return null;
                if (finished) break;
                trip.state = TripState.Completed; trip.Adjust(trip.missed ? 0f : 5f);
                onboard.Remove(person); delivered++; earnings += trip.fare; GrossEarnings += trip.fare; OnFareEarned?.Invoke(trip.fare);
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
                KombiChallenge.SaveBest(Stage, Mathf.RoundToInt(earnings));
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
            if (finished) { DrawResultScreen(); return; }
            float width = Mathf.Min(440f, Screen.width - 24f);
            Rect panel = new(Screen.width * .5f - width * .5f, Screen.height * .5f - 180f, width, 360f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 12, panel.y + 22, width - 24, 42), paused ? "SHIFT PAUSED" : success ? "YOU WON" : "YOU LOST", title);
            string outcome = success
                ? $"Mbare reached. {delivered} passengers delivered; target {MinimumPassengers} reached."
                : timeRemaining <= 0f
                    ? $"TIME EXPIRED. Mbare was not reached. {delivered} passengers delivered; target {MinimumPassengers}."
                    : $"TARGET MISSED at Mbare. {delivered} passengers delivered; {MinimumPassengers} required.";
            string stats = $"Passengers delivered: {delivered} / {MinimumPassengers}\nRivals collected: {RivalPassengers}   Rivals finished: {RivalsFinished} / {(rivals == null ? 0 : rivals.Length)}\nGross fares: ${GrossEarnings:0.00}  Repairs: ${RepairExpenses}  Fines: ${Law.TotalFines}\nNet + time bonus: ${earnings:0.00}   Best: ${KombiChallenge.Best(Stage)}";
            string message = paused ? $"{KombiChallenge.Name(Stage)}\nDeliver {MinimumPassengers}+ passengers to Mbare.\nEscape — resume    R — restart" : $"{KombiChallenge.Name(Stage)}\n{outcome}\n\n{stats}";
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

        // End of shift: results on the left, a map of the pickup route and the path driven on the right.
        private void DrawResultScreen()
        {
            resultText ??= new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true };
            mapLabel ??= new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
            resultText.normal.textColor = mapLabel.normal.textColor = Color.white;
            string destination = CityTripSession.Active ? CityTripSelection.Selected.destination : "Mbare";
            float width = Mathf.Min(940f, Screen.width - 24f), height = Mathf.Min(540f, Screen.height - 24f);
            Rect panel = new(Screen.width * .5f - width * .5f, Screen.height * .5f - height * .5f, width, height);
            GUI.Box(panel, GUIContent.none); GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 10, width, 40), success ? "YOU WON" : "YOU LOST", title);

            bool sideBySide = width >= 640f;
            float column = sideBySide ? width * .46f : width - 40f;
            float buttonsTop = panel.yMax - 50f;
            string outcome = success
                ? $"{destination} reached. {delivered} passengers delivered; target {MinimumPassengers} reached."
                : timeRemaining <= 0f
                    ? $"Time expired before reaching {destination}. {delivered} delivered; target {MinimumPassengers}."
                    : crashed
                    ? $"Your kombi was involved in a serious crash. Shift ended. {delivered} passengers delivered."
                    : $"Target missed at {destination}. {delivered} delivered; {MinimumPassengers} required.";
            string stats =
                $"<b>{KombiChallenge.Name(Stage)}</b>\n{outcome}\n\n" +
                $"Passengers delivered: {delivered} / {MinimumPassengers}\n" +
                $"Rivals collected: {RivalPassengers}\n" +
                $"Rivals finished: {RivalsFinished} / {(rivals == null ? 0 : rivals.Length)}\n" +
                $"Gross fares: ${GrossEarnings:0.00}\n" +
                $"Repairs: ${RepairExpenses}    Fines: ${Law.TotalFines}\n" +
                $"Net + time bonus: ${earnings:0.00}\n" +
                $"Best: ${KombiChallenge.Best(Stage)}";
            Rect textRect = new(panel.x + 20, panel.y + 58, column, buttonsTop - panel.y - 66);
            // Shrink the font until the text fits, so lines never overlap the buttons.
            resultText.fontSize = 16;
            while (resultText.fontSize > 11 && resultText.CalcHeight(new GUIContent(stats), textRect.width) > textRect.height) resultText.fontSize--;
            GUI.Label(textRect, stats, resultText);

            if (sideBySide)
            {
                float size = Mathf.Min(width - column - 60f, buttonsTop - panel.y - 90f);
                Rect map = new(panel.xMax - size - 20f, panel.y + 58f, size, size);
                DrawRouteMap(map);
                GUI.Label(new Rect(map.x, map.yMax + 2, size, 18), "Grey: route   Yellow: your drive   Green: start   Red: terminal", mapLabel);
            }

            bool next = true;
            float buttonWidth = (width - 40f - (next ? 20f : 10f)) / (next ? 3f : 2f);
            if (GUI.Button(new Rect(panel.x + 20, buttonsTop, buttonWidth, 34), "REPLAY")) RestartShift();
            if (GUI.Button(new Rect(panel.x + 30 + buttonWidth, buttonsTop, buttonWidth, 34), "MAIN MENU")) ReturnToMenu();
            if (next && GUI.Button(new Rect(panel.x + 40 + buttonWidth * 2, buttonsTop, buttonWidth, 34), success && Stage == KombiStage.Morning ? "NEXT: RUSH HOUR - CHOOSE ROUTE" : "NEXT: CHOOSE ROUTE")) NextStage();
        }

        private void DrawRouteMap(Rect area)
        {
            if (mapPixel == null) { mapPixel = new Texture2D(1, 1); mapPixel.SetPixel(0, 0, Color.white); mapPixel.Apply(); }
            var planned = new List<Vector3>();
            if (routePath != null) foreach (var p in routePath) if (p != null) planned.Add(p.position);
            if (planned.Count < 2 && stations != null) foreach (var s in stations) if (s != null) planned.Add(s.ServicePoint.position);
            var all = new List<Vector3>(planned); all.AddRange(driven);
            if (vehicle != null) all.Add(vehicle.transform.position);
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .65f); GUI.DrawTexture(area, mapPixel);
            if (all.Count < 2) { GUI.color = previous; return; }
            float minX = all.Min(p => p.x), maxX = all.Max(p => p.x), minZ = all.Min(p => p.z), maxZ = all.Max(p => p.z);
            float span = Mathf.Max(maxX - minX, maxZ - minZ, 1f) * 1.1f;
            float cx = (minX + maxX) * .5f, cz = (minZ + maxZ) * .5f;
            Vector2 ToMap(Vector3 w) => new(area.center.x + (w.x - cx) / span * area.width, area.center.y - (w.z - cz) / span * area.height);
            void Line(List<Vector3> points, Color colour, float thickness)
            {
                GUI.color = colour;
                for (int i = 1; i < points.Count; i++)
                {
                    Vector2 a = ToMap(points[i - 1]), b = ToMap(points[i]);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / (thickness * .6f)));
                    for (int s = 0; s <= steps; s++)
                    {
                        Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                        GUI.DrawTexture(new Rect(p.x - thickness * .5f, p.y - thickness * .5f, thickness, thickness), mapPixel);
                    }
                }
            }
            void Dot(Vector3 world, Color colour, float size, string label)
            {
                Vector2 p = ToMap(world); GUI.color = colour;
                GUI.DrawTexture(new Rect(p.x - size * .5f, p.y - size * .5f, size, size), mapPixel);
                if (!string.IsNullOrEmpty(label)) { GUI.color = Color.white; GUI.Label(new Rect(p.x + size * .6f, p.y - 9f, 120f, 18f), label, mapLabel); }
            }
            Line(planned, new Color(.75f, .75f, .75f, 1f), 4f);
            Line(driven, new Color(1f, .85f, .1f, 1f), 3f);
            if (stations != null)
                for (int i = 0; i < stations.Length; i++)
                {
                    var s = stations[i]; if (s == null) continue;
                    bool first = i == 0, last = s.Terminal;
                    Dot(s.ServicePoint.position, first ? Color.green : last ? Color.red : Color.white, first || last ? 11f : 8f, first ? "Start" : last ? destination() : (i).ToString());
                }
            if (vehicle != null) Dot(vehicle.transform.position, new Color(1f, .85f, .1f, 1f), 9f, "You");
            GUI.color = previous;
            string destination() => CityTripSession.Active ? CityTripSelection.Selected.destination : "Mbare";
        }
    }
}
