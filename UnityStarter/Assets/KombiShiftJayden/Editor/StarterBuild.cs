using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace KombiShift.JaydenStarter.Editor
{
    public static class StarterBuild
    {
        static void Build(BuildTarget target,string location,BuildOptions options)
        {
            bool hdrp=GraphicsSettings.defaultRenderPipeline!=null&&GraphicsSettings.defaultRenderPipeline.GetType().Name.Contains("HDRender");
            if(hdrp&&target==BuildTarget.WebGL)throw new Exception("HDRP is desktop-only. Build Web from UnityStarter.");
            const string scene="Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity";
            PlayerSettings.companyName="Kombi Shift Team";PlayerSettings.productName="Kombi Shift";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            if(target==BuildTarget.WebGL){PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.dataCaching=false;PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.FullWithStacktrace;}
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/"+(hdrp?"HDRP/":"")+location));Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new[]{scene},output,target,options);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);Debug.Log("KOMBI_BUILD_SUCCESS "+output);
        }
        public static void AllTargets(){Linux();Windows();if(GraphicsSettings.defaultRenderPipeline==null)Web();}
        public static void Linux()=>Build(BuildTarget.StandaloneLinux64,"Linux/KombiShift.x86_64",BuildOptions.None);
        public static void Profile()=>Build(BuildTarget.StandaloneLinux64,"Profiling/KombiShift.x86_64",BuildOptions.Development);
        public static void Windows()=>Build(BuildTarget.StandaloneWindows64,"Windows/KombiShift.exe",BuildOptions.None);
        public static void WindowsProfile()=>Build(BuildTarget.StandaloneWindows64,"Windows-Profile/KombiShift.exe",BuildOptions.Development);
        public static void Web()=>Build(BuildTarget.WebGL,"Web",BuildOptions.None);
    }
}
