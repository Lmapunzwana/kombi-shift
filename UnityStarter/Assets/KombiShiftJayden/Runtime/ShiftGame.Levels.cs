using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        bool NearJunction(float z,float margin){foreach(float j in new[]{25f,85,145,205})if(Mathf.Abs(z-j)<margin)return true;return false;}
        void BuildCityConnections(Material asphalt,Material ground,Material paving,Material paint,Material trim,Material[] facades,Material dark,Material glass,Material awning)
        {
            Box("Second city street",new Vector3(40,-.03f,110),new Vector3(12,.1f,310),asphalt,world,true);
            for(int side=-1;side<=1;side+=2)foreach(var segment in new[]{new Vector2(-45,19),new Vector2(31,79),new Vector2(91,139),new Vector2(151,199),new Vector2(211,265)})Box("Second street pavement",new Vector3(40+side*8.5f,.08f,(segment.x+segment.y)/2),new Vector3(4,.22f,segment.y-segment.x),paving,world,true);
            foreach(float z in new[]{25f,85,145,205}){
                Box("Cross street",new Vector3(20,-.015f,z),new Vector3(52,.13f,12),asphalt,world,true);
                for(int x=7;x<37;x+=7)Box("Cross street dash",new Vector3(x,.06f,z),new Vector3(3,.02f,.14f),paint,world);
            }
            for(int z=-35;z<263;z+=7)Box("Second street dash",new Vector3(40,.04f,z),new Vector3(.14f,.02f,3),paint,world);
            for(int i=0;i<12;i++)if(!NearJunction(-18+i*24,15))Building(1,i,facades[i%facades.Length],dark,trim,glass,awning,40);
        }
        void BuildHighway()
        {
            highwayEnvironment=Group("Rainy highway level",world,Vector3.zero);var t=highwayEnvironment;
            var dirt=Grain(Mat(C("727466")),20,80);highwayRoad=Grain(Mat(C("393E40"),.6f),4,170);var paint=Mat(C("CBCABF"));var metal=Mat(C("7C8585"));var roof=Mat(C("686C63"));
            Box("Highway ground",new Vector3(0,-.6f,260),new Vector3(160,1,680),dirt,t,true);Box("Highway carriageway",new Vector3(0,-.03f,260),new Vector3(12,.1f,620),highwayRoad,t,true);
            for(int z=-40;z<570;z+=9){Box("Highway dash",new Vector3(0,.04f,z),new Vector3(.15f,.02f,4),paint,t);for(int side=-1;side<=1;side+=2){Box("Edge paint",new Vector3(side*5.6f,.04f,z),new Vector3(.12f,.02f,9),paint,t);if(z%3==0)Box("Guardrail",new Vector3(side*8,.7f,z),new Vector3(.16f,.15f,8),metal,t);}}
            var definition=LevelDefinition.Highway();for(int i=0;i<definition.stops.Length;i++){
                Vector3 s=definition.stops[i];Box("Highway layby",s+new Vector3(-1,-.02f,0),new Vector3(5,.1f,13),highwayRoad,t,true);Box("Highway rank pole",s+new Vector3(-2,1.5f,0),new Vector3(.12f,3,.12f),metal,t);Box("Rank board",s+new Vector3(-2,2.7f,0),new Vector3(3,.9f,.12f),roof,t);Label(definition.names[i],t,s+new Vector3(-2,2.7f,-.08f),.07f,paper,Quaternion.identity);
                Box("Rural shelter roof",s+new Vector3(-4,2.3f,2),new Vector3(3.5f,.2f,5),roof,t);for(int side=-1;side<=1;side+=2)Box("Shelter post",s+new Vector3(-5,1.2f,2+side*2),new Vector3(.12f,2.4f,.12f),metal,t);
            }
            Box("Highway fuel canopy",new Vector3(10,4,280),new Vector3(10,.3f,12),roof,t);Box("Highway fuel pump",new Vector3(8,1,280),new Vector3(1,2,1),paint,t);Label("FUEL",t,new Vector3(10,4,273.8f),.18f,paper,Quaternion.identity);
            for(int i=0;i<18;i++)Box("Highway distant hills",new Vector3((i%2==0?-1:1)*(35+i%3*8),4+i%4, i*35-20),new Vector3(25,8+i%4*2,30),dirt,t);
            StaticBatchingUtility.Combine(t.gameObject);t.gameObject.SetActive(false);
        }
        Transform TrafficVehicle(int index,Transform parent)
        {
            if(index%3==0)return Bus("Traffic minibus "+index,C("C7C7BA"),parent);
            var root=Group(index%3==1?"Traffic sedan":"Traffic delivery truck",parent,Vector3.zero);var body=Mat(C(index%2==0?"8D9392":"B6B4A9"));var glass=Mat(C("3C505A"));var rubber=Mat(C("262C2C"));
            if(index%3==1){Rounded("Sedan body",new Vector3(0,.48f,0),new Vector3(1.8f,.65f,4.2f),.15f,body,root);Rounded("Sedan cabin",new Vector3(0,1.02f,-.1f),new Vector3(1.55f,.7f,2.1f),.15f,glass,root);}else{Rounded("Truck cab",new Vector3(0,.95f,1.3f),new Vector3(1.9f,1.7f,1.8f),.12f,body,root);Box("Cargo box",new Vector3(0,1.22f,-1),new Vector3(1.95f,2,2.6f),body,root);Box("Truck glass",new Vector3(0,1.4f,2.23f),new Vector3(1.6f,.6f,.03f),glass,root);}
            for(int side=-1;side<=1;side+=2)for(int axle=-1;axle<=1;axle+=2){var w=Shape("Traffic wheel",PrimitiveType.Cylinder,new Vector3(side*.93f,.08f,axle*1.4f),new Vector3(.62f,.14f,.62f),rubber,root,false);w.transform.localRotation=Quaternion.Euler(0,0,90);}return root;
        }
    }
}
