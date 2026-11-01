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
        readonly Vector3[] stops = { new Vector3(-5,0,25), new Vector3(-5,0,85), new Vector3(-5,0,145), new Vector3(-5,0,205) };
        sealed class Traffic { public Transform transform; public Rigidbody body; public float speed; public RivalState state; public bool rival; }
        float elapsed, gross, fuelCost, repairCost, fare = 1, patience = 60, musicVolume = .15f, effectsVolume = .5f;
        float messageUntil, hazardAt, nextRivalDecision; int stopIndex, delivered, missed; bool aboard, boarding, hwindi; string message = "", outcome = "";
        Coroutine boardingRoutine; float boardingProgress;
        Color ink = new Color(.078f,.149f,.137f), paper = new Color(.957f,.922f,.847f), yellow = new Color(1,.827f,.306f);
        float Remittance => Rainy ? 12 : 8;
        float ConductorCost => hwindi ? 3 : 0;
        float TakeHome => gross - fuelCost - repairCost - Remittance - ConductorCost;
        Vector3 Target => stops[(stopIndex + (aboard ? 1 : 0)) % stops.Length];
        void Start()
        {
            musicVolume = PlayerPrefs.GetFloat("KombiJayden.Music", .15f); effectsVolume = PlayerPrefs.GetFloat("KombiJayden.Effects", .5f);
            Application.runInBackground=true;BuildWorld(); screen = Screen.Menu;Application.targetFrameRate=60;
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
            if (!solid) { var c = g.GetComponent<Collider>(); c.enabled = false; Destroy(c); }
            return g;
        }
        AudioClip Tone(string name,float frequency,float duration)
        {
            const int rate=22050;var samples=new float[Mathf.CeilToInt(rate*duration)];
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate; samples[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*.12f*Mathf.Sin(Mathf.PI*i/samples.Length);}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);clips.Add(clip);return clip;
        }
        void ResetTraffic() { for(int i=0;i<traffic.Count;i++){traffic[i].body.position=new Vector3(i%2==0?3:-3,.225f,35+i*31);traffic[i].body.rotation=Quaternion.Euler(0,i%2==0?180:0,0);traffic[i].state=RivalState.Cruise;} }
        void PositionPassenger(){rider.gameObject.SetActive(!aboard);rider.position=stops[stopIndex]+new Vector3(-2.4f,.22f,0);stopMarker.position=Target+new Vector3(0,.08f,0);}
        void Begin()
        {
            if(boardingRoutine!=null){StopCoroutine(boardingRoutine);boardingRoutine=null;} Time.timeScale=1; screen=Screen.Playing; Condition=Fuel=100;
            elapsed=gross=fuelCost=repairCost=0;stopIndex=delivered=missed=0;aboard=boarding=false;patience=60;hazardAt=nextRivalDecision=0;
            driver.Body.position=new Vector3(-3,.34f,0);driver.Body.rotation=Quaternion.identity;driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;
            ResetTraffic();PositionPassenger();ApplyWeather();
            if(Rainy)rain.Play();else rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ShiftInput.Clear();
            Notify("Drive to the yellow marker. Stop nearby and press E to board.");
        }
        void Notify(string text){message=text;messageUntil=Time.unscaledTime+5;}
        public void Damage(float amount){Condition=Mathf.Max(0,Condition-amount);repairCost+=amount*.04f;effects.PlayOneShot(warning,effectsVolume);Notify("Vehicle damaged. Slow down around hazards and traffic.");}
        void Update()
        {
            bool pause=ShiftInput.Pressed(KeyCode.Escape), interact=ShiftInput.Pressed(KeyCode.E), reset=ShiftInput.Pressed(KeyCode.R);
            if(ShiftInput.Pressed(KeyCode.Return) && (screen==Screen.Menu||screen==Screen.Selection)) Begin();
            if(pause && (screen==Screen.Playing || screen==Screen.Paused)) Pause(screen==Screen.Playing);
            if(!IsPlaying)return;
            UpdateEngineSound();
            if(driver.transform.position.y < -3 || Mathf.Abs(driver.transform.position.x)>65) Recover();
            elapsed+=Time.deltaTime;
            float spent=(.025f+driver.Speed*.004f)*(Rainy?1.2f:1)*Time.deltaTime;Fuel=Mathf.Max(0,Fuel-spent);fuelCost+=spent*.08f;
            if(!aboard&&!boarding){patience-=Time.deltaTime;if(patience<=0){missed++;stopIndex=(stopIndex+1)%stops.Length;patience=60;PositionPassenger();Notify("Passenger left. Follow the next marker.");}}
            if(interact&&!boarding)Interact();
            if(reset) Recover();
            if(Time.time>hazardAt)for(int i=0;i<5;i++)if(Vector3.Distance(driver.transform.position,new Vector3(i%2==0?-2:2,.6f,50+i*34))<2){if(driver.Speed>4)Damage(4+driver.Speed*.3f);hazardAt=Time.time+2;break;}
            if(Fuel<=0||Condition<=0)Finish(Fuel<=0?"Out of fuel":"Vehicle unusable");else if(elapsed>=shiftSeconds)Finish(TakeHome>=0&&delivered>=3?"Shift target met!":"Shift target missed");
        }
        void Interact()
        {
            if(driver.Speed>1.2f){Notify("Stop the kombi before boarding, drop-off or refuelling.");return;}
            if(Vector3.Distance(driver.transform.position,new Vector3(4,.6f,110))<7){Fuel=100;effects.PlayOneShot(cue,effectsVolume);Notify("Refuelled. Fuel costs are charged as fuel is used.");return;}
            if(Vector3.Distance(driver.transform.position,Target)>7){Notify("Move within 7 metres of the yellow marker.");return;}
            if(aboard){float payment=fare*6;gross+=payment;delivered++;aboard=false;stopIndex=(stopIndex+1)%stops.Length;patience=60;PositionPassenger();FareEarned?.Invoke(payment);effects.PlayOneShot(cue,effectsVolume);Notify("Passenger delivered: +$"+payment.ToString("F2")+". Next passenger is ready.");}
            else if(fare>1.6f||Condition<20){Notify("Passenger refuses: reduce fare below $1.60 and keep condition above 20%.");}
            else boardingRoutine=StartCoroutine(Board());
        }
        IEnumerator Board()
        {
            boarding=true;Notify("Boarding... stay stopped.");Vector3 from=rider.position;float duration=hwindi?1.3f:2.8f;boardingProgress=0;
            for(float t=0;t<duration;t+=Time.deltaTime){if(!IsPlaying){yield return null;continue;}if(driver.Speed>1.2f){boarding=false;PositionPassenger();Notify("Boarding cancelled: stop moving.");yield break;}boardingProgress=t/duration;rider.position=Vector3.Lerp(from,driver.transform.position-driver.transform.right*1.2f,t/duration);yield return null;}
            aboard=true;boarding=false;PositionPassenger();effects.PlayOneShot(cue,effectsVolume);Notify("Passenger aboard. Drive to the next yellow marker.");
        }
        void FixedUpdate()
        {
            if(!IsPlaying){if(engine!=null)engine.volume=0;return;}
            bool decide=Time.time>=nextRivalDecision;if(decide)nextRivalDecision=Time.time+.5f;
            foreach(var car in traffic){
                if(car.rival&&decide){float d=Vector3.Distance(car.transform.position,driver.transform.position);car.state=Condition<25?RivalState.Flee:aboard?RivalState.Cruise:d<9?RivalState.Block:RivalState.ChasePassenger;}
                Vector3 pos=car.body.position;float speed=car.speed;
                if(car.rival){if(car.state==RivalState.Flee)speed*=1.6f;if(car.state==RivalState.Block)speed*=.65f;if(car.state==RivalState.ChasePassenger)speed*=1.2f;}
                if(car.rival) {
                    float lane = car.state==RivalState.Block ? Mathf.Clamp(driver.transform.position.x,-4,4) : car.state==RivalState.ChasePassenger ? -3 : 3;
                    pos.x=Mathf.MoveTowards(pos.x,lane,Time.fixedDeltaTime*1.5f);
                }
                if(Physics.Raycast(pos+Vector3.up*.8f+car.transform.forward*2.5f,car.transform.forward,out var obstruction,5,~0,QueryTriggerInteraction.Ignore)) speed=0;
                pos+=car.transform.forward*speed*Time.fixedDeltaTime;
                if(pos.z>260)pos.z=-35;if(pos.z< -40)pos.z=255;
                car.body.MovePosition(pos); // Reuse the seven allocated vehicles rather than spawning per lap.
            }
        }
        void LateUpdate()
        {
            if(driver==null)return;
            var t=driver.transform;bool cinematic=screen==Screen.Menu||screen==Screen.Selection||screen==Screen.Instructions;
            Vector3 desired=cinematic?t.position+new Vector3(-7,3.3f,-7):t.position-t.forward*8.8f+Vector3.up*4.1f;
            chase.transform.position=Vector3.Lerp(chase.transform.position,desired,1-Mathf.Exp(-5*Time.unscaledDeltaTime));chase.transform.LookAt(cinematic?t.position+Vector3.left*3+Vector3.up:t.position+Vector3.up*1.2f+t.forward*3);
            rain.transform.position=t.position+Vector3.up*14;
            if(IsPlaying)foreach(var wheel in wheels)wheel.Rotate(Vector3.up,driver.SignedSpeed*180*Time.deltaTime,Space.Self);
            AnimateObjective();
        }
        void Pause(bool paused){screen=paused?Screen.Paused:Screen.Playing;Time.timeScale=paused?0:1;ShiftInput.Clear();if(paused&&engine!=null)engine.volume=0;}
        void Finish(string reason){outcome=reason;screen=Screen.Statement;Time.timeScale=0;if(engine!=null)engine.volume=0;}
        void Recover(){driver.Body.position=new Vector3(-3,.34f,Mathf.Clamp(driver.transform.position.z,-15,245));driver.Body.rotation=Quaternion.identity;driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;repairCost+=1;Notify("Back on the road. Recovery $1.");}
        void Menu(){if(boardingRoutine!=null){StopCoroutine(boardingRoutine);boardingRoutine=null;}boarding=false;screen=Screen.Menu;Time.timeScale=0;if(engine!=null)engine.volume=0;}
        void OnApplicationFocus(bool focused)
        {
            if(focused||!IsPlaying||Application.isBatchMode)return;
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--kombi-"))return;
            Pause(true);
        }
        void SaveSettings(){PlayerPrefs.SetFloat("KombiJayden.Music",musicVolume);PlayerPrefs.SetFloat("KombiJayden.Effects",effectsVolume);PlayerPrefs.Save();}
        void OnDestroy(){Font.textureRebuilt-=RefreshLabelAtlas;Time.timeScale=1;ShiftInput.Clear();foreach(var mesh in generatedMeshes)if(mesh!=null)Destroy(mesh);foreach(var texture in generatedTextures)if(texture!=null)Destroy(texture);foreach(var m in materials)if(m!=null)Destroy(m);foreach(var c in clips)if(c!=null)Destroy(c);}
    }
}
