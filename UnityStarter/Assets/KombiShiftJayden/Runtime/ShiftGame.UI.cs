using System.Collections.Generic;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        readonly Dictionary<int,GUIStyle> uiStyles=new Dictionary<int,GUIStyle>();
        GUIStyle actionStyle,quietStyle;bool mouseControls=true;
        readonly Color uiInk=new Color(.075f,.08f,.08f,.96f),uiMuted=new Color(.68f,.69f,.67f),uiAccent=new Color(.74f,.78f,.77f),uiTeal=new Color(.71f,.76f,.76f);
        float ScreenWidth=>UnityEngine.Screen.width;float ScreenHeight=>UnityEngine.Screen.height;
        void Fill(Rect r,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Panel(Rect r){Fill(r,uiInk);Fill(new Rect(r.x,r.y,r.width,1),new Color(.5f,.7f,.7f,.18f));}
        void Write(Rect r,string text,int size,Color color,FontStyle weight=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft)
        {
            int key=size*100+(int)weight*10+(int)align;
            if(!uiStyles.TryGetValue(key,out var style)){style=new GUIStyle{font=worldFont,fontSize=size,fontStyle=weight,alignment=align,wordWrap=true};uiStyles[key]=style;}
            style.normal.textColor=color;GUI.Label(r,text,style);
        }
        bool Action(Rect r,string value,bool primary=false)
        {
            if(actionStyle==null){actionStyle=new GUIStyle{font=worldFont,fontSize=22,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};actionStyle.normal.textColor=paper;quietStyle=new GUIStyle(actionStyle);}
            Color bg=primary?uiAccent:new Color(.22f,.25f,.25f);if(r.Contains(Event.current.mousePosition))bg=primary?new Color(.85f,.87f,.86f):new Color(.3f,.33f,.33f);
            Fill(r,bg);actionStyle.normal.textColor=primary?ink:paper;return GUI.Button(r,value,actionStyle);
        }
        void Meter(Rect r,float fraction,Color color){Fill(r,new Color(.2f,.28f,.3f));Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),color);}
        void OnGUI()
        {
            ShiftInput.ObserveGUI(Event.current);
            float scale=Mathf.Min(ScreenWidth/1600,ScreenHeight/900);GUI.matrix=Matrix4x4.TRS(new Vector3((ScreenWidth-1600*scale)/2,(ScreenHeight-900*scale)/2),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;GUI.contentColor=paper;
            if(screen==Screen.Playing){DrawHUD();return;}
            ShiftInput.Virtual(0,0,false);
            Fill(new Rect(-1000,-1000,3600,2900),new Color(.015f,.035f,.045f,.3f));
            if(screen==Screen.Menu){
                Panel(new Rect(64,70,620,760));Fill(new Rect(64,70,620,5),uiAccent);
                Write(new Rect(104,112,500,28),"HARARE  /  COMMUTER SIMULATOR",18,uiTeal,FontStyle.Bold);
                Write(new Rect(99,159,530,190),"KOMBI\nSHIFT",84,paper,FontStyle.Bold);
                Write(new Rect(106,378,500,70),"Your city. Your hustle.\nMake every stop count.",26,uiMuted);
                Write(new Rect(106,452,500,26),hwindi?"HWINDI ON DUTY  /  $3 per shift":"DRIVER ONLY  /  no hwindi hired",17,uiMuted);
                if(Action(new Rect(104,493,540,64),"START DRIVING   ",true))Begin();
                if(Action(new Rect(104,573,540,54),"ROUTE & DIFFICULTY"))screen=Screen.Selection;
                if(Action(new Rect(104,641,350,52),"HOW TO PLAY"))screen=Screen.Instructions;
                if(Action(new Rect(470,641,174,52),"EXIT"))Application.Quit();
                if(Action(new Rect(104,710,200,38),"TEST SOUND"))PlayHorn();
                Write(new Rect(106,765,500,58),"ENTER to drive  •  WASD / arrows to steer\nClick the game window to give it keyboard focus.",18,uiMuted);
                Panel(new Rect(1120,700,416,130));Write(new Rect(1146,723,360,24),"YOUR NEXT SHIFT",16,uiTeal,FontStyle.Bold);
                Write(new Rect(1146,758,360,36),Rainy?"02  /  RAINY CITY":"01  /  CITY MORNING",25,paper,FontStyle.Bold);
                return;
            }
            if(screen==Screen.Selection){
                Panel(new Rect(350,90,900,720));Write(new Rect(398,129,800,28),"CHOOSE YOUR SHIFT",18,uiTeal,FontStyle.Bold);Write(new Rect(398,179,800,60),"Same city. Higher stakes.",42,paper,FontStyle.Bold);
                if(Action(new Rect(398,280,380,164),"01  CITY MORNING\nDry road · $8 remittance",!Rainy))Rainy=false;
                if(Action(new Rect(800,280,402,164),"02  RAINY CITY\nLower grip · $12 remittance",Rainy))Rainy=true;
                Write(new Rect(400,474,790,60),"Complete 3 trips and cover your costs before the shift ends. Turn around at the end plazas for the return journey.",23,uiMuted);
                if(Action(new Rect(398,559,804,52),hwindi?"HWINDI HIRED     $3 / shift — faster boarding":"ADD A HWINDI   +$3 / shift — faster boarding",hwindi))hwindi=!hwindi;
                if(Action(new Rect(398,650,540,64),"LET'S DRIVE   ",true))Begin();if(Action(new Rect(958,650,244,64),"BACK"))screen=Screen.Menu;return;
            }
            Panel(new Rect(430,75,740,750));Write(new Rect(478,108,640,30),screen==Screen.Instructions?"DRIVER'S GUIDE":screen==Screen.Paused?"TAKE A BREATHER":"SHIFT STATEMENT",18,uiTeal,FontStyle.Bold);
            if(screen==Screen.Instructions){
                Write(new Rect(478,153,640,60),"Get moving. Get paid.",42,paper,FontStyle.Bold);
                string[] labels={"01  DRIVE","02  PICK UP","03  DELIVER","04  MANAGE YOUR SHIFT"};
                string[] bodies={"WASD or arrows to drive. SPACE brakes. Mouse-held drive buttons also work. ENTER starts a shift.","Follow the marked stop bay. Stop within 7m, press E or click BOARD. Stay still while your passenger boards.","Follow the next stop and press E to collect your fare. Complete 3 trips with positive take-home.","E refuels near CITY FUEL. Avoid potholes and traffic. R recovers the vehicle ($1). ESC pauses. Keep fares at $1.60 or less."};
                for(int i=0;i<4;i++){Write(new Rect(478,238+i*113,640,28),labels[i],18,uiAccent,FontStyle.Bold);Write(new Rect(478,273+i*113,640,78),bodies[i],23,paper);}
                if(Action(new Rect(478,737,640,52),"GOT IT — BACK",true))screen=Screen.Menu;
            }
            else if(screen==Screen.Paused){
                Write(new Rect(478,167,640,70),"Shift paused.",48,paper,FontStyle.Bold);Write(new Rect(478,259,640,36),"Music  "+Mathf.RoundToInt(musicVolume*100)+"%",24,paper);GUI.color=uiAccent;musicVolume=GUI.HorizontalSlider(new Rect(478,309,640,30),musicVolume,0,1);GUI.color=Color.white;
                Write(new Rect(478,365,640,36),"Effects  "+Mathf.RoundToInt(effectsVolume*100)+"%",24,paper);GUI.color=uiAccent;effectsVolume=GUI.HorizontalSlider(new Rect(478,414,640,30),effectsVolume,0,1);GUI.color=Color.white;music.volume=musicVolume*.4f;
                if(Action(new Rect(478,505,640,64),"RESUME DRIVING   ",true)){SaveSettings();Pause(false);}
                if(Action(new Rect(478,589,640,56),"RESTART SHIFT")){SaveSettings();Begin();}
                if(Action(new Rect(478,665,640,56),"RETURN TO MENU")){SaveSettings();Menu();}
            }
            else if(screen==Screen.Statement){
                bool success=outcome=="Shift target met!";Write(new Rect(478,158,640,60),success?"A shift well earned.":"Next shift, come back stronger.",36,paper,FontStyle.Bold);
                Write(new Rect(478,242,640,30),delivered+" TRIPS COMPLETED  /  "+missed+" MISSED",18,uiMuted);
                string[] labels={"Gross fares","Fuel used","Damage & recovery","Daily remittance","Hwindi"};float[] values={gross,-fuelCost,-repairCost,-Remittance,-ConductorCost};
                for(int i=0;i<5;i++){Write(new Rect(478,297+i*47,420,35),labels[i],24,uiMuted);Write(new Rect(930,297+i*47,188,35),(values[i]<0?"−$":"$")+Mathf.Abs(values[i]).ToString("F2"),24,paper,FontStyle.Bold,TextAnchor.UpperRight);}
                Fill(new Rect(478,548,640,1),uiMuted);Write(new Rect(478,573,320,38),"TAKE-HOME",20,uiAccent,FontStyle.Bold);Write(new Rect(830,559,288,75),"$"+TakeHome.ToString("F2"),48,paper,FontStyle.Bold,TextAnchor.UpperRight);
                if(Action(new Rect(478,662,405,62),"DRIVE AGAIN",true))Begin();if(Action(new Rect(900,662,218,62),"MENU"))Menu();
                Write(new Rect(478,755,640,30),outcome,20,uiMuted);
            }
        }
        void DrawHUD()
        {
            Panel(new Rect(40,32,310,116));Fill(new Rect(40,32,4,116),uiAccent);Write(new Rect(62,48,270,24),"KOMBI SHIFT   /   FARES",16,uiMuted,FontStyle.Bold);Write(new Rect(59,81,270,54),"$"+gross.ToString("F2"),42,paper,FontStyle.Bold);
            Panel(new Rect(370,32,330,116));Write(new Rect(394,49,282,28),Rainy?"02  RAINY CITY":"01  CITY MORNING",18,uiTeal,FontStyle.Bold);Write(new Rect(394,86,282,32),delivered+" / 3 TRIPS COMPLETED",22,paper);
            Panel(new Rect(1090,32,470,116));float remaining=Mathf.Max(0,shiftSeconds-elapsed);Write(new Rect(1114,50,200,24),"SHIFT REMAINING",16,uiMuted);Write(new Rect(1114,83,200,50),((int)remaining/60).ToString("00")+":"+((int)remaining%60).ToString("00"),34,paper,FontStyle.Bold);
            Write(new Rect(1333,51,200,24),"FUEL  "+Fuel.ToString("F0")+"%",17,Fuel<20?uiAccent:paper);Meter(new Rect(1333,82,200,6),Fuel/100,uiTeal);Write(new Rect(1333,101,200,24),"CONDITION  "+Condition.ToString("F0")+"%",17,paper);Meter(new Rect(1333,129,200,4),Condition/100,Condition<35?uiAccent:uiTeal);
            if(Action(new Rect(728,32,110,42),"PAUSE"))Pause(true);
            if(Time.unscaledTime<messageUntil){Panel(new Rect(40,169,800,57));Write(new Rect(60,181,760,40),message,21,paper);}
            // Compact route diagram, with live player position and the four stops.
            Panel(new Rect(1320,211,240,389));Write(new Rect(1340,229,205,26),"CITY ROUTE",16,uiMuted,FontStyle.Bold);Fill(new Rect(1357,278,3,188),new Color(.3f,.47f,.49f));
            for(int i=0;i<stops.Length;i++){float y=286+i*54;bool next=i==(stopIndex+(aboard?1:0))%4;Fill(new Rect(1351,y-4,15,15),next?uiAccent:uiMuted);Write(new Rect(1380,y-10,160,32),(i+1).ToString("00")+"  "+stopNames[i],15,next?paper:uiMuted);}
            float progress=Mathf.InverseLerp(25,205,driver.transform.position.z);Fill(new Rect(1347,283+progress*162,23,5),uiTeal);Write(new Rect(1340,485,200,27),"TURN AT END PLAZAS",12,uiMuted);
            Write(new Rect(1340,535,200,22),"HWINDI",15,uiMuted);Write(new Rect(1340,562,200,27),hwindi?HwindiDescription:"Not hired",17,paper);
            Panel(new Rect(40,671,360,187));Write(new Rect(62,691,316,25),"FARE PER PASSENGER",16,uiMuted,FontStyle.Bold);Write(new Rect(62,728,205,52),"$"+fare.ToString("F2"),36,paper,FontStyle.Bold);
            if(Action(new Rect(231,723,64,52),"−"))fare=Mathf.Max(.5f,Mathf.Round((fare-.1f)*10)/10);if(Action(new Rect(306,723,64,52),"+"))fare=Mathf.Min(2,Mathf.Round((fare+.1f)*10)/10);
            Write(new Rect(62,802,316,35),fare>1.6f?"Too expensive — passengers will refuse":"Trip payout  $"+(fare*6).ToString("F2"),18,fare>1.6f?uiAccent:uiMuted);
            Panel(new Rect(430,671,740,132));Write(new Rect(453,689,692,23),unloading?"PASSENGER ALIGHTING":boarding?"PASSENGER BOARDING":aboard?"NEXT DESTINATION":"NEXT PICKUP",15,uiTeal,FontStyle.Bold);
            float distance=Vector3.Distance(driver.transform.position,Target);Vector3 direction=(Target-driver.transform.position).normalized;bool behind=Vector3.Dot(driver.transform.forward,direction)<-.2f;
            Write(new Rect(453,725,474,41),stopNames[(stopIndex+(aboard?1:0))%4],30,paper,FontStyle.Bold);Write(new Rect(453,770,450,24),unloading?"Keep stopped · passenger alighting":boarding?"Brake held · boarding in progress":distance.ToString("F0")+" m  ·  "+(behind?"Turn around safely":aboard?"Follow the route":"Passenger waiting · "+Mathf.Max(0,patience).ToString("F0")+"s"),17,uiMuted);
            if(boarding||unloading)Meter(new Rect(934,739,212,8),boardingProgress,uiTeal);else if(Action(new Rect(934,720,212,56),distance<=7?(aboard?"E  DELIVER":"E  BOARD"):"E  INTERACT",distance<=7))Interact();
            Panel(new Rect(1320,671,240,187));Write(new Rect(1340,687,200,26),"SPEED",15,uiMuted);Write(new Rect(1334,714,135,78),(driver.Speed*3.6f).ToString("F0"),64,paper,FontStyle.Bold);Write(new Rect(1473,750,70,30),"km/h",19,uiMuted);
            Write(new Rect(1340,815,200,24),driver.Grounded?(driver.IsParked?"BRAKE HOLD":driver.SignedSpeed<-.5f?"REVERSE":"DRIVE") : "SETTLING",16,uiTeal,FontStyle.Bold);
            if(mouseControls){
                Write(new Rect(432,822,225,25),"HOLD TO DRIVE  ",14,uiMuted);
                bool left=Hold(new Rect(634,815,63,43),"A",ShiftInput.Turn<0),forward=Hold(new Rect(705,815,63,43),"W",ShiftInput.Throttle>0),back=Hold(new Rect(776,815,63,43),"S",ShiftInput.Throttle<0),right=Hold(new Rect(847,815,63,43),"D",ShiftInput.Turn>0),brake=Hold(new Rect(918,815,122,43),"STOP",ShiftInput.Brake||driver.IsParked);
                ShiftInput.Virtual(forward?1:back?-1:0,left?-1:right?1:0,brake);
            }
            if(!Application.isFocused){Panel(new Rect(490,382,620,112));Write(new Rect(516,404,568,66),"CLICK THE GAME WINDOW\nThen use WASD or the drive buttons.",25,paper,FontStyle.Bold,TextAnchor.MiddleCenter);}
            else Write(new Rect(40,876,1500,22),"WASD / ARROWS  DRIVE     SPACE  STOP / HOLD     H  HORN     E  INTERACT     ESC  PAUSE     R  RECOVER ($1)",13,uiMuted,FontStyle.Normal,TextAnchor.MiddleCenter);
        }
        bool Hold(Rect rect,string label,bool active)
        {
            if(label=="STOP"&&Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))driver.RequestStop();
            if(quietStyle==null)quietStyle=new GUIStyle{font=worldFont,fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            Fill(rect,active?uiAccent:new Color(.11f,.2f,.23f,.96f));quietStyle.normal.textColor=active?ink:paper;return GUI.RepeatButton(rect,label,quietStyle);
        }
    }
}
