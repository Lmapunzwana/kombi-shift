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
        string[] stopNames=LevelDefinition.City().names;
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
            var root=Group(name,parent,Vector3.zero);var body=Mat(color,.18f);var cream=Mat(C("D8D5C9"),.18f);var glass=Mat(C("44575D"),.25f);var rubber=Mat(C("182023"));var trim=Mat(C("687478"),.55f);var stripe=Mat(C("405D6C"));var plate=Mat(C("D3C9A3"));var red=Mat(C("B92E28"));
            bool playerModel=name=="Player Kombi";
            if(playerModel){
                Rounded("Floor and sill",new Vector3(0,.25f,0),new Vector3(1.86f,.35f,4.55f),.1f,body,root);
                Rounded("Front body",new Vector3(0,.7f,1.7f),new Vector3(1.86f,1.25f,1.15f),.12f,body,root);
                Rounded("Rear body",new Vector3(0,.7f,-1.25f),new Vector3(1.86f,1.25f,2.05f),.12f,body,root);
                Box("Right side",new Vector3(.86f,.76f,0),new Vector3(.14f,1.35f,4.3f),body,root);
                Rounded("Roof",new Vector3(0,1.88f,-.12f),new Vector3(1.8f,.26f,4.03f),.1f,cream,root);
                Box("Right cabin frame",new Vector3(.86f,1.5f,-.12f),new Vector3(.13f,.8f,3.9f),cream,root);
                Box("Rear left frame",new Vector3(-.86f,1.5f,-1.3f),new Vector3(.13f,.8f,1.8f),cream,root);
                Box("Front left frame",new Vector3(-.86f,1.5f,1.5f),new Vector3(.13f,.8f,.8f),cream,root);
                Box("Rear cabin frame",new Vector3(0,1.5f,-2.11f),new Vector3(1.7f,.8f,.08f),cream,root);
                Box("Front cabin frame",new Vector3(0,1.5f,1.86f),new Vector3(1.7f,.8f,.08f),cream,root);
                for(int row=0;row<3;row++)Box("Passenger bench",new Vector3(.12f,.61f,-1.5f+row*.78f),new Vector3(1.15f,.24f,.55f),rubber,root);
                slidingDoor=Group("Working sliding passenger door",root,new Vector3(0,0,.45f));
                Box("Door lower panel",new Vector3(-.955f,.75f,0),new Vector3(.08f,1.22f,1.3f),body,slidingDoor);
                Box("Door upper frame",new Vector3(-.91f,1.5f,0),new Vector3(.08f,.78f,1.3f),cream,slidingDoor);
                Box("Door glass",new Vector3(-.962f,1.53f,0),new Vector3(.025f,.53f,1.13f),glass,slidingDoor);
                Box("Door livery",new Vector3(-1.002f,.88f,0),new Vector3(.025f,.18f,1.3f),stripe,slidingDoor);
                Box("Door handle",new Vector3(-1.015f,1.02f,.4f),new Vector3(.035f,.05f,.19f),trim,slidingDoor);
            } else {
                Rounded("Lower body",new Vector3(0,.7f,0),new Vector3(1.86f,1.25f,4.55f),.14f,body,root);
                Rounded("Cream roof",new Vector3(0,1.52f,-.12f),new Vector3(1.8f,.94f,4.03f),.14f,cream,root);
            }
            Rounded("Front windscreen",new Vector3(0,1.52f,1.905f),new Vector3(1.54f,.61f,.045f),.02f,glass,root);
            Rounded("Rear windscreen",new Vector3(0,1.54f,-2.155f),new Vector3(1.51f,.55f,.045f),.02f,glass,root);
            Box("Rear route stripe",new Vector3(0,.87f,-2.29f),new Vector3(1.74f,.18f,.035f),stripe,root);
            Box("Rear bumper",new Vector3(0,.23f,-2.34f),new Vector3(1.9f,.15f,.14f),rubber,root);
            Box("Front bumper",new Vector3(0,.23f,2.34f),new Vector3(1.9f,.15f,.14f),rubber,root);
            Box("Front grille",new Vector3(0,.53f,2.29f),new Vector3(.85f,.24f,.04f),rubber,root);
            Box("Rear number plate",new Vector3(0,.42f,-2.29f),new Vector3(.69f,.2f,.04f),plate,root);
            Label("ABX 0426",root,new Vector3(0,.42f,-2.318f),.025f,C("162C31"),Quaternion.identity);
            Label(name=="Player Kombi"?"CITYLINK":"COMMUTER",root,new Vector3(0,1.58f,-2.185f),.033f,C("F4EBD8"),Quaternion.identity);
            Label("HARARE • CITY RANK",root,new Vector3(0,1.82f,-2.18f),.018f,C("CECDBF"),Quaternion.identity);
            for(int side=-1;side<=1;side+=2){
                Box("Tail lights",new Vector3(side*.73f,.49f,-2.29f),new Vector3(.16f,.31f,.035f),red,root);
                Box("Headlights",new Vector3(side*.66f,.63f,2.29f),new Vector3(.35f,.2f,.05f),cream,root);
                if(playerModel&&side<0){Box("Rear livery",new Vector3(-.94f,.88f,-1.25f),new Vector3(.024f,.18f,2.05f),stripe,root);Box("Front livery",new Vector3(-.94f,.88f,1.7f),new Vector3(.024f,.18f,1.15f),stripe,root);}
                else Box("Livery",new Vector3(side*.94f,.88f,0),new Vector3(.024f,.18f,4.25f),stripe,root);
                Box("Window rail",new Vector3(side*.912f,1.16f,-.16f),new Vector3(.022f,.04f,3.8f),rubber,root);
                for(int i=0;i<4;i++){if(playerModel&&side<0&&i==2)continue;float z=playerModel&&side<0&&i==3?1.45f:-1.55f+i*.9f;Rounded("Side window",new Vector3(side*.94f,1.51f,z),new Vector3(.035f,.54f,.65f),.015f,glass,root);}
                if(!(playerModel&&side<0)){Box("Door seam",new Vector3(side*.944f,.7f,.95f),new Vector3(.025f,1.05f,.025f),rubber,root);
                Box("Door handle",new Vector3(side*.963f,1.04f,.64f),new Vector3(.045f,.045f,.18f),trim,root);}
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
        void Building(int side,int index,Material facade,Material dark,Material trim,Material glass,Material awning,float offset=0)
        {
            float z=-18+index*24;float height=5+(index%4)*2.6f;var block=Group("Shop block "+side+" "+index,world,new Vector3(side*16+offset,0,z));
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
            if(name=="Passenger"&&passengerPrefab!=null){var instance=Instantiate(passengerPrefab,world);instance.name=name;instance.transform.localPosition=position;passengerArmL=instance.transform.Find("Arm L");passengerArmR=instance.transform.Find("Arm R");return instance.transform;}
            var p=Group(name,world,position);var clothes=Mat(shirt);var trousers=Mat(C("283744"));var shoe=Mat(C("242629"));
            Shape("Torso",PrimitiveType.Capsule,new Vector3(0,1.04f,0),new Vector3(.42f,.34f,.3f),clothes,p,false);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.64f,0),new Vector3(.3f,.36f,.3f),skin,p,false);
            Shape("Hair",PrimitiveType.Sphere,new Vector3(0,1.77f,-.015f),new Vector3(.3f,.17f,.29f),shoe,p,false);
            for(int side=-1;side<=1;side+=2){
                Shape("Leg",PrimitiveType.Capsule,new Vector3(side*.115f,.42f,0),new Vector3(.14f,.34f,.16f),trousers,p,false);
                Box("Shoe",new Vector3(side*.115f,.08f,.05f),new Vector3(.17f,.12f,.32f),shoe,p);
                var arm=Shape(side<0?"Arm L":"Arm R",PrimitiveType.Capsule,new Vector3(side*.29f,1,0),new Vector3(.13f,.28f,.13f),clothes,p,false);
                if(name=="Passenger"){if(side<0)passengerArmL=arm.transform;else passengerArmR=arm.transform;}
            }
            return p;
        }
        void BuildWorld()
        {
            world=Group("City • generated environment",transform,Vector3.zero);worldFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            worldLabelMaterial=new Material(Resources.Load<Shader>(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRender")?"HDRPWorldLabel":"WorldLabel"));materials.Add(worldLabelMaterial);Font.textureRebuilt+=RefreshLabelAtlas;
            var asphalt=Grain(Mat(C("3B3D3B"),.12f),5,110);roadSurface=asphalt;
            var ground=Grain(Mat(C("827B68")),20,50);var pavement=Mat(C("A8A397"));var curb=Mat(C("C1BDB1"));var paint=Mat(C("E8E1CB"));var metal=Mat(C("34474C"));var dark=Mat(C("28343B"));var teal=Mat(C("4E5B5E"));var bark=Mat(C("5F4D3E"));var leaves=Mat(C("5B654E"));var skin=Mat(C("87563A"));var wood=Mat(C("80573C"));
            Box("Ground",new Vector3(0,-.55f,110),new Vector3(150,1,350),ground,world,true);
            Box("Road",new Vector3(0,-.03f,110),new Vector3(12,.1f,310),asphalt,world,true);
            for(int side=-1;side<=1;side+=2){
                foreach(var segment in new[]{new Vector2(-45,19),new Vector2(31,79),new Vector2(91,139),new Vector2(151,199),new Vector2(211,265)})Box("Pavement",new Vector3(side*8.3f,.09f,(segment.x+segment.y)/2),new Vector3(4.5f,.25f,segment.y-segment.x),pavement,world,true);
                for(int z=-40;z<266;z+=4){if(NearJunction(z,8))continue;Box("Curb stone",new Vector3(side*6.13f,.13f,z),new Vector3(.28f,.32f,3.9f),curb,world,true);Box("Paving joint",new Vector3(side*8.3f,.22f,z),new Vector3(4.4f,.012f,.025f),dark,world);}
                Box("Road edge",new Vector3(side*5.55f,.035f,110),new Vector3(.1f,.015f,300),paint,world);
                for(int z=-20;z<270;z+=28){if(NearJunction(z,8))continue;
                    bool clearRank=false;foreach(var rank in stops)if(Mathf.Abs(z+13-rank.z)<15)clearRank=true;
                    if(Mathf.Abs(z+13)>12&&!clearRank)Tree(new Vector3(side*9.4f,.22f,z+13),bark,leaves);
                    var lamp=Group("Street light",world,new Vector3(side*6.8f,.22f,z));
                    Shape("Pole",PrimitiveType.Cylinder,new Vector3(0,2.7f,0),new Vector3(.095f,2.7f,.095f),metal,lamp,false);
                    Box("Arm",new Vector3(-side*.55f,5.35f,0),new Vector3(1.2f,.1f,.1f),metal,lamp);
                    Box("Lamp head",new Vector3(-side*1.1f,5.28f,0),new Vector3(.5f,.14f,.8f),curb,lamp);
                }
            }
            for(int z=-35;z<263;z+=7)Box("Centre dash",new Vector3(0,.035f,z),new Vector3(.15f,.012f,3),paint,world);
            for(int crossing=0;crossing<3;crossing++)for(int i=0;i<9;i++)Box("Pedestrian crossing",new Vector3(-5+i*1.25f,.04f,55+crossing*70),new Vector3(.62f,.015f,3.5f),paint,world);
            Material[] facades={Mat(C("B5AD9B")),Mat(C("A38C79")),Mat(C("92978F")),Mat(C("BEB9AD"))};
            var shopGlass=Mat(C("233F4A"),.45f);var shopRed=Mat(C("76675B"));
            for(int side=-1;side<=1;side+=2)for(int i=0;i<12;i++)if(!NearJunction(-18+i*24,15))Building(side,i,facades[(i+(side>0?1:0))%4],dark,curb,shopGlass,i%2==0?teal:shopRed);
            BuildCityConnections(asphalt,ground,pavement,paint,curb,facades,dark,shopGlass,teal);
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
                Shape("Litter bin",PrimitiveType.Cylinder,new Vector3(-4.6f,.65f,-3),new Vector3(.55f,.45f,.55f),metal,stop,false);

            }
            var station=Group("CITY FUEL",world,new Vector3(11,0,110));
            Box("Station canopy",new Vector3(0,4.1f,0),new Vector3(9,.4f,11),teal,station);
            Box("Station fascia",new Vector3(0,4.15f,-5.6f),new Vector3(9,.7f,.2f),teal,station);
            Label("CITY FUEL",station,new Vector3(0,4.2f,-5.72f),.12f,paper,Quaternion.identity);
            for(int j=-1;j<=1;j+=2){Box("Canopy column",new Vector3(2,2,j*4),new Vector3(.25f,4,.25f),curb,station);Rounded("Fuel pump",new Vector3(-2,.9f,j*2.7f),new Vector3(.75f,1.6f,.75f),.06f,curb,station);Box("Pump screen",new Vector3(-2,1.2f,j*2.7f-.39f),new Vector3(.5f,.4f,.025f),dark,station);}
            // Wide end plazas provide room to turn around for the return journey.
            for(int i=0;i<2;i++){float z=i==0?-27:252;Shape("Turning apron",PrimitiveType.Cylinder,new Vector3(0,-.025f,z),new Vector3(25,.045f,25),asphalt,world,true);Box("Turn sign",new Vector3(-7,2.2f,z),new Vector3(2.7f,.65f,.15f),teal,world);Label("TURN AROUND",world,new Vector3(-7,2.2f,z-.09f),.065f,paper,Quaternion.identity);}
            var batchable=new List<GameObject>();foreach(var rendererToBatch in world.GetComponentsInChildren<MeshRenderer>())if(rendererToBatch.GetComponent<TextMesh>()==null)batchable.Add(rendererToBatch.gameObject);
            StaticBatchingUtility.Combine(batchable.ToArray(),world.gameObject);
            cityEnvironment=world;world=Group("Runtime actors",transform,Vector3.zero);cityEnvironment.SetParent(world,false);
            var player=Bus("Player Kombi",C("D5D4C9"),world);player.position=new Vector3(-3,.34f,0);
            var collider=player.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.79f,0);collider.size=new Vector3(1.86f,1.98f,4.55f);
            player.gameObject.AddComponent<Rigidbody>();driver=player.gameObject.AddComponent<KombiDriver>();driver.game=this;
            rider=Person("Passenger",stops[0]+Vector3.left*2,C("746A5D"),skin);
            departingRider=Person("Alighting passenger",Vector3.zero,C("746A5D"),skin);departingRider.gameObject.SetActive(false);
            CreateHwindi(skin);
            stopMarker=Group("Pickup / drop-off ring",world,Vector3.zero);BuildRing(stopMarker,Mat(C("C5C0AE")),2.6f,2.4f);
            var cameraObject=Group("Chase Camera",world,player.position-Vector3.forward*8.8f+Vector3.up*4.1f).gameObject;
            chase=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();chase.fieldOfView=58;chase.nearClipPlane=.2f;chase.farClipPlane=330;chase.clearFlags=CameraClearFlags.SolidColor;chase.backgroundColor=C("8CAEB7");chase.allowHDR=true;
            var lightObject=Group("Afternoon sun",world,Vector3.zero).gameObject;sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.shadows=LightShadows.None;sun.transform.rotation=Quaternion.Euler(42,-32,0);
            QualitySettings.shadowDistance=0;QualitySettings.shadows=ShadowQuality.Disable;QualitySettings.antiAliasing=4;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=C("9A9B97")*.65f;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=245;
            for(int i=0;i<10;i++){
                var car=i==0?Bus("Rival Kombi",C("B7B5AA"),world):TrafficVehicle(i,world);var c=car.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.79f,0);c.size=new Vector3(1.86f,1.98f,4.55f);
                var rb=car.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;traffic.Add(new Traffic{transform=car,body=rb,speed=5+i*.5f,rival=i==0});
            }
            var rainObject=Group("Rain streaks",world,Vector3.zero).gameObject;rain=rainObject.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.startLifetime=1;main.startSpeed=20;main.startSize=.025f;main.maxParticles=1000;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.65f,.78f,.84f,.45f);
            var emission=rain.emission;emission.rateOverTime=650;var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(26,1,30);rainObject.transform.rotation=Quaternion.Euler(90,0,0);
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Mat(C("9DB7C3"));renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=9;renderer.velocityScale=.08f;
            InitializeWorkingAudio();
            screen=Screen.Menu;Time.timeScale=0;ResetTraffic();PositionPassenger();ResetHwindi();ApplyWeather();
        }
        void RefreshLabelAtlas(Font font){if(font==worldFont&&worldLabelMaterial!=null)worldLabelMaterial.mainTexture=worldFont.material.mainTexture;}
        void BuildRing(Transform parent,Material material,float outer,float inner)
        {
            Box("Stop bay left",new Vector3(-1.1f,0,0),new Vector3(.06f,.015f,6.4f),material,parent);
            Box("Stop bay right",new Vector3(1.1f,0,0),new Vector3(.06f,.015f,6.4f),material,parent);
            Box("Stop bay front",new Vector3(0,0,3.2f),new Vector3(2.2f,.015f,.06f),material,parent);
            Box("Stop bay rear",new Vector3(0,0,-3.2f),new Vector3(2.2f,.015f,.06f),material,parent);
        }
        void AnimateObjective()
        {
            if(stopMarker!=null)stopMarker.localScale=Vector3.one;
            if(boarding&&passengerArmL!=null){passengerArmL.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*10)*25,0,0);passengerArmR.localRotation=Quaternion.Euler(-Mathf.Sin(Time.time*10)*25,0,0);}
        }
        void ApplyWeather()
        {
            sun.color=Rainy?C("D4D7D8"):C("F0EDE4");sun.intensity=Rainy?.62f:.85f;
            chase.backgroundColor=Rainy?C("929A9D"):C("B3BAB9");RenderSettings.fogColor=chase.backgroundColor;RenderSettings.fogEndDistance=Rainy?160:245;
            WeatherChanged?.Invoke(Rainy);
            if(roadSurface.HasProperty("_Smoothness"))roadSurface.SetFloat("_Smoothness",Rainy?.65f:.12f);if(highwayRoad!=null&&highwayRoad.HasProperty("_Smoothness"))highwayRoad.SetFloat("_Smoothness",.6f);
            if(roadSurface.HasProperty("_Glossiness"))roadSurface.SetFloat("_Glossiness",Rainy?.65f:.12f);if(highwayRoad!=null&&highwayRoad.HasProperty("_Glossiness"))highwayRoad.SetFloat("_Glossiness",.6f);
        }
        AudioClip AmbientLoop()
        {
            const int rate=22050;const int seconds=8;var samples=new float[rate*seconds];float[] notes={261.63f,329.63f,392,440,392,329.63f,293.66f,329.63f,261.63f,392,440,523.25f,440,392,329.63f,293.66f};
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate,phase=t%.5f;float f=notes[(int)(t*2)];float envelope=Mathf.Exp(-phase*12);samples[i]=(Mathf.Sin(t*f*Mathf.PI*2)+.35f*Mathf.Sin(t*f*Mathf.PI*5.52f))*.22f*envelope;}
            var clip=AudioClip.Create("Original shift theme",samples.Length,1,rate,false);clip.SetData(samples,0);clips.Add(clip);return clip;
        }
    }
}
