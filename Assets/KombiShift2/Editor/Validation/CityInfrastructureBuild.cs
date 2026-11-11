using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class KombiCityInfrastructureBuild
{
    public static void Linux()=>Build(BuildTarget.StandaloneLinux64,"Linux/KombiShift2.x86_64");
    public static void Web()=>Build(BuildTarget.WebGL,"Web");
    static void Build(BuildTarget target,string output)
    {
        string root=Path.GetFullPath("../../Builds/CityInfrastructure2026");Directory.CreateDirectory(root);
        if(target==BuildTarget.WebGL){PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.decompressionFallback=false;}
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{KombiProjectWorkbench.Menu,KombiProjectWorkbench.Shift},target=target,locationPathName=Path.Combine(root,output),options=BuildOptions.None});
        var s=report.summary;Directory.CreateDirectory("Docs/Validation/CityInfrastructure");
        File.WriteAllText("Docs/Validation/CityInfrastructure/build-"+target+".json",JsonUtility.ToJson(new Result{utc=DateTime.UtcNow.ToString("o"),unity=Application.unityVersion,platform=target.ToString(),result=s.result.ToString(),errors=(int)s.totalErrors,warnings=(int)s.totalWarnings,bytes=(long)s.totalSize,seconds=s.totalTime.TotalSeconds,output=Path.Combine(root,output)},true));
        if(s.result!=BuildResult.Succeeded)throw new Exception("City build failed: "+s.result);
    }
    [Serializable] class Result {public string utc,unity,platform,result,output;public int errors,warnings;public long bytes;public double seconds;}
}
