using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using KombiShift2.Traffic;

public static class KombiAssessmentBuild
{
    public static void Linux() => Build(BuildTarget.StandaloneLinux64, "Linux/KombiShift2.x86_64");
    public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Windows/KombiShift2.exe");
    public static void Web() => Build(BuildTarget.WebGL, "Web");
    static void Build(BuildTarget target, string destination)
    {
        string root = Path.GetFullPath("../../Builds/Assessment2026");
        Directory.CreateDirectory(root);
        PlayerSettings.companyName = "Kombi Shift Team";
        PlayerSettings.productName = "Kombi Shift 2";
        PlayerSettings.bundleVersion = "1.1.0";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        // This is a desktop/browser game; inherited headset loaders must not gate its release.
        var group = BuildPipeline.GetBuildTargetGroup(target);
        var xr = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
        if (xr != null)
        {
            xr.InitManagerOnStart = false;
            if (xr.Manager != null)
            {
                foreach (var loader in xr.Manager.activeLoaders.ToArray()) xr.Manager.TryRemoveLoader(loader);
                EditorUtility.SetDirty(xr.Manager);
            }
            EditorUtility.SetDirty(xr);
        }
        if (target == BuildTarget.StandaloneWindows64)
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
        }
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { KombiProjectWorkbench.Menu, KombiProjectWorkbench.Shift },
            locationPathName = Path.Combine(root, destination), target = target, options = BuildOptions.None });
        var summary = report.summary;
        File.WriteAllText(Path.Combine(root, target + "-build.json"), JsonUtility.ToJson(new BuildRecord {
            platform = target.ToString(), result = summary.result.ToString(), bytes = (long)summary.totalSize,
            errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings, unity = Application.unityVersion,
            builtUtc = DateTime.UtcNow.ToString("o"), durationSeconds = summary.totalTime.TotalSeconds }, true));
        if (summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + summary.result);
    }
    [Serializable] class BuildRecord { public string platform,result,unity,builtUtc; public long bytes; public int errors,warnings; public double durationSeconds; }

    public static void ProfileTraffic()
    {
        EditorSceneManager.OpenScene(KombiProjectWorkbench.Shift);
        var traffic = UnityEngine.Object.FindObjectsByType<KombiTrafficVehicle>(FindObjectsSortMode.None);
        if (traffic.Length == 0) throw new Exception("No traffic for fixture");
        Physics.SyncTransforms();
        foreach (var vehicle in traffic) { vehicle.ProfileQueries(true); vehicle.ProfileQueries(false); }
        string folder = "Docs/Validation/Assessment"; Directory.CreateDirectory(folder);
        using var csv = new StreamWriter(Path.Combine(folder, "traffic-query-samples.csv"));
        csv.WriteLine("sample,mode,vehicles,iterations,monoUsedHeapDeltaBytes,elapsedMilliseconds,hitCount");
        long baseline = 0, optimized = 0;
        int expected = -1;
        var watch = new System.Diagnostics.Stopwatch();
        for (int sample = 0; sample < 120; sample++)
        {
            for (int mode = 0; mode < 2; mode++)
            {
                // Alternate order to reduce systematic first-query timing bias.
                bool allocating = (mode == 0) == (sample % 2 == 0);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                long start = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(); watch.Restart(); int hits = 0;
                for (int repeat = 0; repeat < 20; repeat++) foreach (var vehicle in traffic) hits += vehicle.ProfileQueries(allocating);
                watch.Stop(); long allocation = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - start;
                if (expected < 0) expected = hits;
                if (expected != hits) throw new Exception("Buffered query hit counts differ from full queries; increase buffers before claiming optimisation.");
                if (allocating) baseline += allocation; else optimized += allocation;
                csv.WriteLine($"{sample},{(allocating ? "baseline" : "buffered")},{traffic.Length},20,{allocation},{watch.Elapsed.TotalMilliseconds:F6},{hits}");
            }
        }
        File.WriteAllText(Path.Combine(folder, "traffic-query-results.json"), JsonUtility.ToJson(new ProfileRecord {
            vehicles=traffic.Length,samplesPerMode=120,iterationsPerSample=20,baselineTotalBytes=baseline,bufferedTotalBytes=optimized,
            reductionPercent=baseline==0?0:100f*(baseline-optimized)/baseline,identicalHitCounts=true,
            scope="Editor CPU physics-query fixture; Mono used-heap delta after forced collection per sample; not whole-game FPS or human playtesting",utc=DateTime.UtcNow.ToString("o") }, true));
        if (optimized >= baseline) throw new Exception("No measured allocation improvement");
        Debug.Log("KOMBI_TRAFFIC_PROFILE_PASS");
    }
    [Serializable] class ProfileRecord { public int vehicles,samplesPerMode,iterationsPerSample; public long baselineTotalBytes,bufferedTotalBytes; public float reductionPercent; public bool identicalHitCounts; public string scope,utc; }
}
