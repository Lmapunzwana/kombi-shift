#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class KombiExpansionVerification : MonoBehaviour
{
    [Serializable] public class Result { public string utc, unity, scope; public List<string> passed = new(), failed = new(); }
    private readonly Result result = new();
    private float began;
    private bool done;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/KombiShift2/Scenes/KombiShift_FirstShift.unity");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged += Begin;
        EditorApplication.isPlaying = true;
    }
    private static void Begin(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= Begin;
        new GameObject("Expansion integration verification").AddComponent<KombiExpansionVerification>();
    }
    private void Awake() { DontDestroyOnLoad(gameObject); Application.logMessageReceived += Error; }
    private void OnDestroy() => Application.logMessageReceived -= Error;
    private void Error(string message, string stack, LogType type) { if (type == LogType.Exception) result.failed.Add("Exception: " + message); }
    private void Update() { if (!done && Time.realtimeSinceStartup - began > 240f) { Check(false,"wall clock timeout"); Complete(); } }
    private void Check(bool condition, string label)
    { (condition ? result.passed : result.failed).Add(label); Debug.Log("KOMBI_EXPANSION_CHECK " + (condition ? "PASS " : "FAIL ") + label); }
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner,value);
    private IEnumerator Start()
    {
        began = Time.realtimeSinceStartup;
        result.utc = DateTime.UtcNow.ToString("o"); result.unity = Application.unityVersion;
        result.scope = "Actual scene physics/rival observation, then staged passenger, offence and reset integration. Staged positions are test fixtures; not human driving.";
        yield return new WaitForSeconds(2f);
        var director = FindFirstObjectByType<KombiShiftGameDirector>();
        var car = director.Vehicle;
        var hud = FindFirstObjectByType<KombiNavigationHud>();
        Check(director.Stations.Length == 7 && hud.RoutePoints.Length > 100, "expanded stations and navigation references");
        float length = 0f; bool contiguous = true, grounded = true;
        for (int i = 1; i < hud.RoutePoints.Length; i++)
        {
            float gap = Vector3.Distance(hud.RoutePoints[i-1].position,hud.RoutePoints[i].position); length += gap; contiguous &= gap < 30f;
            grounded &= Physics.Raycast(hud.RoutePoints[i].position + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        }
        Check(length > 1200f && contiguous && grounded, "expanded route has contiguous grounded road waypoints over 1.2 km");
        Check(director.Rivals.All(x => !x.GetComponent<Rigidbody>().isKinematic && x.GetComponentsInChildren<WheelCollider>().Length == 4), "rivals use dynamic rigidbodies and four suspension wheels");
        Check(director.Rivals.Select(x => x.MarkerColor).Distinct().Count() == 2, "rival identifiers differ");
        // Give the rivals a clear test approach; a stationary player now correctly starts ahead of them.
        FindFirstObjectByType<RccHiaceTestBootstrap>().enabled=false;
        car.GetComponent<Rigidbody>().isKinematic=true; car.transform.position+=car.transform.right*8f; Physics.SyncTransforms();
        var rival = director.Rivals[0]; var start = rival.transform.position;
        foreach (var item in director.Rivals) { Set(item,"departureDelay",0f); }
        float deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && director.Rivals.Sum(x => x.PassengersTaken) == 0) yield return null;
        Check(Vector3.Distance(start,rival.transform.position)>5f, "rival makes physical progress along expanded route");
        Check(director.Rivals.Any(x => x.Target != null || x.PassengersTaken > 0), "rival evaluates demand and approaches a station");
        Check(director.Rivals.Any(x => x.PassengersTaken > 0), "rival physically reaches and boards from shared station pool");
        if (director.Rivals.Any(x => x.PassengersTaken > 0))
        {
            var collector = director.Rivals.First(x => x.PassengersTaken > 0);
            Check(collector.State == RivalState.Boarding || collector.State == RivalState.Depart, "physical pickup enters boarding/depart state");
            deadline = Time.realtimeSinceStartup + 12f;
            while (collector.State == RivalState.Boarding && Time.realtimeSinceStartup < deadline) yield return null;
            Vector3 departure = collector.transform.position;
            yield return new WaitForSeconds(3f);
            Check(collector.State != RivalState.Boarding && Vector3.Distance(departure,collector.transform.position) > .2f, "rival leaves boarding and physically departs");
        }
        // Start a fresh scene for isolated service tests, without removing rival passengers artificially.
        yield return SceneManager.LoadSceneAsync("KombiShift_FirstShift"); yield return null; yield return null;
        director = FindFirstObjectByType<KombiShiftGameDirector>(); car = director.Vehicle;
        foreach (var item in director.Rivals) { item.enabled = false; item.GetComponent<Rigidbody>().isKinematic = true; }
        foreach (var traffic in FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None)) traffic.gameObject.SetActive(false);
        FindFirstObjectByType<RccHiaceTestBootstrap>().enabled = false;
        car.GetComponent<KombiVehicleStability>().enabled = false;
        var rb = car.GetComponent<Rigidbody>(); rb.isKinematic = true;
        int fareEvents = 0; director.OnFareEarned += amount => fareEvents += amount;
        var first = director.Stations[0];
        car.transform.SetPositionAndRotation(first.ServicePoint.position, first.ServicePoint.rotation);
        Time.timeScale = 4f; deadline = Time.realtimeSinceStartup + 40f;
        while ((director.Passengers < 6 || director.Servicing) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(director.Passengers == 6 && director.Trips.Count == 6, "actual boarding creates six individual trip records");
        Check(fareEvents == 0 && director.Earnings == 0, "fares wait for completed delivery");
        Check(director.Trips.All(x => x.destination > x.boardingStation && x.destination < director.StopCount), "destinations always downstream and valid");
        Check(first.People.Select(x => x.transform.parent).Distinct().Count() == 6, "concurrent boarding allocates distinct cabin seats");
        float timer = director.TimeRemaining; director.SetPaused(true); yield return new WaitForSecondsRealtime(.2f);
        Check(director.TimeRemaining == timer, "pause freezes shift and services"); director.SetPaused(false); Time.timeScale = 4f;
        var drop = director.Stations[1]; car.transform.position = drop.ServicePoint.position - drop.ServicePoint.forward * 50f;
        yield return new WaitForSeconds(1f);
        Check(director.Trips.Any(x => x.requested), "approach emits passenger stop request");
        car.transform.SetPositionAndRotation(drop.ServicePoint.position,drop.ServicePoint.rotation);
        deadline = Time.realtimeSinceStartup + 40f;
        while (director.Delivered < 3 && Time.realtimeSinceStartup < deadline) yield return null;
        Check(director.Delivered >= 3 && director.Trips.Count(x => x.state == TripState.Completed) >= 3, "intermediate alighting completes through character animation");
        Check(fareEvents >= 6 && director.Quality.XP >= 30, "completed trips finalise fare and XP");
        int before = director.Quality.XP;
        Check(director.Quality.Apply("fixture","Harsh brake fixture",-10f,-3), "service reaction event accepted");
        Check(!director.Quality.Apply("fixture","Duplicate",-10f,-3) && director.Quality.XP == before - 3, "quality cooldown suppresses repeated penalties");
        int fines = director.Law.TotalFines;
        Check(director.Law.Record("fixture","Fine fixture",3) && !director.Law.Record("fixture","Duplicate",3) && director.Law.TotalFines == fines+3, "fine incident cooldown charges once");
        director.Condition.ApplyDamage(20f); Check(director.Condition.PowerFactor < 1f && director.Condition.BrakeFactor < 1f, "condition reduces power and braking");
        Check(director.Condition.Repair() && director.Condition.Health == 100f, "paid repair at a stopped service point");
        int speedingBefore = director.Law.TotalFines;
        director.Law.SampleSpeed(80f,1f);
        Check(director.Law.TotalFines == speedingBefore,"speeding grace period avoids instant fine");
        director.Law.SampleSpeed(80f,3f); int speedingFirst = director.Law.TotalFines;
        director.Law.SampleSpeed(80f,20f);
        Check(speedingFirst > speedingBefore && director.Law.TotalFines == speedingFirst,"continuous overspeed incident receives one severity-scaled fine");
        director.Law.SampleSpeed(0f,1f); director.Law.SampleSpeed(80f,4f);
        Check(director.Law.TotalFines > speedingFirst,"compliance re-arms a distinct speeding incident");
        Set(director,"stopIndex",director.StopCount-1);
        var terminal = director.Stations.Last(); car.transform.SetPositionAndRotation(terminal.ServicePoint.position,terminal.ServicePoint.rotation);
        deadline = Time.realtimeSinceStartup+40f;
        while (!director.Finished && Time.realtimeSinceStartup < deadline) yield return null;
        Check(director.Success && director.Delivered >= 6, "intermediate plus terminal deliveries complete Morning stage");
        Time.timeScale=1f; yield return SceneManager.LoadSceneAsync("KombiShift_FirstShift"); yield return null; yield return null;
        director=FindFirstObjectByType<KombiShiftGameDirector>();
        Check(director.Trips.Count==0 && director.Delivered==0 && director.Earnings==0 && director.Quality.XP==0 && director.Condition.Health==100f && director.Law.TotalFines==0, "restart clears trips XP condition and violations");
        Set(director,"timeRemaining",.01f); yield return new WaitForSecondsRealtime(.2f);
        Check(director.Finished && !director.Success,"timeout produces failure");
        Complete();
    }
    private void Complete()
    {
        if (done) return; done=true; Time.timeScale=1f;
        Directory.CreateDirectory("Docs/Validation/Expansion"); File.WriteAllText("Docs/Validation/Expansion/integration.json",JsonUtility.ToJson(result,true));
        Debug.Log("KOMBI_EXPANSION_RESULT " + result.passed.Count + " passed " + result.failed.Count + " failed");
        EditorApplication.Exit(result.failed.Count==0?0:1);
    }
}
#endif
