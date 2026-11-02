using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace KombiShift.JaydenStarter
{
    // Isolated, generated prototype. Does not replace the team's production managers.
    public sealed partial class ShiftGame : MonoBehaviour
    {
        public Material surfaceTemplate;
        [Min(30)] public float shiftSeconds = 240;
        public event Action<float> FareEarned;
        enum Screen { Menu, Instructions, Selection, Playing, Paused, Statement }
        enum RivalState { Cruise, ChasePassenger, Block, Flee }
        Screen screen;
        public bool IsPlaying => screen == Screen.Playing;
        public bool DriveBlocked => boarding||unloading||Time.time<hwindiReadyAt||(hwindiDepartureRequested&&doorAmount>.04f);
        public bool Rainy { get; private set; }
        public float Condition { get; private set; } = 100;
        public float Fuel { get; private set; } = 100;
        KombiDriver driver; Camera chase; Transform world, rider, stopMarker;
        ParticleSystem rain; Light sun; AudioSource effects, music;
        AudioClip cue, warning, musicClip;
        readonly List<Material> materials = new List<Material>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly List<Transform> wheels = new List<Transform>();
        readonly List<Traffic> traffic = new List<Traffic>();
        Vector3[] stops=LevelDefinition.City().stops;
        sealed class Traffic { public Transform transform; public Rigidbody body; public float speed; public RivalState state; public bool rival; public AudioSource sound; public float dwellUntil; public int nextStop; public bool atStop; }
        float elapsed, gross, fuelCost, repairCost, fare = 1, patience = 60, musicVolume = .15f, effectsVolume = .5f;
        float messageUntil, hazardAt, nextRivalDecision; int stopIndex, delivered, missed; bool aboard, boarding; bool hwindi=true; bool unloading; string message = "", outcome = "";
        Coroutine rainRoutine;
        Coroutine boardingRoutine; float boardingProgress;
        Color ink = new Color(.14f,.15f,.15f), paper = new Color(.91f,.91f,.88f), yellow = new Color(.72f,.73f,.68f);
        float Remittance => Rainy ? 12 : 8;
        float ConductorCost => hwindi ? 3 : 0;
        float TakeHome => gross - fuelCost - repairCost - Remittance - ConductorCost;
        Vector3 Target=>stops[Mathf.Clamp(stopIndex,0,stops.Length-1)];
        void Start()
        {
            musicVolume = PlayerPrefs.GetFloat("KombiJayden.Music.v2", .12f); effectsVolume = PlayerPrefs.GetFloat("KombiJayden.Effects.v2", .75f);
            Application.runInBackground=true;BuildWorld();InitializeSystems();screen=Screen.Menu;Application.targetFrameRate=60;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-profile")>=0)StartCoroutine(RunPerformanceComparison());
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--kombi-input-probe")>=0)StartCoroutine(ProbePhysicalInput());
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--kombi-smoke") >= 0) StartCoroutine(RunSmokeCheck());
            foreach(var arg in Environment.GetCommandLineArgs()) if(arg.StartsWith("--kombi-capture-dir=")) StartCoroutine(CapturePreview(arg.Substring("--kombi-capture-dir=".Length)));
        }
        Material Mat(Color color, float smoothness = .1f)
        {
            Shader shader = surfaceTemplate != null ? surfaceTemplate.shader : Shader.Find(GraphicsSettings.currentRenderPipeline == null ? "Standard" : GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : "Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Use Kombi Shift > Jayden > Create Starter Scene after configuring the render pipeline.");
            var m = surfaceTemplate != null ? new Material(surfaceTemplate) : new Material(shader);
            if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if(m.HasProperty("_Color")) m.SetColor("_Color", color);
            if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if(m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            materials.Add(m); return m;
        }
        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid = true)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = position; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) { var c = g.GetComponent<Collider>(); if(c!=null){c.enabled = false; Destroy(c);} }
            return g;
        }
        AudioClip Tone(string name,float frequency,float duration)
        {
            const int rate=22050;var samples=new float[Mathf.CeilToInt(rate*duration)];
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate; samples[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*.12f*Mathf.Sin(Mathf.PI*i/samples.Length);}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);clips.Add(clip);return clip;
        }
        int NextRank(float z,int direction)
        {
            int best=direction>0?0:stops.Length-1;float distance=float.MaxValue;
            for(int i=0;i<stops.Length;i++){float d=(stops[i].z-z)*direction;if(d>3&&d<distance){distance=d;best=i;}}
            return best;
        }
        void ResetTraffic()
        {
            for(int i=0;i<traffic.Count;i++){var car=traffic[i];car.body.position=new Vector3(i%2==0?3:-3,.225f,i==0?-25:35+i*31);car.body.rotation=Quaternion.Euler(0,i%2==0?180:0,0);car.state=RivalState.Cruise;car.atStop=false;car.dwellUntil=0;car.nextStop=NextRank(car.body.position.z,i%2==0?-1:1);}
        }
        void PositionPassenger()
        {
            if(gameManager==null){rider.position=stops[0]+new Vector3(-2.4f,.22f,0);return;}
            foreach(var p in waiting)if(p.origin==stopIndex&&p.state==PassengerState.Waiting){rider=p.transform;break;}
            stopMarker.position=Target+Vector3.up*.08f;
        }
        void Begin()
        {
            if(boardingRoutine!=null){StopCoroutine(boardingRoutine);boardingRoutine=null;} Time.timeScale=1; screen=Screen.Playing; Condition=Fuel=100;
            elapsed=gross=fuelCost=repairCost=0;stopIndex=delivered=missed=0;aboard=boarding=unloading=false;hwindiReadyAt=0;patience=60;hazardAt=nextRivalDecision=0;
            driver.Body.position=new Vector3(-3,.34f,0);driver.Body.rotation=Quaternion.identity;driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;
            driver.ParkNow();ConfigureLevel();ResetTraffic();rival.Configure(this,roadGraph,level.aggression,Rainy);tipsTotal=noCrashBonus=0;crashCount=0;stopIndex=BestStop();PositionPassenger();ResetHwindi();ApplyWeather();
            if(rainRoutine!=null)StopCoroutine(rainRoutine);if(Rainy)rainRoutine=StartCoroutine(StartRain());else rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ShiftInput.Clear();
            Notify("Pull into the stop bay. Press Space to hold, then E to board.");
        }
        IEnumerator StartRain(){rain.Play();var emission=rain.emission;for(float t=0;t<2;t+=Time.deltaTime){emission.rateOverTime=650*t/2;yield return null;}emission.rateOverTime=650;}
        void Notify(string text){message=text;messageUntil=Time.unscaledTime+5;}
        public void Damage(float amount){crashCount++;if(gameManager!=null)gameManager.PublishDamage(amount);Condition=Mathf.Max(0,Condition-amount);repairCost+=amount*.04f;effects.PlayOneShot(warning,effectsVolume);Notify("Vehicle damaged. Slow down around hazards and traffic.");}
        void Update()
        {
            bool pause=ShiftInput.Pressed(KeyCode.Escape), interact=ShiftInput.Pressed(KeyCode.E), reset=ShiftInput.Pressed(KeyCode.R);
            if(ShiftInput.Pressed(KeyCode.Return) && (screen==Screen.Menu||screen==Screen.Selection)) Begin();
            if(pause && (screen==Screen.Playing || screen==Screen.Paused)) Pause(screen==Screen.Playing);
            if(!IsPlaying){if(engine!=null)UpdateEngineSound();return;}
            UpdateEngineSound();
            if(ShiftInput.Pressed(KeyCode.H))PlayHorn();
            if(driver.transform.position.y < -3 || Mathf.Abs(driver.transform.position.x)>65) Recover();
            elapsed+=Time.deltaTime;
            UpdatePassengersAndFuel();
            if(interact&&!boarding&&!unloading)Interact();
            if(reset) Recover();
            string result=gameManager.Evaluate(gross,Fuel,Condition,elapsed);if(result!=null)Finish(result);
        }
        void Interact()=>ServiceStop();
        IEnumerator Board(){yield return BoardPassengers();}
        IEnumerator DropOff(){yield return DeliverPassengers();}
        void FixedUpdate()
        {
            if(!IsPlaying){if(engine!=null)engine.volume=0;return;}
            bool decide=Time.time>=nextRivalDecision;if(decide)nextRivalDecision=Time.time+.5f;
            foreach(var car in traffic){
                if(car.rival||!car.transform.gameObject.activeSelf)continue;
                Vector3 pos=car.body.position;int direction=car.transform.forward.z>0?1:-1;float baseLane=direction>0?-3:3;
                if(car.atStop){
                    if(Time.time<car.dwellUntil){car.body.MovePosition(pos);continue;}
                    car.atStop=false;car.nextStop=NextRank(pos.z,direction);
                }
                float toRank=(stops[car.nextStop].z-pos.z)*direction;bool approaching=toRank>=0&&toRank<18;
                if(car.rival&&decide){float d=Vector3.Distance(pos,driver.transform.position);car.state=Condition<25?RivalState.Flee:approaching?RivalState.ChasePassenger:d<10?RivalState.Block:RivalState.Cruise;}
                float speed=car.speed*(car.rival&&car.state==RivalState.Flee?1.25f:1);
                pos.x=Mathf.MoveTowards(pos.x,approaching?(direction>0?-4.55f:4.55f):baseLane,Time.fixedDeltaTime*.8f);
                if(approaching)speed=Mathf.Min(speed,toRank<8?2.4f:4);
                if(approaching&&toRank<.75f){car.atStop=true;car.dwellUntil=Time.time+(car.rival?4:6);if(car.sound!=null)car.sound.PlayOneShot(doorClip,.4f);continue;}
                if(Physics.SphereCast(pos+Vector3.up*.8f+car.transform.forward*2.7f,.65f,car.transform.forward,out var obstruction,5,~0,QueryTriggerInteraction.Ignore))speed=0;
                pos+=car.transform.forward*speed*Time.fixedDeltaTime;
                if(pos.z>(Rainy?535:260)){pos.z=-35;car.nextStop=0;}if(pos.z< -40){pos.z=Rainy?525:255;car.nextStop=stops.Length-1;}
                car.body.MovePosition(pos);
            }
        }
        void LateUpdate()
        {
            if(driver==null)return;
            var t=driver.transform;bool cinematic=screen==Screen.Menu||screen==Screen.Selection||screen==Screen.Instructions;
            float shoulder=driver.IsParked&&Vector3.Distance(t.position,Target)<16?4f:1.1f;
            Vector3 desired=cinematic?t.position+new Vector3(-7,3.3f,-7):t.position-t.forward*8-t.right*shoulder+Vector3.up*3.6f;
            chase.transform.position=Vector3.Lerp(chase.transform.position,desired,1-Mathf.Exp(-5*Time.unscaledDeltaTime));chase.transform.LookAt(cinematic?t.position+Vector3.left*3+Vector3.up:t.position+Vector3.up*1.2f+t.forward*3);
            rain.transform.position=t.position+Vector3.up*14;
            if(IsPlaying)foreach(var wheel in wheels)wheel.Rotate(Vector3.up,driver.SignedSpeed*180*Time.deltaTime,Space.Self);
            AnimateObjective();UpdateHwindi();
        }
        void Pause(bool paused){screen=paused?Screen.Paused:Screen.Playing;Time.timeScale=paused?0:1;ShiftInput.Clear();if(paused&&engine!=null)engine.volume=0;if(hwindiVoice!=null){if(paused)hwindiVoice.Pause();else hwindiVoice.UnPause();}}
        void Finish(string reason){if(gameManager!=null&&gameManager.Ended)return;noCrashBonus=crashCount==0?5:0;if(gameManager!=null)gameManager.End(reason);outcome=reason;screen=Screen.Statement;Time.timeScale=0;if(engine!=null)engine.volume=0;}
        void Recover(){driver.Body.position=new Vector3(-3,.34f,Mathf.Clamp(driver.transform.position.z,-15,Rainy?520:245));driver.Body.rotation=Quaternion.identity;driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;repairCost+=1;wallet=Mathf.Max(0,wallet-1);fuelPreviousPosition=driver.transform.position;Notify("Back on the road. Recovery $1.");}
        void Menu(){if(boardingRoutine!=null){StopCoroutine(boardingRoutine);boardingRoutine=null;}boarding=unloading=false;screen=Screen.Menu;Time.timeScale=0;if(engine!=null)engine.volume=0;}
        void OnApplicationFocus(bool focused)
        {
            if(focused||!IsPlaying||Application.isBatchMode)return;
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--kombi-"))return;
            Pause(true);
        }
        void SaveSettings(){if(uiManager!=null)uiManager.Save(hudTextScale,touchControls);PlayerPrefs.SetFloat("KombiJayden.Music.v2",musicVolume);PlayerPrefs.SetFloat("KombiJayden.Effects.v2",effectsVolume);PlayerPrefs.Save();}
        void OnDestroy(){Font.textureRebuilt-=RefreshLabelAtlas;Time.timeScale=1;ShiftInput.Clear();foreach(var mesh in generatedMeshes)if(mesh!=null)Destroy(mesh);foreach(var texture in generatedTextures)if(texture!=null)Destroy(texture);foreach(var m in materials)if(m!=null)Destroy(m);foreach(var c in clips)if(c!=null)Destroy(c);}
    }
}
