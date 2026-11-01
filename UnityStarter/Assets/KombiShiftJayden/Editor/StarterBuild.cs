using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace KombiShift.JaydenStarter.Editor
{
    public static class StarterBuild
    {
        public static void Linux()
        {
            const string scene="Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity";
            PlayerSettings.companyName="Kombi Shift Team"; PlayerSettings.productName="Kombi Shift — Jayden Preview";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Linux/KombiShift.x86_64"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new[]{scene},output,BuildTarget.StandaloneLinux64,BuildOptions.None);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Starter build failed: "+report.summary.result);
            Debug.Log("KOMBI_BUILD_SUCCESS "+output);
        }
    }
}
