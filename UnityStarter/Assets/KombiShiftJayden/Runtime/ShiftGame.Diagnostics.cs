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
        [Serializable] sealed class AudioProof{public int sampleRate;public float peakRms,listenerVolume,effectsLevel;public bool realEngineLoaded,enginePlaying,hwindiClipsLoaded;}
        IEnumerator CapturePreview(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"menu.png"));
            screen=Screen.Selection;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"level-hwindi-selection.png"));
            screen=Screen.Menu;
            yield return new WaitForEndOfFrame();
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
            Rainy=false;Begin();driver.Body.position=stops[0]+new Vector3(.5f,.3f,-1);driver.ParkNow();
            yield return new WaitForSeconds(1);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"hwindi-calling.png"));
            Interact();yield return new WaitForSeconds(.65f);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"hwindi-boarding.png"));
            yield return new WaitForSeconds(2);
            driver.Body.position=Target+new Vector3(.5f,.3f,-1);driver.ParkNow();yield return new WaitForSeconds(.4f);Interact();
            yield return new WaitForSeconds(.65f);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,"hwindi-alighting.png"));
            yield return new WaitForSeconds(1.5f);
            var proof=new AudioProof{sampleRate=AudioSettings.outputSampleRate,peakRms=peakOutputRms,listenerVolume=AudioListener.volume,effectsLevel=effectsVolume,realEngineLoaded=idleRecording!=null,enginePlaying=engine.isPlaying,hwindiClipsLoaded=callClip!=null&&readyClip!=null&&fareVoiceClip!=null};
            File.WriteAllText(Path.Combine(directory,"audio-proof.json"),JsonUtility.ToJson(proof,true));
            Rainy=false;Menu();
            rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            Debug.Log("KOMBI_PREVIEW_CAPTURED "+directory);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-exit")>=0)Application.Quit();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-exit")>=0&&!Application.isEditor)Application.Quit();
        }
        void IsolateTraffic(){rival.enabled=false;foreach(var car in traffic)car.transform.gameObject.SetActive(false);}
        IEnumerator RunSmokeCheck()
        {
            var report=new SmokeReport{unityVersion=Application.unityVersion,mode="concept mechanics regression; automated actions, not human playtesting"};
            Action<bool,string> check=(ok,name)=>{if(ok)report.passed.Add(name);else report.failed.Add(name);};
            Rainy=false;Begin();IsolateTraffic();
            check(gameManager!=null&&fuelSystem!=null&&spawnManager!=null&&uiManager!=null&&audioManager!=null,"modular managers initialized");
            check(stops.Length==6&&cityEnvironment.gameObject.activeSelf&&!highwayEnvironment.gameObject.activeSelf,"CBD is six stops on a road network");
            check(waiting.Count==12&&manifest.Count==0&&passengerPrefab!=null,"individual waiting passengers spawned from pool");
            check(waiting[0].destination!=waiting[1].destination&&waiting[0].baseFare!=waiting[1].baseFare,"passengers have different destinations and distance fares");
            yield return new WaitForSeconds(.5f);check(driver.Grounded,"grounded body");
            ShiftInput.testOverride=true;ShiftInput.testThrottle=1;ShiftInput.testTurn=0;ShiftInput.testBrake=false;float startZ=driver.Body.position.z;
            yield return new WaitForSeconds(1.2f);check(driver.Speed>4&&driver.Body.position.z>startZ+2,"controller accelerates from rest");
            ShiftInput.testTurn=.7f;float yaw=driver.transform.eulerAngles.y;yield return new WaitForSeconds(.4f);check(Mathf.Abs(Mathf.DeltaAngle(yaw,driver.transform.eulerAngles.y))>10,"steering changes heading");
            ShiftInput.testThrottle=0;ShiftInput.testTurn=0;ShiftInput.testBrake=true;yield return new WaitForSeconds(1);check(driver.IsParked&&driver.Speed<.1f,"braking parks vehicle");
            ShiftInput.testBrake=false;Vector3 parked=driver.Body.position;yield return new WaitForSeconds(.6f);driver.Body.AddForce(Vector3.right*10000,ForceMode.Impulse);yield return new WaitForSeconds(.2f);check(Vector3.Distance(parked,driver.Body.position)<.01f,"stop remains stable after release and push");
            ShiftInput.testThrottle=-1;yield return new WaitForSeconds(.8f);check(driver.SignedSpeed< -2,"deliberate reverse releases hold");ShiftInput.testOverride=false;ShiftInput.testThrottle=0;
            Begin();IsolateTraffic();stopIndex=0;driver.Body.position=stops[0]+new Vector3(0,.3f,7);driver.ParkNow();Interact();check(!boarding,"pickup rejected outside trigger zone");
            driver.Body.position=stops[0]+new Vector3(0,.3f,0);yield return new WaitForSeconds(.3f);Interact();Interact();check(boarding,"inside-zone boarding starts once");
            yield return new WaitForSeconds(.3f);check(hwindiState==HwindiState.Boarding&&doorAmount>.4f,"hwindi and sliding door support boarding");Pause(true);Vector3 passengerSpot=rider.position;yield return new WaitForSecondsRealtime(.2f);check(boarding&&Vector3.Distance(passengerSpot,rider.position)<.01f,"boarding pauses with shift");Pause(false);
            yield return new WaitForSeconds(2.3f);check(manifest.Count==2&&aboard&&!boarding,"multiple passengers boarded");
            var first=manifest[0];var second=manifest[1];float before=first.remaining;yield return new WaitForSeconds(.3f);check(first.remaining<before&&first.gameObject.activeSelf,"onboard patience continues while visual is hidden");
            float agreed=first.agreedFare;fare=1.5f;check(first.agreedFare==agreed,"fare is quoted at boarding and cannot be raised onboard");fare=1;
            int payouts=0;Action<float> paid=amount=>payouts++;FareEarned+=paid;float cashBefore=wallet;stopIndex=first.destination;driver.Body.position=Target+Vector3.up*.3f;driver.ParkNow();yield return new WaitForSeconds(.2f);Interact();check(unloading,"destination starts alighting coroutine");yield return new WaitForSeconds(1.1f);
            check(payouts==1&&gross>agreed&&tipsTotal>0&&manifest.Count==1,"only matching destination pays fare and quick-delivery tip");check(wallet>cashBefore,"fare is banked as spendable cash");FareEarned-=paid;
            second.remaining=.01f;yield return new WaitForSeconds(.2f);check(second.state==PassengerState.Expired,"delivery patience expires individually");float earned=gross;stopIndex=second.destination;driver.Body.position=Target+Vector3.up*.3f;driver.ParkNow();yield return new WaitForSeconds(.2f);Interact();yield return new WaitForSeconds(1.1f);check(gross==earned&&manifest.Count==0,"late passenger leaves without fare");
            fuelSystem.Amount=30;wallet=10;driver.Body.position=level.station+Vector3.up*.3f;driver.ParkNow();yield return new WaitForSeconds(.2f);float available=wallet;Interact();check(fuelSystem.Amount>49&&wallet<available&&fuelSystem.PurchaseCost>0,"refuel purchases fuel using cash");
            fuelSystem.Amount=30;wallet=0;Interact();check(fuelSystem.Amount<=30,"refuel refuses insufficient cash");
            int created=spawnManager.Created;Begin();IsolateTraffic();Begin();IsolateTraffic();check(spawnManager.Created==created,"traffic pothole and passenger pools reused on restart");
            var expired=waiting[0];expired.remaining=.01f;int oldMissed=missed;yield return new WaitForSeconds(.2f);check(missed>oldMissed&&!waiting.Contains(expired),"waiting patience expiry recycles pedestrian");
            check(gameManager.Evaluate(FareTarget-.1f,100,100,0)==null,"below target is not a win");int ends=0;Action<string> ended=reason=>ends++;gameManager.OnShiftEnded+=ended;gross=FareTarget;yield return null;Finish("duplicate");check(screen==Screen.Statement&&outcome=="Fare target reached!"&&ends==1&&noCrashBonus==5,"fare target wins immediately and clean bonus is scored once");gameManager.OnShiftEnded-=ended;
            Begin();IsolateTraffic();fuelSystem.Amount=0;yield return null;check(outcome=="Out of fuel","zero fuel loses");Begin();IsolateTraffic();Condition=0;yield return null;check(outcome=="Vehicle unusable","zero health loses");Begin();IsolateTraffic();elapsed=shiftSeconds;yield return null;check(outcome=="Shift ended below target","clock expiry below target loses");
            Rainy=true;Begin();IsolateTraffic();check(stops.Length==4&&stops[3].z==465&&highwayEnvironment.gameObject.activeSelf&&!cityEnvironment.gameObject.activeSelf,"highway has a separate long-route environment");check(Fuel<50&&level.fuelPerMetre>.07f&&level.aggression>1&&FareTarget>18,"highway changes fuel economy target grip and aggression");check(hazards.Count==12&&rain.isPlaying,"highway activates pooled hazards and rain");
            Rainy=false;Begin();IsolateTraffic();rival.gameObject.SetActive(true);rival.enabled=true;driver.Body.position=new Vector3(40,.3f,220);rival.GetComponent<Rigidbody>().position=new Vector3(0,.225f,-15);rival.Configure(this,roadGraph,1,false);check(rival.DebugLabel=="CRUISE","rival exposes a readable cruise debug label");yield return new WaitForSeconds(.5f);check(rival.Current==RivalAI.State.ChasePassenger&&rival.DebugLabel=="CHASE PASSENGER","rival pursues nearest waiting passenger and updates its debug label");
            rival.ApplyDamage(90);yield return new WaitForSeconds(.5f);check(rival.Current==RivalAI.State.Flee&&rival.DebugLabel=="FLEE","damaged rival retreats and updates its debug label");rival.Configure(this,roadGraph,1,false);driver.Body.position=rival.transform.position+Vector3.forward*9;yield return new WaitForSeconds(.5f);check(rival.Current==RivalAI.State.Block&&rival.DebugLabel=="BLOCK","nearby rival attempts bounded blocking and updates its debug label");
            rival.enabled=false;check(sun.shadows==LightShadows.None&&QualitySettings.shadows==ShadowQuality.Disable,"prior no-shadow preference preserved");check(idleRecording!=null&&callClip!=null,"recorded engine and conductor calls load");
            Rainy=false;Begin();IsolateTraffic();
            float freshStopping=0,wornStopping=0;
            for(int sample=0;sample<2;sample++){Wear.brakes=sample==0?100:0;driver.Body.position=new Vector3(-3,.34f,0);driver.ParkNow();ShiftInput.testOverride=true;ShiftInput.testThrottle=1;ShiftInput.testBrake=false;yield return new WaitForSeconds(.7f);driver.Body.linearVelocity=Vector3.forward*12;ShiftInput.testThrottle=0;ShiftInput.testBrake=true;float brakeStartZ=driver.Body.position.z;yield return new WaitForSeconds(1);float travelled=driver.Body.position.z-brakeStartZ;if(sample==0)freshStopping=travelled;else wornStopping=travelled;}
            check(wornStopping>freshStopping*1.2f,"worn brakes measurably increase stopping distance");ShiftInput.testOverride=false;ShiftInput.testThrottle=0;ShiftInput.testBrake=false;
            Wear.ResetWear();Damage(10);check(Wear.suspension<100&&Condition<100,"impacts damage suspension and vehicle health");Begin();IsolateTraffic();check(Wear.brakes==100&&Wear.suspension==100,"restart restores brakes and suspension");
            Pause(true);check(gameManager.State==GameManager.ShiftState.Paused,"manager publishes paused state");Pause(false);check(gameManager.State==GameManager.ShiftState.Playing,"manager resumes playing state");
            check(waiting[0].Accepts(1,100)&&!waiting[0].Accepts(2,100),"passengers accept affordable fares and reject excessive fares");
            var animator=waiting[0].GetComponent<Animator>();waiting[0].state=PassengerState.Boarding;yield return new WaitForSeconds(.3f);check(animator!=null&&animator.runtimeAnimatorController!=null&&animator.GetBool("Walking"),"boarding drives real Animator walking transition");waiting[0].state=PassengerState.Waiting;
            driver.Body.position=level.station+Vector3.up*.3f;driver.ParkNow();wallet=8;Wear.brakes=20;Condition=50;RepairAtStation();check(wallet==5&&Wear.brakes==55&&Condition==80,"station repairs cost cash and restore condition and wear");driver.Body.position=stops[0]+Vector3.up*.3f;RepairAtStation();check(wallet==5,"workshop service is restricted to station");
            Menu();check(screen==Screen.Menu&&gameManager.State==GameManager.ShiftState.Menu,"replay and return to menu");
            string path=Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,Application.isEditor?"../../Builds/concept-results.json":"../../concept-results.json"));foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--kombi-report="))path=arg.Substring("--kombi-report=".Length);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("KOMBI_SMOKE_COMPLETE "+report.passed.Count+" passed; "+report.failed.Count+" failed; "+path);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-exit")>=0)Application.Quit(report.failed.Count==0?0:1);
        }
    }
}
