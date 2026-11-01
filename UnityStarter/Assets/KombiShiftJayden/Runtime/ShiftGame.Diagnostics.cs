using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        [Serializable] sealed class SmokeReport { public string unityVersion; public string mode; public List<string> passed = new List<string>(); public List<string> failed = new List<string>(); }
        [Serializable] sealed class InputProbeReport
        {
            public string test="native keyboard event probe";public float maximumThrottle,maximumSteering,minimumThrottle,maximumSpeed,distance;
            public bool sawPause,sawResume,grounded;public string backend;public int focusedFrames,guiKeyEvents,anyKeyFrames;
        }
        IEnumerator ProbePhysicalInput()
        {
            Rainy=false;Begin();foreach(var car in traffic)car.transform.gameObject.SetActive(false);
            var report=new InputProbeReport();
#if ENABLE_INPUT_SYSTEM
            report.backend="Input System";
#else
            report.backend="Legacy direct keys + GUI event fallback";
#endif
            yield return new WaitForSecondsRealtime(2);Vector3 origin=driver.transform.position;
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,Application.isEditor?"../../Builds":"../.."));Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"input-probe-ready.txt"),"Ready for native W D SPACE S ESC key events");
            for(float until=Time.realtimeSinceStartup+25;Time.realtimeSinceStartup<until;){
                if(Application.isFocused)report.focusedFrames++;report.guiKeyEvents=ShiftInput.GUIKeyEvents;
#if ENABLE_LEGACY_INPUT_MANAGER
                if(Input.anyKey)report.anyKeyFrames++;
#endif
                report.maximumThrottle=Mathf.Max(report.maximumThrottle,ShiftInput.Throttle);report.minimumThrottle=Mathf.Min(report.minimumThrottle,ShiftInput.Throttle);
                report.maximumSteering=Mathf.Max(report.maximumSteering,Mathf.Abs(ShiftInput.Turn));report.maximumSpeed=Mathf.Max(report.maximumSpeed,driver.Speed);
                report.grounded|=driver.Grounded;report.distance=Mathf.Max(report.distance,Vector3.Distance(origin,driver.transform.position));
                if(screen==Screen.Paused)report.sawPause=true;if(report.sawPause&&screen==Screen.Playing)report.sawResume=true;
                yield return null;
            }
            File.WriteAllText(Path.Combine(directory,"input-probe.json"),JsonUtility.ToJson(report,true));
            Debug.Log("KOMBI_INPUT_PROBE_COMPLETE "+report.distance+"m; throttle "+report.maximumThrottle);
            foreach(var car in traffic)car.transform.gameObject.SetActive(true);Menu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-exit")>=0)Application.Quit();
        }
        IEnumerator CapturePreview(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"menu.png"));
            yield return new WaitForSecondsRealtime(1);
            Begin();
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"city.png"));
            yield return new WaitForSecondsRealtime(1);
            Rainy=true;Begin();
            yield return new WaitForSecondsRealtime(1);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"rain.png"));
            yield return new WaitForSecondsRealtime(1);
            Rainy=false;Menu();
            rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            Debug.Log("KOMBI_PREVIEW_CAPTURED "+directory);
        }
        IEnumerator RunSmokeCheck()
        {
            var report=new SmokeReport{unityVersion=Application.unityVersion,mode="automated control-to-physics and gameplay regression; injected control commands, not human playtesting"};
            Action<bool,string> check=(ok,name)=>{if(ok)report.passed.Add(name);else report.failed.Add(name);};
            check(driver!=null&&chase!=null&&rider!=null,"generated player, camera and passenger");
            check(traffic.Count==7,"seven preallocated traffic vehicles");
            check(screen==Screen.Menu,"startup menu");
            foreach(var car in traffic)car.transform.gameObject.SetActive(false); // Isolate physics/passenger assertions from random traffic contact.
            Rainy=false;Begin();check(IsPlaying&&Fuel==100&&Condition==100,"begin resets state");
            yield return new WaitForSeconds(.5f);check(driver.Grounded,"ground detection excludes own collider");
            ShiftInput.testOverride=true;ShiftInput.testThrottle=1;ShiftInput.testTurn=0;ShiftInput.testBrake=false;
            float startZ=driver.transform.position.z;yield return new WaitForSeconds(1.2f);
            check(driver.Speed>4&&driver.transform.position.z>startZ+2,"throttle command accelerates stationary kombi through controller");
            float startAngle=driver.transform.eulerAngles.y;ShiftInput.testTurn=.7f;yield return new WaitForSeconds(.45f);
            check(Mathf.Abs(Mathf.DeltaAngle(startAngle,driver.transform.eulerAngles.y))>10,"steering command rotates moving vehicle");
            ShiftInput.testThrottle=0;ShiftInput.testTurn=0;ShiftInput.testBrake=true;yield return new WaitForSeconds(1);
            check(driver.Speed<.6f,"brake command stops vehicle");
            ShiftInput.testBrake=false;ShiftInput.testThrottle=-1;yield return new WaitForSeconds(.8f);
            check(driver.SignedSpeed< -2,"reverse command drives backwards");
            ShiftInput.testOverride=false;ShiftInput.testThrottle=ShiftInput.testTurn=0;ShiftInput.testBrake=false;Begin();
            driver.Body.position=stops[0]+new Vector3(0,.62f,0);driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;
            yield return new WaitForSeconds(.4f);Interact();check(boarding,"pickup starts boarding coroutine");
            yield return new WaitForSeconds(.2f);Pause(true);Vector3 pausedPosition=rider.position;
            yield return new WaitForSecondsRealtime(.25f);check(boarding&&Vector3.Distance(pausedPosition,rider.position)<.001f,"pause holds boarding animation");Pause(false);
            yield return new WaitForSeconds(3.2f);check(aboard&&!boarding&&!rider.gameObject.activeSelf,"boarding completes and hides passenger");
            int eventCount=0;Action<float> listener=value=>eventCount++;FareEarned+=listener;
            driver.Body.position=stops[1]+new Vector3(0,.62f,0);driver.Body.linearVelocity=Vector3.zero;
            yield return new WaitForFixedUpdate();Interact();FareEarned-=listener;
            check(delivered==1&&gross==6&&eventCount==1&&!aboard,"drop-off pays fare once and publishes event");
            fare=2;Interact();check(!boarding&&!aboard,"excessive fare refused");fare=1;
            float prior=Condition;Damage(5);check(Condition==prior-5&&repairCost>0,"damage updates condition and cost");
            Fuel=50;driver.Body.position=new Vector3(4,.62f,110);driver.Body.linearVelocity=Vector3.zero;
            yield return new WaitForFixedUpdate();Interact();check(Fuel==100,"station refills stopped vehicle");
            Fuel=0;yield return null;check(screen==Screen.Statement,"empty fuel ends shift");
            Rainy=true;Begin();check(IsPlaying&&Rainy&&rain.isPlaying&&Remittance==12,"rain challenge starts with higher remittance");
            Pause(true);check(Time.timeScale==0&&screen==Screen.Paused,"pause stops simulation");Pause(false);
            elapsed=shiftSeconds;delivered=3;gross=100;yield return null;
            check(screen==Screen.Statement&&outcome=="Shift target met!","successful shift completion");
            Begin();check(gross==0&&delivered==0&&Fuel==100,"replay resets economy and progress");Menu();check(screen==Screen.Menu,"return to menu");
            foreach(var car in traffic)car.transform.gameObject.SetActive(true);
            string path=Path.Combine(Application.dataPath,"../../Builds/smoke-results.json");
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--kombi-report="))path=arg.Substring("--kombi-report=".Length);
            path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
            Debug.Log("KOMBI_SMOKE_COMPLETE "+report.passed.Count+" passed; "+report.failed.Count+" failed; "+path);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-exit")>=0)Application.Quit(report.failed.Count==0?0:1);
        }
    }
}
