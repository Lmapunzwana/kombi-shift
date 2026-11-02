using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        [Serializable] sealed class PerfResult{public string mode;public int samples,traffic,passengers,hazards;public float meanFrameMs,p95FrameMs,meanMainThreadMs,meanGCBytes;public bool gcCounterValid,threadCounterValid;public string route="CBD x=-3 z=0..100, fixed 60 Hz simulation; identical traffic density";public string hardware="Intel UHD Graphics 620; Windows 11; Unity 6000.6.2f1; development player";}
        IEnumerator RunPerformanceComparison()
        {
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Builds/ProfilingEvidence"));Directory.CreateDirectory(dir);var summaries=new List<PerfResult>();Time.captureFramerate=60;
            foreach(bool pooling in new[]{false,true}){
                spawnManager.UsePooling=pooling;Rainy=false;Begin();rival.enabled=false;
                for(int i=0;i<30;i++)yield return null;
                string mode=pooling?"pooled":"baseline";
                Profiler.logFile=Path.Combine(dir,mode+".raw");Profiler.enableBinaryLog=true;Profiler.enabled=true;
                var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");var thread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");
                var frames=new List<double>();double gcSum=0,cpuSum=0;var clock=Stopwatch.StartNew();long previous=clock.ElapsedTicks;
                var csv=new System.Text.StringBuilder("frame,wall_ms,gc_bytes,main_thread_ms\n");
                for(int frame=0;frame<240;frame++){
                    // Same route positions, simulation step, objects and recycling rate in both passes.
                    driver.Body.position=new Vector3(-3,.23f,frame*100f/239);driver.Body.rotation=Quaternion.identity;driver.ParkNow();fuelPreviousPosition=driver.Body.position;
                    var old=waiting[0];int origin=old.origin,destination=old.destination;float remaining=old.remaining,distance=old.routeDistance;Vector3 position=old.transform.position;
                    waiting.RemoveAt(0);spawnManager.Release("passengers",old.gameObject);var next=spawnManager.Acquire("passengers").GetComponent<PassengerAI>();foreach(var renderer in next.GetComponentsInChildren<Renderer>())renderer.enabled=true;next.Configure(this,origin,destination,distance,remaining);next.transform.position=position;waiting.Add(next);
                    var h=hazards[0];Vector3 hp=h.transform.position;spawnManager.Release("potholes",h);h=spawnManager.Acquire("potholes");h.transform.position=hp;h.GetComponent<Pothole>().Configure(this,7);hazards[0]=h;
                    yield return null;
                    long now=clock.ElapsedTicks;double ms=(now-previous)*1000d/Stopwatch.Frequency;previous=now;frames.Add(ms);long bytes=gc.Valid?gc.LastValue:0;double cpu=thread.Valid?thread.LastValue*1e-6:0;gcSum+=bytes;cpuSum+=cpu;csv.Append(frame).Append(',').Append(ms.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(bytes).Append(',').Append(cpu.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
                }
                Profiler.enabled=false;Profiler.logFile="";double total=0;foreach(double f in frames)total+=f;frames.Sort();summaries.Add(new PerfResult{mode=mode,samples=frames.Count,traffic=level.trafficCount,passengers=waiting.Count,hazards=hazards.Count,meanFrameMs=(float)(total/frames.Count),p95FrameMs=(float)frames[(int)(frames.Count*.95f)],meanGCBytes=(float)(gcSum/frames.Count),meanMainThreadMs=(float)(cpuSum/frames.Count),gcCounterValid=gc.Valid,threadCounterValid=thread.Valid});gc.Dispose();thread.Dispose();File.WriteAllText(Path.Combine(dir,mode+"-frames.csv"),csv.ToString());
            }
            Time.captureFramerate=0;spawnManager.UsePooling=true;File.WriteAllText(Path.Combine(dir,"comparison.json"),"{\"unityVersion\":\""+Application.unityVersion+"\",\"fixture\":\"controlled pedestrian and pothole churn, not human playtesting\",\"baseline\":"+JsonUtility.ToJson(summaries[0])+",\"pooled\":"+JsonUtility.ToJson(summaries[1])+"}");UnityEngine.Debug.Log("KOMBI_PROFILE_COMPLETE "+dir);Application.Quit();
        }
    }
}
