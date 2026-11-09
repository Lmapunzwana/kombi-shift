using System;
using System.IO;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Traffic;
using KombiShift2.UI;
using KombiShift2.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KombiExpansionValidation
{
    public static void Prepare()
    {
        Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh();
        foreach (var stage in new[] { KombiStage.Morning, KombiStage.RushHour })
        {
            string path = "Assets/Resources/" + (stage == KombiStage.Morning ? "MorningShift" : "RushHour") + ".asset";
            if (AssetDatabase.LoadAssetAtPath<KombiStageSettings>(path) != null) continue;
            var settings = ScriptableObject.CreateInstance<KombiStageSettings>();
            if (stage == KombiStage.RushHour) { settings.duration = 420; settings.deliveryTarget = 6; settings.rivalSpeed = 9; settings.rivalHeadStart = 8; settings.trafficFraction = 1; settings.fare = 3; }
            AssetDatabase.CreateAsset(settings, path);
        }
        AssetDatabase.SaveAssets(); Audit(); Rules();
    }
    public static void Audit()
    {
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var hud = UnityEngine.Object.FindFirstObjectByType<KombiNavigationHud>();
        var report = new System.Text.StringBuilder();
        report.AppendLine("Route points");
        for (int i = 0; i < hud.RoutePoints.Length; i++) report.AppendLine(i + ": " + hud.RoutePoints[i].position.ToString("F2"));
        report.AppendLine("Stations");
        foreach (var station in UnityEngine.Object.FindObjectsByType<PassengerStation>(FindObjectsSortMode.None)) report.AppendLine(station.name + ": " + station.ServicePoint.position.ToString("F2") + " waiting=" + station.WaitingCount);
        report.AppendLine("Road splines");
        foreach (var road in UnityEngine.Object.FindObjectsByType<BezierSolution.BezierSpline>(FindObjectsSortMode.None)) report.AppendLine(road.name + ": " + road.GetPoint(0).ToString("F2") + " -> " + road.GetPoint(1).ToString("F2"));
        Directory.CreateDirectory("Docs/Validation/Expansion");
        File.WriteAllText("Docs/Validation/Expansion/map-audit.txt", report.ToString());
        Debug.Log("KOMBI_EXPANSION_AUDIT_COMPLETE");
    }
    public static void Rules()
    {
        var results = new System.Collections.Generic.List<string>();
        void Check(string name, bool passed) { results.Add((passed ? "PASS " : "FAIL ") + name); }
        var profile = RivalDrivingProfile.For(RivalPersonality.Competitive);
        Check("utility favours demand over proximity", profile.Utility(6, 6, 100, 8, 0, false) > profile.Utility(1, 6, 5, 8, 0, false));
        Check("utility caps available seats", profile.Utility(6, 1, 10, 8, 0, false) == profile.Utility(1, 1, 10, 8, 0, false));
        Check("reservations reduce utility", profile.Utility(6, 6, 10, 8, 0, true) < profile.Utility(6, 6, 10, 8, 0, false));
        Check("congestion reduces utility", profile.Utility(6, 6, 10, 8, 2, false) < profile.Utility(6, 6, 10, 8, 0, false));
        Check("forward left lane", RivalKombiAI.LeftOffset(Vector3.forward, 1.5f).x < 0f);
        Check("reverse left lane", RivalKombiAI.LeftOffset(Vector3.back, 1.5f).x > 0f);
        var trip = new PassengerTrip();
        Check("request outside radius rejected", !trip.Request(100f, 90f));
        Check("request near destination", trip.Request(60f, 90f));
        Check("request emitted once", !trip.Request(50f, 90f));
        Check("stop not missed on approach", !trip.CheckMissed(50f, 90f));
        Check("missed stop after passing", trip.CheckMissed(160f, 90f));
        Check("missed stop penalty once", !trip.CheckMissed(180f, 90f) && trip.happiness == 80f);
        trip.Adjust(-1000f); Check("happiness lower clamp", trip.happiness == 0f);
        trip.Adjust(1000f); Check("happiness upper clamp", trip.happiness == 100f);
        Check("harmless contact no damage", KombiVehicleCondition.ImpactDamage(2f, 3f) == 0f);
        Check("low impulse no damage", KombiVehicleCondition.ImpactDamage(10f, .1f) == 0f);
        Check("severity proportional damage", KombiVehicleCondition.ImpactDamage(12f, 3f) > KombiVehicleCondition.ImpactDamage(5f, 3f));
        Check("damage capped", KombiVehicleCondition.ImpactDamage(100f, 30f) == 35f);
        var go = new GameObject("Condition fixture"); var condition = go.AddComponent<KombiVehicleCondition>();
        condition.ApplyDamage(1000); Check("condition and modifiers bounded", condition.Health == 10f && condition.PowerFactor >= .45f && condition.BrakeFactor >= .75f);
        UnityEngine.Object.DestroyImmediate(go);
        var stationGo = new GameObject("Reservation fixture"); var station = stationGo.AddComponent<PassengerStation>();
        var passengerGo = new GameObject("Waiting person"); var person = passengerGo.AddComponent<StationPassenger>();
        station.Configure(new[] { person }, stationGo.transform, Array.Empty<Transform>(), false);
        var a = new GameObject("A"); var b = new GameObject("B");
        Check("first physical reservation succeeds", station.TryReserve(a.transform));
        Check("competing reservation rejected", !station.TryReserve(b.transform));
        station.Release(b.transform); Check("other owner cannot release", station.Reserved);
        station.Release(a.transform); Check("owner releases reservation", !station.Reserved);
        b.transform.position = Vector3.forward * 20f; Check("distant reservation rejected", !station.TryReserve(b.transform));
        foreach (var item in new[] { a, b, passengerGo, stationGo }) UnityEngine.Object.DestroyImmediate(item);
        Check("stage escalation", KombiChallenge.RivalSpeed(KombiStage.RushHour) > KombiChallenge.RivalSpeed(KombiStage.Morning) && KombiChallenge.Seconds(KombiStage.RushHour) < KombiChallenge.Seconds(KombiStage.Morning));
        Directory.CreateDirectory("Docs/Validation/Expansion");
        File.WriteAllLines("Docs/Validation/Expansion/rule-results.txt", new[] { "Executed " + DateTime.UtcNow.ToString("o") + " Unity " + Application.unityVersion, "Scope: editor rule/component fixtures; not a driven shift or human playtest." }.Concat(results));
        if (results.Any(x => x.StartsWith("FAIL"))) throw new Exception("Expansion rules failed");
        Debug.Log("KOMBI_EXPANSION_RULES_PASS " + results.Count);
    }
}
