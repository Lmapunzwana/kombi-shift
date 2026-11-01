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
            var report=new SmokeReport{unityVersion=Application.unityVersion,mode="automated prototype smoke; no human input simulated"};
            Action<bool,string> check=(ok,name)=>{if(ok)report.passed.Add(name);else report.failed.Add(name);};
            check(driver!=null&&chase!=null&&rider!=null,"generated player, camera and passenger");
            check(traffic.Count==7,"seven preallocated traffic vehicles");
            check(screen==Screen.Menu,"startup menu");
            foreach(var car in traffic)car.transform.gameObject.SetActive(false); // Isolate physics/passenger assertions from random traffic contact.
            Rainy=false;Begin();check(IsPlaying&&Fuel==100&&Condition==100,"begin resets state");
            driver.Body.linearVelocity=Vector3.forward*8;float startZ=driver.transform.position.z;
            yield return new WaitForSeconds(.3f);check(driver.transform.position.z>startZ+1,"Rigidbody advances in fixed timestep");
            driver.Body.position=stops[0]+new Vector3(0,.62f,0);driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;
            yield return new WaitForFixedUpdate();Interact();check(boarding,"pickup starts boarding coroutine");
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
