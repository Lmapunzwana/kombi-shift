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
        GUIStyle titleStyle, textStyle, smallStyle; bool stylesReady; Coroutine boardingRoutine;
        Color ink = new Color(.078f,.149f,.137f), paper = new Color(.957f,.922f,.847f), yellow = new Color(1,.827f,.306f);
        float Remittance => Rainy ? 12 : 8;
        float ConductorCost => hwindi ? 3 : 0;
        float TakeHome => gross - fuelCost - repairCost - Remittance - ConductorCost;
        Vector3 Target => stops[(stopIndex + (aboard ? 1 : 0)) % stops.Length];
        void Start()
        {
            musicVolume = PlayerPrefs.GetFloat("KombiJayden.Music", .15f); effectsVolume = PlayerPrefs.GetFloat("KombiJayden.Effects", .5f);
            BuildWorld(); screen = Screen.Menu;
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
        Transform Bus(string name, Color color, Transform parent)
        {
            var root = new GameObject(name).transform; root.SetParent(parent,false);
            var body = Mat(color); var dark = Mat(ink); var stripe = Mat(yellow);
            Shape("Body",PrimitiveType.Cube,new Vector3(0,.65f,0),new Vector3(1.8f,1.15f,4.6f),body,root,false);
            Shape("Cabin",PrimitiveType.Cube,new Vector3(0,1.45f,-.1f),new Vector3(1.7f,.8f,3.9f),body,root,false);
            Shape("Windscreen",PrimitiveType.Cube,new Vector3(0,1.5f,1.87f),new Vector3(1.5f,.55f,.04f),dark,root,false);
            Shape("Rear glass",PrimitiveType.Cube,new Vector3(0,1.5f,-2.07f),new Vector3(1.45f,.5f,.035f),dark,root,false);
            Shape("Rear route stripe",PrimitiveType.Cube,new Vector3(0,.8f,-2.32f),new Vector3(1.7f,.22f,.035f),stripe,root,false);
            Shape("Number plate",PrimitiveType.Cube,new Vector3(0,.38f,-2.32f),new Vector3(.6f,.18f,.04f),stripe,root,false);
            var lamp=Mat(new Color(.8f,.12f,.08f));
            for(int side=-1;side<=1;side+=2) {
                Shape("Tail lamp",PrimitiveType.Cube,new Vector3(side*.72f,.5f,-2.32f),new Vector3(.18f,.25f,.04f),lamp,root,false);
                Shape("Route stripe",PrimitiveType.Cube,new Vector3(side*.91f,.8f,0),new Vector3(.03f,.22f,4.3f),stripe,root,false);
                for(int i=0;i<3;i++) Shape("Window",PrimitiveType.Cube,new Vector3(side*.86f,1.5f,-1.3f+i*1.1f),new Vector3(.03f,.48f,.85f),dark,root,false);
                for(int axle=-1;axle<=1;axle+=2) {
                    var w=Shape("Wheel",PrimitiveType.Cylinder,new Vector3(side*.93f,.05f,axle*1.45f),new Vector3(.65f,.16f,.65f),dark,root,false);
                    w.transform.localRotation=Quaternion.Euler(0,0,90); if(name=="Player Kombi") wheels.Add(w.transform);
                }
            }
            return root;
        }
        void BuildWorld()
        {
            world = new GameObject("Generated World").transform; world.SetParent(transform,false);
            var dust=Mat(new Color(.72f,.55f,.39f)); var road=Mat(new Color(.23f,.27f,.26f)); var white=Mat(paper); var teal=Mat(new Color(.086f,.49f,.55f));
            Shape("Ground",PrimitiveType.Cube,new Vector3(0,-.55f,110),new Vector3(150,1,340),dust,world);
            Shape("Road",PrimitiveType.Cube,new Vector3(0,-.03f,110),new Vector3(12,.1f,300),road,world);
            for(int z=-30;z<260;z+=10) Shape("Lane marking",PrimitiveType.Cube,new Vector3(0,.04f,z),new Vector3(.15f,.02f,4),white,world,false);
            var buildingMat=Mat(new Color(.65f,.4f,.29f));
            for(int i=0;i<22;i++) {
                int side=i%2==0?1:-1; float h=3+(i*7%8);
                Shape("City block "+i,PrimitiveType.Cube,new Vector3(side*(15+i%3*5),h/2,i/2*26-20),new Vector3(8,h,10),buildingMat,world);
            }
            for(int i=0;i<stops.Length;i++) {
                Shape("Rank sign pole",PrimitiveType.Cube,stops[i]+new Vector3(-2,1.5f,0),new Vector3(.12f,3,.12f),white,world,false);
                Shape("Rank sign "+(i+1),PrimitiveType.Cube,stops[i]+new Vector3(-2,2.8f,0),new Vector3(1.6f,.7f,.12f),teal,world,false);
            }
            Shape("Fuel station",PrimitiveType.Cube,new Vector3(10,2,110),new Vector3(5,4,8),teal,world);
            Shape("Fuel canopy",PrimitiveType.Cube,new Vector3(7,4,110),new Vector3(10,.4f,10),white,world);
            for(int i=0;i<5;i++) Shape("Pothole",PrimitiveType.Cylinder,new Vector3(i%2==0?-2:2,.035f,50+i*34),new Vector3(1.4f,.015f,2),Mat(ink),world,false);
            var player=Bus("Player Kombi",new Color(.086f,.49f,.55f),world);
            player.position=new Vector3(-3,.6f,0);
            var collider=player.gameObject.AddComponent<BoxCollider>(); collider.center=new Vector3(0,.8f,0); collider.size=new Vector3(1.8f,1.9f,4.6f);
            player.gameObject.AddComponent<Rigidbody>(); driver=player.gameObject.AddComponent<KombiDriver>(); driver.game=this;
            var passenger = new GameObject("Passenger").transform; passenger.SetParent(world,false); rider=passenger;
            Shape("Torso",PrimitiveType.Capsule,new Vector3(0,.9f,0),new Vector3(.45f,.65f,.45f),Mat(new Color(.7f,.25f,.14f)),passenger,false);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.65f,0),Vector3.one*.35f,dust,passenger,false);
            stopMarker=Shape("Current destination",PrimitiveType.Cylinder,Vector3.zero,new Vector3(4,.025f,4),Mat(yellow),world,false).transform;
            var cameraObject=new GameObject("Starter Chase Camera"); cameraObject.transform.SetParent(world,false); chase=cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
            chase.fieldOfView=62; chase.nearClipPlane=.15f; chase.farClipPlane=450; chase.clearFlags=CameraClearFlags.SolidColor; chase.backgroundColor=new Color(.55f,.7f,.7f);
            var lightObject=new GameObject("Starter Sun"); lightObject.transform.SetParent(world,false); sun=lightObject.AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.5f; sun.transform.rotation=Quaternion.Euler(40,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.68f,.65f);
            for(int i=0;i<7;i++) {
                var car=Bus(i==0?"Rival Kombi":"Pooled Traffic "+i,i==0?new Color(.7f,.2f,.12f):new Color(.48f,.52f,.5f),world);
                var c=car.gameObject.AddComponent<BoxCollider>(); c.center=new Vector3(0,.8f,0);c.size=new Vector3(1.8f,1.9f,4.6f);
                var rb=car.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;
                traffic.Add(new Traffic{transform=car,body=rb,speed=6+i*.5f,rival=i==0});
            }
            var rainObject=new GameObject("Rain");rainObject.transform.SetParent(world,false); rain=rainObject.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main; main.startLifetime=1.2f;main.startSpeed=18;main.startSize=.045f;main.maxParticles=1300;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=rain.emission;emission.rateOverTime=800;var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(28,1,35);
            rainObject.transform.rotation=Quaternion.Euler(90,0,0);
            rain.GetComponent<ParticleSystemRenderer>().sharedMaterial=white;
            screen=Screen.Menu; Time.timeScale=0;
            effects=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();effects.spatialBlend=music.spatialBlend=0;
            cue=Tone("Fare cue",660,.13f);warning=Tone("Warning",150,.2f);musicClip=Tone("Shift pulse",110,2);music.clip=musicClip;music.loop=true;music.volume=musicVolume;music.Play();
            ResetTraffic(); PositionPassenger();
        }
        AudioClip Tone(string name,float frequency,float duration)
        {
            const int rate=22050;var samples=new float[Mathf.CeilToInt(rate*duration)];
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate; samples[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*.12f*Mathf.Sin(Mathf.PI*i/samples.Length);}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);clips.Add(clip);return clip;
        }
        void ResetTraffic() { for(int i=0;i<traffic.Count;i++){traffic[i].body.position=new Vector3(i%2==0?3:-3,.6f,35+i*31);traffic[i].body.rotation=Quaternion.Euler(0,i%2==0?180:0,0);traffic[i].state=RivalState.Cruise;} }
        void PositionPassenger(){rider.gameObject.SetActive(!aboard);rider.position=stops[stopIndex]+new Vector3(-1,0,0);stopMarker.position=Target+new Vector3(0,.08f,0);}
        void Begin()
        {
            if(boardingRoutine!=null){StopCoroutine(boardingRoutine);boardingRoutine=null;} Time.timeScale=1; screen=Screen.Playing; Condition=Fuel=100;
            elapsed=gross=fuelCost=repairCost=0;stopIndex=delivered=missed=0;aboard=boarding=false;patience=60;hazardAt=nextRivalDecision=0;
            driver.Body.position=new Vector3(-3,.6f,0);driver.Body.rotation=Quaternion.identity;driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;
            ResetTraffic();PositionPassenger();sun.color=Rainy?new Color(.7f,.8f,1):new Color(1,.92f,.78f);
            if(Rainy)rain.Play();else rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            Notify("Drive to the yellow marker. Stop nearby and press E to board.");
        }
        void Notify(string text){message=text;messageUntil=Time.unscaledTime+5;}
        public void Damage(float amount){Condition=Mathf.Max(0,Condition-amount);repairCost+=amount*.04f;effects.PlayOneShot(warning,effectsVolume);Notify("Vehicle damaged. Slow down around hazards and traffic.");}
        void Update()
        {
            bool pause=false,interact=false,reset=false;
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k!=null){pause=k.escapeKey.wasPressedThisFrame;interact=k.eKey.wasPressedThisFrame;reset=k.rKey.wasPressedThisFrame;}
#elif ENABLE_LEGACY_INPUT_MANAGER
            pause=Input.GetKeyDown(KeyCode.Escape);interact=Input.GetKeyDown(KeyCode.E);reset=Input.GetKeyDown(KeyCode.R);
#endif
            if(pause && (screen==Screen.Playing || screen==Screen.Paused)) Pause(screen==Screen.Playing);
            if(!IsPlaying)return;
            elapsed+=Time.deltaTime;
            float spent=(.025f+driver.Speed*.004f)*(Rainy?1.2f:1)*Time.deltaTime;Fuel=Mathf.Max(0,Fuel-spent);fuelCost+=spent*.08f;
            if(!aboard&&!boarding){patience-=Time.deltaTime;if(patience<=0){missed++;stopIndex=(stopIndex+1)%stops.Length;patience=60;PositionPassenger();Notify("Passenger left. Follow the next marker.");}}
            if(interact&&!boarding)Interact();
            if(reset){driver.Body.position=new Vector3(-3,.7f,Mathf.Clamp(driver.transform.position.z,-15,245));driver.Body.rotation=Quaternion.identity;driver.Body.linearVelocity=Vector3.zero;driver.Body.angularVelocity=Vector3.zero;repairCost+=1;Notify("Recovered to road. Recovery cost $1.");}
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
            boarding=true;Notify("Boarding... stay stopped.");Vector3 from=rider.position;float duration=hwindi?1.3f:2.8f;
            for(float t=0;t<duration;t+=Time.deltaTime){if(!IsPlaying){yield return null;continue;}if(driver.Speed>1.2f){boarding=false;PositionPassenger();Notify("Boarding cancelled: stop moving.");yield break;}rider.position=Vector3.Lerp(from,driver.transform.position+driver.transform.right,t/duration);yield return null;}
            aboard=true;boarding=false;PositionPassenger();effects.PlayOneShot(cue,effectsVolume);Notify("Passenger aboard. Drive to the next yellow marker.");
        }
        void FixedUpdate()
        {
            if(!IsPlaying)return;
            bool decide=Time.time>=nextRivalDecision;if(decide)nextRivalDecision=Time.time+.5f;
            foreach(var car in traffic){
                if(car.rival&&decide){float d=Vector3.Distance(car.transform.position,driver.transform.position);car.state=Condition<25?RivalState.Flee:aboard?RivalState.Cruise:d<9?RivalState.Block:RivalState.ChasePassenger;}
                Vector3 pos=car.body.position;float speed=car.speed;
                if(car.rival){if(car.state==RivalState.Flee)speed*=1.6f;if(car.state==RivalState.Block)speed*=.65f;if(car.state==RivalState.ChasePassenger)speed*=1.2f;}
                if(car.rival) {
                    float lane = car.state==RivalState.Block ? Mathf.Clamp(driver.transform.position.x,-4,4) : car.state==RivalState.ChasePassenger ? -3 : 3;
                    pos.x=Mathf.MoveTowards(pos.x,lane,Time.fixedDeltaTime*1.5f);
                }
                pos+=car.transform.forward*speed*Time.fixedDeltaTime;
                if(pos.z>260)pos.z=-35;if(pos.z< -40)pos.z=255;
                car.body.MovePosition(pos); // Reuse the seven allocated vehicles rather than spawning per lap.
            }
        }
        void LateUpdate()
        {
            if(driver==null)return;
            var t=driver.transform;Vector3 desired=t.position-t.forward*9+Vector3.up*5;
            chase.transform.position=Vector3.Lerp(chase.transform.position,desired,1-Mathf.Exp(-5*Time.unscaledDeltaTime));chase.transform.LookAt(t.position+Vector3.up*1.3f+t.forward*3);
            rain.transform.position=t.position+Vector3.up*14;
            if(IsPlaying)foreach(var wheel in wheels)wheel.Rotate(Vector3.up,driver.Speed*180*Time.deltaTime,Space.Self);
        }
        void Pause(bool paused){screen=paused?Screen.Paused:Screen.Playing;Time.timeScale=paused?0:1;}
        void Finish(string reason){outcome=reason;screen=Screen.Statement;Time.timeScale=0;}
        void Menu(){if(boardingRoutine!=null){StopCoroutine(boardingRoutine);boardingRoutine=null;}boarding=false;screen=Screen.Menu;Time.timeScale=0;}
        void Styles()
        {
            if(stylesReady)return;stylesReady=true;
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=38,fontStyle=FontStyle.Bold,wordWrap=true};titleStyle.normal.textColor=yellow;
            textStyle=new GUIStyle(GUI.skin.label){fontSize=21,wordWrap=true};textStyle.normal.textColor=paper;
            smallStyle=new GUIStyle(textStyle){fontSize=16};
        }
        bool Button(string text){return GUILayout.Button(text,GUILayout.Height(44));}
        void OnGUI()
        {
            Styles();GUI.skin.box.normal.background=Texture2D.whiteTexture;GUI.skin.button.fontSize=20;GUI.skin.toggle.fontSize=18;float scale=Mathf.Min(ScreenWidth/1100f,ScreenHeight/720f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            float w=ScreenWidth/scale,h=ScreenHeight/scale;GUI.backgroundColor=ink;GUI.contentColor=paper;
            if(screen==Screen.Playing){
                GUILayout.BeginArea(new Rect(20,15,w-40,110),GUI.skin.box);GUILayout.Label("KOMBI SHIFT   |   "+(Rainy?"RAINY HIGHWAY":"CITY MORNING")+"   |   $"+gross.ToString("F2"),titleStyle);
                GUILayout.Label("Fuel "+Fuel.ToString("F0")+"%   Condition "+Condition.ToString("F0")+"%   "+(driver.Speed*3.6f).ToString("F0")+" km/h   Time "+Mathf.Max(0,shiftSeconds-elapsed).ToString("F0")+"s   Trips "+delivered,textStyle);GUILayout.EndArea();
                GUILayout.BeginArea(new Rect(20,h-165,500,150),GUI.skin.box);GUILayout.Label(aboard?"DROP OFF AT YELLOW MARKER":"PICKUP AT YELLOW MARKER",textStyle);
                GUILayout.Label("Distance "+Vector3.Distance(driver.transform.position,Target).ToString("F0")+"m  |  "+(aboard?"Passenger aboard":"Patience "+patience.ToString("F0")+"s"),smallStyle);
                GUILayout.Label("Fare $"+fare.ToString("F2")+" (paid x6 per trip)",textStyle);fare=GUILayout.HorizontalSlider(fare,.5f,2f);GUILayout.EndArea();
                GUILayout.BeginArea(new Rect(w-340,h-145,320,130),GUI.skin.box);GUILayout.Label("WASD / arrows: drive\nSpace: brake  E: interact\nEsc: pause  R: recover ($1)\nRival: "+traffic[0].state,smallStyle);GUILayout.EndArea();
                if(Time.unscaledTime<messageUntil)GUI.Label(new Rect(24,140,w-48,90),message,textStyle);return;
            }
            GUILayout.BeginArea(new Rect((w-650)/2,25,650,h-50),GUI.skin.box);GUILayout.Label("KOMBI SHIFT",titleStyle);
            if(screen==Screen.Menu){GUILayout.Label("Your city. Your shift.",textStyle);GUILayout.Label("Generated standalone prototype • Jayden starter",smallStyle);if(Button("START SHIFT"))screen=Screen.Selection;if(Button("HOW TO PLAY"))screen=Screen.Instructions;if(Button("EXIT"))Application.Quit();}
            else if(screen==Screen.Instructions){GUILayout.Label("Drive with WASD or arrow keys. Space brakes. Follow the yellow ground marker. Stop within 7m and press E to board; stay stopped until boarding finishes. Follow the next marker and press E to drop off.\n\nSet your fare with the HUD slider. Fares above $1.60 are refused. Avoid potholes and traffic. E near the teal fuel station refills fuel at a cost. R recovers to the road for $1.\n\nWin by completing at least 3 trips with non-negative take-home before time ends. Fuel or condition reaching zero ends the shift. Esc pauses. Keyboard controls require desktop; mobile controls are not supplied.",textStyle);if(Button("BACK"))screen=Screen.Menu;}
            else if(screen==Screen.Selection){GUILayout.Label("Choose a challenge",textStyle);Rainy=GUILayout.Toggle(Rainy,"  LEVEL 2: rain, lower grip, faster fuel use, $12 remittance");GUILayout.Label(Rainy?"Rainy highway: harder handling and tighter costs.":"LEVEL 1: daytime city, normal grip, $8 remittance.",textStyle);hwindi=GUILayout.Toggle(hwindi,"  Hire hwindi: faster boarding for $3 per shift");if(Button("DRIVE"))Begin();if(Button("BACK"))screen=Screen.Menu;}
            else if(screen==Screen.Paused){GUILayout.Label("PAUSED",textStyle);GUILayout.Label("Music",textStyle);musicVolume=GUILayout.HorizontalSlider(musicVolume,0,1);GUILayout.Label("Effects",textStyle);effectsVolume=GUILayout.HorizontalSlider(effectsVolume,0,1);music.volume=musicVolume;if(Button("RESUME")){SaveSettings();Pause(false);}if(Button("RESTART SHIFT")){SaveSettings();Begin();}if(Button("RETURN TO MENU")){SaveSettings();Menu();}}
            else if(screen==Screen.Statement){GUILayout.Label(outcome,textStyle);GUILayout.Label("Delivered: "+delivered+"   Missed: "+missed+"\nGross fares: $"+gross.ToString("F2")+"\nFuel: -$"+fuelCost.ToString("F2")+"\nDamage / recovery: -$"+repairCost.ToString("F2")+"\nRemittance: -$"+Remittance.ToString("F2")+"\nHwindi: -$"+ConductorCost.ToString("F2"),textStyle);GUILayout.Label("TAKE-HOME  $"+TakeHome.ToString("F2"),titleStyle);if(Button("REPLAY"))Begin();if(Button("LEVEL SELECT")){Time.timeScale=0;screen=Screen.Selection;}if(Button("MAIN MENU"))Menu();}
            GUILayout.EndArea();
        }
        // Qualify UnityEngine.Screen because the prototype state enum is also named Screen.
        float ScreenWidth => UnityEngine.Screen.width; float ScreenHeight => UnityEngine.Screen.height;
        void SaveSettings(){PlayerPrefs.SetFloat("KombiJayden.Music",musicVolume);PlayerPrefs.SetFloat("KombiJayden.Effects",effectsVolume);PlayerPrefs.Save();}
        void OnDestroy(){Time.timeScale=1;foreach(var m in materials)if(m!=null)Destroy(m);foreach(var c in clips)if(c!=null)Destroy(c);}
    }
}
