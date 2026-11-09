using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class KombiExpansionBuild
{
    public static void Linux() => Build(BuildTarget.StandaloneLinux64,"Linux/KombiShift2.x86_64");
    public static void Windows() => Build(BuildTarget.StandaloneWindows64,"Windows/KombiShift2.exe");
    public static void Web() => Build(BuildTarget.WebGL,"Web");
    static void Build(BuildTarget target,string path)
    {
        string root = Path.GetFullPath("../../Builds/Expansion2026"); Directory.CreateDirectory(root);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{KombiProjectWorkbench.Menu,KombiProjectWorkbench.Shift},
            target=target,locationPathName=Path.Combine(root,path),options=BuildOptions.None });
        Directory.CreateDirectory("Docs/Validation/Expansion");
        var summary=report.summary;
        File.WriteAllText("Docs/Validation/Expansion/build-"+target+".json",JsonUtility.ToJson(new Record {
            unity=Application.unityVersion,utc=DateTime.UtcNow.ToString("o"),platform=target.ToString(),result=summary.result.ToString(),
            errors=(int)summary.totalErrors,warnings=(int)summary.totalWarnings,bytes=(long)summary.totalSize,seconds=summary.totalTime.TotalSeconds,output=Path.Combine(root,path)},true));
        if(summary.result!=BuildResult.Succeeded)throw new Exception("Expansion build failed: "+summary.result);
    }
    [Serializable] class Record { public string unity,utc,platform,result,output;public int errors,warnings;public long bytes;public double seconds; }
}
