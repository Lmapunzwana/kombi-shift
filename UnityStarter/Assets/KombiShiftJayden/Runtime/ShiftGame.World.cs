using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        readonly List<Mesh> generatedMeshes=new List<Mesh>();
        readonly List<Texture2D> generatedTextures=new List<Texture2D>();
        readonly Dictionary<string,Mesh> roundedMeshes=new Dictionary<string,Mesh>();
        readonly string[] stopNames={"Market Square","Central Avenue","Mbare Link","Eastgate Rank"};
        Font worldFont; Material worldLabelMaterial; AudioSource engine; Transform passengerArmL,passengerArmR; Material roadSurface;
        Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        Transform Group(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        GameObject Box(string name,Vector3 position,Vector3 size,Material m,Transform parent,bool solid=false){return Shape(name,PrimitiveType.Cube,position,size,m,parent,solid);}
        void Label(string value,Transform parent,Vector3 position,float size,Color color,Quaternion rotation)
        {
            worldFont.RequestCharactersInTexture(value,64,FontStyle.Normal);
            var t=Group(value,parent,position);t.localRotation=rotation;
            var text=t.gameObject.AddComponent<TextMesh>();text.text=value;text.font=worldFont;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;
            worldLabelMaterial.mainTexture=worldFont.material.mainTexture;t.GetComponent<MeshRenderer>().sharedMaterial=worldLabelMaterial;t.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        GameObject Rounded(string name,Vector3 position,Vector3 size,float radius,Material m,Transform parent)
        {
            string key=size.ToString("F3")+radius;
            if(!roundedMeshes.TryGetValue(key,out var mesh)){
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
                Vector3[] face={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
                Vector3[] along={Vector3.forward,Vector3.back,Vector3.right,Vector3.right,Vector3.right,Vector3.left};
                Vector3[] vertical={Vector3.up,Vector3.up,Vector3.back,Vector3.forward,Vector3.up,Vector3.up};
                Vector3 half=size*.5f,inner=half-Vector3.one*radius;const int segments=4;
                for(int f=0;f<6;f++){
                    int offset=vertices.Count;
                    for(int y=0;y<=segments;y++)for(int x=0;x<=segments;x++){
                        Vector3 p=Vector3.Scale(face[f],half)+Vector3.Scale(along[f],half)*(x*2f/segments-1)+Vector3.Scale(vertical[f],half)*(y*2f/segments-1);
                        Vector3 core=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                        Vector3 n=(p-core).normalized;vertices.Add(core+n*radius);normals.Add(n);
                    }
                    bool flip=Vector3.Dot(Vector3.Cross(along[f],vertical[f]),face[f])<0;
                    for(int y=0;y<segments;y++)for(int x=0;x<segments;x++){
                        int a=offset+y*(segments+1)+x,b=a+1,c=a+segments+1,d=c+1;
                        if(flip)triangles.AddRange(new[]{a,c,b,b,c,d});else triangles.AddRange(new[]{a,b,c,b,d,c});
                    }
                }
                mesh=new Mesh{name="Rounded box "+key};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();generatedMeshes.Add(mesh);roundedMeshes[key]=mesh;
            }
            var g=Group(name,parent,position).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;return g;
        }
        Material Grain(Material m,float tilesX,float tilesY)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGB24,false){name="Procedural aggregate",wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float n=.76f+Mathf.PerlinNoise(x*1.13f,y*1.17f)*.35f;pixels[y*64+x]=new Color(n,n,n);}
            texture.SetPixels(pixels);texture.Apply();generatedTextures.Add(texture);
            if(m.HasProperty("_MainTex")){m.SetTexture("_MainTex",texture);m.SetTextureScale("_MainTex",new Vector2(tilesX,tilesY));}
            if(m.HasProperty("_BaseColorMap"))m.SetTexture("_BaseColorMap",texture);return m;
        }
        Transform Bus(string name,Color color,Transform parent)
        {
            var root=Group(name,parent,Vector3.zero);var body=Mat(color,.3f);var cream=Mat(C("E7E2D2"),.25f);var glass=Mat(C("142E38"),.65f);var rubber=Mat(C("182023"));var trim=Mat(C("687478"),.55f);var stripe=Mat(C("E9B746"));var red=Mat(C("B92E28"));
            Rounded("Lower body",new Vector3(0,.7f,0),new Vector3(1.86f,1.25f,4.55f),.14f,body,root);
            Rounded("Cream roof",new Vector3(0,1.52f,-.12f),new Vector3(1.8f,.94f,4.03f),.14f,cream,root);
            Rounded("Front windscreen",new Vector3(0,1.52f,1.905f),new Vector3(1.54f,.61f,.045f),.02f,glass,root);
            Rounded("Rear windscreen",new Vector3(0,1.54f,-2.155f),new Vector3(1.51f,.55f,.045f),.02f,glass,root);
            Box("Rear route stripe",new Vector3(0,.87f,-2.29f),new Vector3(1.74f,.18f,.035f),stripe,root);
            Box("Rear bumper",new Vector3(0,.23f,-2.34f),new Vector3(1.9f,.15f,.14f),rubber,root);
            Box("Front bumper",new Vector3(0,.23f,2.34f),new Vector3(1.9f,.15f,.14f),rubber,root);
            Box("Front grille",new Vector3(0,.53f,2.29f),new Vector3(.85f,.24f,.04f),rubber,root);
            Box("Rear number plate",new Vector3(0,.42f,-2.29f),new Vector3(.69f,.2f,.04f),stripe,root);
            Label("ABX 0426",root,new Vector3(0,.42f,-2.318f),.025f,C("162C31"),Quaternion.identity);
            Label(name=="Player Kombi"?"CITYLINK":"COMMUTER",root,new Vector3(0,1.58f,-2.185f),.033f,C("F4EBD8"),Quaternion.identity);
            Label("HARARE • CITY RANK",root,new Vector3(0,1.82f,-2.18f),.018f,C("E9B746"),Quaternion.identity);
            for(int side=-1;side<=1;side+=2){
                Box("Tail lights",new Vector3(side*.73f,.49f,-2.29f),new Vector3(.16f,.31f,.035f),red,root);
                Box("Headlights",new Vector3(side*.66f,.63f,2.29f),new Vector3(.35f,.2f,.05f),cream,root);
                Box("Livery",new Vector3(side*.94f,.88f,0),new Vector3(.024f,.18f,4.25f),stripe,root);
                Box("Window rail",new Vector3(side*.912f,1.16f,-.16f),new Vector3(.022f,.04f,3.8f),rubber,root);
                for(int i=0;i<4;i++)Rounded("Side window",new Vector3(side*.911f,1.51f,-1.55f+i*.9f),new Vector3(.035f,.54f,.75f),.015f,glass,root);
                Box("Door seam",new Vector3(side*.944f,.7f,.95f),new Vector3(.025f,1.05f,.025f),rubber,root);
                Box("Door handle",new Vector3(side*.963f,1.04f,.64f),new Vector3(.045f,.045f,.18f),trim,root);
                Box("Mirror arm",new Vector3(side*1.04f,1.45f,1.75f),new Vector3(.3f,.05f,.06f),trim,root);
                Rounded("Side mirror",new Vector3(side*1.18f,1.49f,1.72f),new Vector3(.16f,.27f,.14f),.035f,rubber,root);
                for(int axle=-1;axle<=1;axle+=2){
                    var wheel=Shape("Wheel",PrimitiveType.Cylinder,new Vector3(side*.94f,.06f,axle*1.43f),new Vector3(.66f,.16f,.66f),rubber,root,false);wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                    var hub=Shape("Wheel hub",PrimitiveType.Cylinder,new Vector3(side*1.11f,.06f,axle*1.43f),new Vector3(.35f,.012f,.35f),trim,root,false);hub.transform.localRotation=Quaternion.Euler(0,0,90);
                    if(name=="Player Kombi")wheels.Add(wheel.transform);
                }
            }
            return root;
        }
        void Building(int side,int index,Material facade,Material dark,Material trim,Material glass,Material awning)
        {
            float z=-18+index*24;float height=5+(index%4)*2.6f;var block=Group("Shop block "+side+" "+index,world,new Vector3(side*16,0,z));
            Box("Masonry",new Vector3(0,height/2,0),new Vector3(10,height,18),facade,block,true);
            Box("Roof cornice",new Vector3(0,height-.08f,0),new Vector3(10.35f,.28f,18.35f),trim,block);
            Box("Roof",new Vector3(0,height+.1f,0),new Vector3(9.6f,.15f,17.5f),dark,block);
            Box("Parapet",new Vector3(side*-5.08f,height+.25f,0),new Vector3(.18f,.7f,18.3f),facade,block);
            for(int f=0;f<Mathf.FloorToInt((height-2.7f)/2.6f);f++)for(int col=0;col<5;col++){
                float y=4.2f+f*2.6f,zz=-6.6f+col*3.3f;
                Box("Window frame",new Vector3(-side*5.05f,y,zz),new Vector3(.13f,1.65f,2.25f),trim,block);
                Box("Window glass",new Vector3(-side*5.13f,y,zz),new Vector3(.035f,1.39f,2.02f),glass,block);
                Box("Mullion",new Vector3(-side*5.17f,y,zz),new Vector3(.04f,1.4f,.055f),trim,block);
            }
            string[] names={"MUSIKA MARKET","CITY PHARMACY","RANK CAFE","CHIEDZA STORES","KOMBI PARTS","FRESH PRODUCE","MOBILE & MORE","PRINT & COPY","MATOBO FOODS","CITY HARDWARE","BAKERY"};
            Box("Shop fascia",new Vector3(-side*5.17f,2.85f,0),new Vector3(.22f,.76f,16.5f),awning,block);
            Label(names[(index+(side>0?3:0))%names.Length],block,new Vector3(-side*5.31f,2.88f,0),.08f,C("F2E9D8"),Quaternion.Euler(0,side>0?90:-90,0));
            for(int i=0;i<3;i++){
                float zz=-5.4f+i*5.4f;
                Box("Shop glazing",new Vector3(-side*5.07f,1.25f,zz),new Vector3(.12f,2.05f,4.5f),glass,block);
                Box("Shop post",new Vector3(-side*5.2f,1.2f,zz-2.35f),new Vector3(.22f,2.4f,.22f),trim,block);
                for(int stripe=0;stripe<8;stripe++)Box("Awning stripe",new Vector3(-side*5.75f,2.36f,zz-2.1f+stripe*.6f),new Vector3(1.55f,.12f,.6f),stripe%2==0?awning:trim,block);
            }
            Box("Entrance door",new Vector3(-side*5.18f,1.13f,0),new Vector3(.16f,2.2f,1.3f),dark,block);
            Box("Door handle",new Vector3(-side*5.28f,1.1f,.45f),new Vector3(.08f,.35f,.06f),trim,block);
            if(index%2==0){Box("AC unit",new Vector3(-side*5.17f,3.6f,7),new Vector3(.5f,.7f,1.1f),trim,block);}
        }
        void Tree(Vector3 position,Material bark,Material leaves)
        {
            var tree=Group("Street tree",world,position);Shape("Trunk",PrimitiveType.Cylinder,new Vector3(0,1.45f,0),new Vector3(.22f,1.45f,.22f),bark,tree,false);
            Shape("Canopy",PrimitiveType.Sphere,new Vector3(0,3.6f,0),new Vector3(2.9f,3.2f,2.7f),leaves,tree,false);
            Shape("Canopy cluster",PrimitiveType.Sphere,new Vector3(.65f,3.4f,.3f),new Vector3(2.4f,2.3f,2.6f),leaves,tree,false);
        }
        Transform Person(string name,Vector3 position,Color shirt,Material skin)
        {
            var p=Group(name,world,position);var clothes=Mat(shirt);var trousers=Mat(C("283744"));var shoe=Mat(C("242629"));
            Shape("Torso",PrimitiveType.Capsule,new Vector3(0,1.04f,0),new Vector3(.42f,.34f,.3f),clothes,p,false);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.64f,0),new Vector3(.3f,.36f,.3f),skin,p,false);
            Shape("Hair",PrimitiveType.Sphere,new Vector3(0,1.77f,-.015f),new Vector3(.3f,.17f,.29f),shoe,p,false);
            for(int side=-1;side<=1;side+=2){
                Shape("Leg",PrimitiveType.Capsule,new Vector3(side*.115f,.42f,0),new Vector3(.14f,.34f,.16f),trousers,p,false);
                Box("Shoe",new Vector3(side*.115f,.08f,.05f),new Vector3(.17f,.12f,.32f),shoe,p);
                var arm=Shape("Arm",PrimitiveType.Capsule,new Vector3(side*.29f,1,0),new Vector3(.13f,.28f,.13f),clothes,p,false);
                if(name=="Passenger"){if(side<0)passengerArmL=arm.transform;else passengerArmR=arm.transform;}
            }
            return p;
        }
        void BuildWorld()
        {
            world=Group("City • generated environment",transform,Vector3.zero);worldFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            worldLabelMaterial=new Material(Resources.Load<Shader>("WorldLabel"));materials.Add(worldLabelMaterial);Font.textureRebuilt+=RefreshLabelAtlas;
            var asphalt=Grain(Mat(C("252F35"),.12f),5,110);roadSurface=asphalt;
            var ground=Grain(Mat(C("6A7359")),20,50);var pavement=Mat(C("ADA99C"));var curb=Mat(C("CFCBBF"));var paint=Mat(C("E8E1CB"));var metal=Mat(C("34474C"));var dark=Mat(C("28343B"));var teal=Mat(C("166774"));var bark=Mat(C("5F4D3E"));var leaves=Mat(C("405F40"));var skin=Mat(C("87563A"));var wood=Mat(C("80573C"));
            Box("Ground",new Vector3(0,-.55f,110),new Vector3(150,1,350),ground,world,true);
            Box("Road",new Vector3(0,-.03f,110),new Vector3(12,.1f,310),asphalt,world,true);
            for(int side=-1;side<=1;side+=2){
                Box("Pavement",new Vector3(side*8.3f,.09f,110),new Vector3(4.5f,.25f,310),pavement,world,true);
                for(int z=-40;z<266;z+=4){Box("Curb stone",new Vector3(side*6.13f,.13f,z),new Vector3(.28f,.32f,3.9f),curb,world,true);Box("Paving joint",new Vector3(side*8.3f,.22f,z),new Vector3(4.4f,.012f,.025f),dark,world);}
                Box("Road edge",new Vector3(side*5.55f,.035f,110),new Vector3(.1f,.015f,300),paint,world);
                for(int z=-20;z<270;z+=28){
                    if(Mathf.Abs(z+13)>12)Tree(new Vector3(side*9.4f,.22f,z+13),bark,leaves);
                    var lamp=Group("Street light",world,new Vector3(side*6.8f,.22f,z));
                    Shape("Pole",PrimitiveType.Cylinder,new Vector3(0,2.7f,0),new Vector3(.095f,2.7f,.095f),metal,lamp,false);
                    Box("Arm",new Vector3(-side*.55f,5.35f,0),new Vector3(1.2f,.1f,.1f),metal,lamp);
                    Box("Lamp head",new Vector3(-side*1.1f,5.28f,0),new Vector3(.5f,.14f,.8f),curb,lamp);
                }
            }
            for(int z=-35;z<263;z+=7)Box("Centre dash",new Vector3(0,.035f,z),new Vector3(.15f,.012f,3),paint,world);
            for(int crossing=0;crossing<3;crossing++)for(int i=0;i<9;i++)Box("Pedestrian crossing",new Vector3(-5+i*1.25f,.04f,55+crossing*70),new Vector3(.62f,.015f,3.5f),paint,world);
            Material[] facades={Mat(C("B3A58E")),Mat(C("9D6F59")),Mat(C("8B9A98")),Mat(C("C0B59C"))};
            var shopGlass=Mat(C("233F4A"),.45f);var shopRed=Mat(C("88513B"));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<12;i++)Building(side,i,facades[(i+(side>0?1:0))%4],dark,curb,shopGlass,i%2==0?teal:shopRed);
            for(int i=0;i<stops.Length;i++){
                var stop=Group("Rank • "+stopNames[i],world,stops[i]);
                Box("Stop sign pole",new Vector3(-2,1.65f,0),new Vector3(.1f,3.3f,.1f),metal,stop);
                Box("Rank sign",new Vector3(-2,2.8f,0),new Vector3(2.2f,.9f,.13f),teal,stop);
                Label("KOMBI STOP\n"+stopNames[i].ToUpperInvariant(),stop,new Vector3(-2,2.82f,-.075f),.045f,paper,Quaternion.identity);
                Box("Shelter roof",new Vector3(-3.2f,2.5f,3.2f),new Vector3(2.8f,.17f,4.7f),teal,stop);
                for(int j=-1;j<=1;j+=2)Box("Shelter column",new Vector3(-4.2f,1.3f,3.2f+j*1.9f),new Vector3(.1f,2.6f,.1f),metal,stop);
                Box("Bench",new Vector3(-3.3f,.7f,3.2f),new Vector3(.5f,.13f,3.5f),wood,stop);
                Box("Bench back",new Vector3(-3.58f,1,3.2f),new Vector3(.12f,.6f,3.5f),wood,stop);
                for(int j=-1;j<=1;j+=2)Box("Bench leg",new Vector3(-3.3f,.4f,3.2f+j*1.3f),new Vector3(.3f,.6f,.12f),metal,stop);
                Shape("Litter bin",PrimitiveType.Cylinder,new Vector3(-2.8f,.65f,-3),new Vector3(.55f,.45f,.55f),metal,stop,false);
                Person("Waiting pedestrian",stops[i]+new Vector3(-3,.22f,4.2f),C(i%2==0?"C3A04C":"4C687E"),skin);
            }
            var station=Group("CITY FUEL",world,new Vector3(11,0,110));
            Box("Station canopy",new Vector3(0,4.1f,0),new Vector3(9,.4f,11),teal,station);
            Box("Station fascia",new Vector3(0,4.15f,-5.6f),new Vector3(9,.7f,.2f),teal,station);
            Label("CITY FUEL",station,new Vector3(0,4.2f,-5.72f),.12f,paper,Quaternion.identity);
            for(int j=-1;j<=1;j+=2){Box("Canopy column",new Vector3(2,2,j*4),new Vector3(.25f,4,.25f),curb,station);Rounded("Fuel pump",new Vector3(-2,.9f,j*2.7f),new Vector3(.75f,1.6f,.75f),.06f,curb,station);Box("Pump screen",new Vector3(-2,1.2f,j*2.7f-.39f),new Vector3(.5f,.4f,.025f),dark,station);}
            for(int i=0;i<5;i++){var hole=Shape("Pothole",PrimitiveType.Cylinder,new Vector3(i%2==0?-2:2,.035f,50+i*34),new Vector3(1.25f,.012f,1.7f),dark,world,false);for(int j=0;j<4;j++)Shape("Broken asphalt",PrimitiveType.Cube,hole.transform.position+new Vector3(Mathf.Cos(j*1.6f)*.8f,.02f,Mathf.Sin(j*1.6f)),new Vector3(.16f,.08f,.25f),asphalt,world,false);}
            // Wide end plazas provide room to turn around for the return journey.
            for(int i=0;i<2;i++){float z=i==0?-27:252;Shape("Turning apron",PrimitiveType.Cylinder,new Vector3(0,-.025f,z),new Vector3(25,.045f,25),asphalt,world,true);Box("Turn sign",new Vector3(-7,2.2f,z),new Vector3(2.7f,.65f,.15f),teal,world);Label("TURN AROUND",world,new Vector3(-7,2.2f,z-.09f),.065f,paper,Quaternion.identity);}
            var batchable=new List<GameObject>();foreach(var rendererToBatch in world.GetComponentsInChildren<MeshRenderer>())if(rendererToBatch.GetComponent<TextMesh>()==null)batchable.Add(rendererToBatch.gameObject);
            StaticBatchingUtility.Combine(batchable.ToArray(),world.gameObject);
            var player=Bus("Player Kombi",C("1A6973"),world);player.position=new Vector3(-3,.34f,0);
            var collider=player.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.79f,0);collider.size=new Vector3(1.86f,1.98f,4.55f);
            player.gameObject.AddComponent<Rigidbody>();driver=player.gameObject.AddComponent<KombiDriver>();driver.game=this;
            rider=Person("Passenger",stops[0]+Vector3.left*2,C("B65F40"),skin);
            stopMarker=Group("Pickup / drop-off ring",world,Vector3.zero);BuildRing(stopMarker,Mat(C("E5B746")),2.6f,2.4f);
            var cameraObject=Group("Chase Camera",world,player.position-Vector3.forward*8.8f+Vector3.up*4.1f).gameObject;
            chase=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();chase.fieldOfView=58;chase.nearClipPlane=.2f;chase.farClipPlane=330;chase.clearFlags=CameraClearFlags.SolidColor;chase.backgroundColor=C("8CAEB7");chase.allowHDR=true;
            var lightObject=Group("Afternoon sun",world,Vector3.zero).gameObject;sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.65f;sun.shadowBias=.045f;sun.transform.rotation=Quaternion.Euler(42,-32,0);
            QualitySettings.shadowDistance=70;QualitySettings.shadows=ShadowQuality.All;QualitySettings.antiAliasing=4;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=C("697B80")*.55f;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=245;
            for(int i=0;i<7;i++){
                var car=Bus(i==0?"Rival Kombi":"Traffic "+i,C(i==0?"A34C38":i%2==0?"ACAD9A":"587384"),world);var c=car.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.79f,0);c.size=new Vector3(1.86f,1.98f,4.55f);
                var rb=car.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;traffic.Add(new Traffic{transform=car,body=rb,speed=5+i*.5f,rival=i==0});
            }
            var rainObject=Group("Rain streaks",world,Vector3.zero).gameObject;rain=rainObject.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.startLifetime=1;main.startSpeed=20;main.startSize=.025f;main.maxParticles=1000;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.65f,.78f,.84f,.45f);
            var emission=rain.emission;emission.rateOverTime=650;var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(26,1,30);rainObject.transform.rotation=Quaternion.Euler(90,0,0);
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Mat(C("9DB7C3"));renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=9;renderer.velocityScale=.08f;
            effects=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();engine=gameObject.AddComponent<AudioSource>();effects.spatialBlend=music.spatialBlend=engine.spatialBlend=0;
            cue=Tone("Fare cue",660,.13f);warning=Tone("Warning",150,.2f);musicClip=AmbientLoop();music.clip=musicClip;music.loop=true;music.volume=musicVolume*.4f;music.Play();
            engine.clip=Tone("Engine",70,1);engine.loop=true;engine.volume=0;engine.Play();
            screen=Screen.Menu;Time.timeScale=0;ResetTraffic();PositionPassenger();ApplyWeather();
        }
        void RefreshLabelAtlas(Font font){if(font==worldFont&&worldLabelMaterial!=null)worldLabelMaterial.mainTexture=worldFont.material.mainTexture;}
        void BuildRing(Transform parent,Material material,float outer,float inner)
        {
            const int segments=64;var v=new Vector3[(segments+1)*2];var tris=new int[segments*6];
            for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;v[i*2]=new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer);v[i*2+1]=new Vector3(Mathf.Cos(a)*inner,0,Mathf.Sin(a)*inner);if(i<segments){int k=i*6;tris[k]=i*2;tris[k+1]=i*2+1;tris[k+2]=i*2+2;tris[k+3]=i*2+1;tris[k+4]=i*2+3;tris[k+5]=i*2+2;}}
            var mesh=new Mesh{name="Destination ring"};mesh.vertices=v;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();generatedMeshes.Add(mesh);parent.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;parent.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        void AnimateObjective()
        {
            if(stopMarker!=null){stopMarker.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*2)*.025f);}
            if(boarding&&passengerArmL!=null){passengerArmL.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*10)*25,0,0);passengerArmR.localRotation=Quaternion.Euler(-Mathf.Sin(Time.time*10)*25,0,0);}
        }
        void ApplyWeather()
        {
            sun.color=Rainy?C("C2D3DC"):C("FFE9C7");sun.intensity=Rainy?.7f:1.1f;
            chase.backgroundColor=Rainy?C("6F8996"):C("8CAEB7");RenderSettings.fogColor=chase.backgroundColor;RenderSettings.fogEndDistance=Rainy?160:245;
            if(roadSurface.HasProperty("_Glossiness"))roadSurface.SetFloat("_Glossiness",Rainy?.65f:.12f);
        }
        void UpdateEngineSound(){engine.volume=IsPlaying?effectsVolume*(.06f+driver.Speed*.003f):0;engine.pitch=.75f+driver.Speed*.045f+Mathf.Abs(driver.Throttle)*.18f;}
        AudioClip AmbientLoop()
        {
            const int rate=22050;const int seconds=8;var samples=new float[rate*seconds];float[] notes={220,277.18f,329.63f,277.18f,196,246.94f,293.66f,246.94f};
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate,phase=t%1;float envelope=Mathf.Sin(phase*Mathf.PI);samples[i]=(Mathf.Sin(t*notes[(int)t]*Mathf.PI*2)+.3f*Mathf.Sin(t*notes[(int)t]*Mathf.PI*4))*.035f*envelope;}
            var clip=AudioClip.Create("Original shift theme",samples.Length,1,rate,false);clip.SetData(samples,0);clips.Add(clip);return clip;
        }
    }
}
