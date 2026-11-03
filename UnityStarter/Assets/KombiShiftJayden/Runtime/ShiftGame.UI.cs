using System.Collections.Generic;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        readonly Dictionary<int,GUIStyle> uiStyles=new Dictionary<int,GUIStyle>();
        GUIStyle actionStyle,quietStyle,mapStyle;bool mouseControls=true,touchControls;float hudTextScale=1;
        readonly Color uiInk=new Color(.075f,.08f,.08f,.96f),uiMuted=new Color(.68f,.69f,.67f),uiAccent=new Color(.74f,.78f,.77f),uiTeal=new Color(.71f,.76f,.76f);
        float ScreenWidth=>UnityEngine.Screen.width;float ScreenHeight=>UnityEngine.Screen.height;
        void Fill(Rect r,Color color){Color old=GUI.color;GUI.color=QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Panel(Rect r){Fill(r,uiInk);Fill(new Rect(r.x,r.y,r.width,1),new Color(.5f,.7f,.7f,.18f));}
        void Write(Rect r,string text,int size,Color color,FontStyle weight=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft)
        {
            size=Mathf.RoundToInt(size*(screen==Screen.Playing?hudTextScale:1));int key=size*100+(int)weight*10+(int)align;
            if(!uiStyles.TryGetValue(key,out var style)){style=new GUIStyle{font=worldFont,fontSize=size,fontStyle=weight,alignment=align,wordWrap=true};uiStyles[key]=style;}
            style.normal.textColor=QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;GUI.Label(r,text,style);
        }
        bool Action(Rect r,string value,bool primary=false)
        {
            if(actionStyle==null){actionStyle=new GUIStyle{font=worldFont,fontSize=22,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};actionStyle.normal.textColor=paper;quietStyle=new GUIStyle(actionStyle);}
            Color bg=primary?uiAccent:new Color(.22f,.25f,.25f);if(r.Contains(Event.current.mousePosition))bg=primary?new Color(.85f,.87f,.86f):new Color(.3f,.33f,.33f);
            Fill(r,bg);actionStyle.normal.textColor=QualitySettings.activeColorSpace==ColorSpace.Linear?(primary?ink.linear:paper.linear):(primary?ink:paper);return GUI.Button(r,value,actionStyle);
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
                if(Action(new Rect(104,710,200,38),"TEST SOUND"))PlayHorn(true);
                Write(new Rect(106,765,500,58),"ENTER to drive  •  WASD / arrows to steer\nClick the game window to give it keyboard focus.",18,uiMuted);
                Panel(new Rect(1120,700,416,130));Write(new Rect(1146,723,360,24),"NEXT SHIFT / BEST "+PlayerPrefs.GetFloat("Kombi.Best."+(Rainy?"Highway":"City"),0).ToString("F1"),16,uiTeal,FontStyle.Bold);
                Write(new Rect(1146,758,360,36),Rainy?"02  /  RAINY HIGHWAY":"01  /  CBD MORNING",25,paper,FontStyle.Bold);
                return;
            }
            if(screen==Screen.Selection){
                Panel(new Rect(350,90,900,720));Write(new Rect(398,129,800,28),"CHOOSE YOUR SHIFT",18,uiTeal,FontStyle.Bold);Write(new Rect(398,179,800,60),"Choose your shift.",42,paper,FontStyle.Bold);
                if(Action(new Rect(398,280,380,164),"01  CITY MORNING\nDry road · $8 remittance",!Rainy))Rainy=false;
                if(Action(new Rect(800,280,402,164),"02  RAINY HIGHWAY\nLong routes · tighter fuel",Rainy))Rainy=true;
                Write(new Rect(400,474,790,60),"Reach the fare target before time or fuel runs out. Passengers have individual destinations and delivery deadlines.",23,uiMuted);
                if(Action(new Rect(398,559,804,52),hwindi?"HWINDI HIRED     $3 / shift — faster boarding":"ADD A HWINDI   +$3 / shift — faster boarding",hwindi))hwindi=!hwindi;
                if(Action(new Rect(398,650,540,64),"LET'S DRIVE   ",true))Begin();if(Action(new Rect(958,650,244,64),"BACK"))screen=Screen.Menu;return;
            }
            Panel(new Rect(430,75,740,750));Write(new Rect(478,108,640,30),screen==Screen.Instructions?"DRIVER'S GUIDE":screen==Screen.Paused?"TAKE A BREATHER":"SHIFT STATEMENT",18,uiTeal,FontStyle.Bold);
            if(screen==Screen.Instructions){
                Write(new Rect(478,153,640,60),"Get moving. Get paid.",42,paper,FontStyle.Bold);
                string[] labels={"01  DRIVE","02  PICK UP","03  DELIVER","04  MANAGE YOUR SHIFT"};
                string[] bodies={"WASD or arrows to drive. SPACE brakes. Mouse-held drive buttons also work. ENTER starts a shift.","Follow the marked stop bay. Stop inside the marked bay, press E or click BOARD. Stay still while your passenger boards.","Use the map to choose pickup or destination stops. Deliver before patience expires; distance sets the fare and quick trips earn tips.","E buys fuel near a station using your available cash. Collisions cost health and your clean-driving bonus. ESC pauses; R recovers ($1)."};
                for(int i=0;i<4;i++){Write(new Rect(478,238+i*113,640,28),labels[i],18,uiAccent,FontStyle.Bold);Write(new Rect(478,273+i*113,640,78),bodies[i],23,paper);}
                if(Action(new Rect(478,737,640,52),"GOT IT — BACK",true))screen=Screen.Menu;
            }
            else if(screen==Screen.Paused){
                Write(new Rect(478,167,640,70),"Shift paused.",48,paper,FontStyle.Bold);Write(new Rect(478,259,640,36),"Music  "+Mathf.RoundToInt(musicVolume*100)+"%",24,paper);GUI.color=uiAccent;musicVolume=GUI.HorizontalSlider(new Rect(478,309,640,30),musicVolume,0,1);GUI.color=Color.white;
                Write(new Rect(478,365,640,36),"Effects  "+Mathf.RoundToInt(effectsVolume*100)+"%",24,paper);GUI.color=uiAccent;effectsVolume=GUI.HorizontalSlider(new Rect(478,414,640,30),effectsVolume,0,1);GUI.color=Color.white;music.volume=musicVolume*.4f;
                Write(new Rect(478,454,260,30),"HUD text "+Mathf.RoundToInt(hudTextScale*100)+"%",20,paper);hudTextScale=GUI.HorizontalSlider(new Rect(740,465,375,25),hudTextScale,.85f,1.1f);
                if(Action(new Rect(478,494,640,42),touchControls?"CONTROLS: TOUCH JOYSTICK":"CONTROLS: KEYBOARD / BUTTONS"))touchControls=!touchControls;
                if(Action(new Rect(478,552,640,54),"RESUME DRIVING   ",true)){SaveSettings();Pause(false);}
                if(Action(new Rect(478,625,640,54),"RESTART SHIFT")){SaveSettings();Begin();}
                if(Action(new Rect(478,699,640,54),"RETURN TO MENU")){SaveSettings();Menu();}
            }
            else if(screen==Screen.Statement){
                bool success=outcome=="Fare target reached!";Write(new Rect(478,158,640,60),success?"A shift well earned.":"Next shift, come back stronger.",36,paper,FontStyle.Bold);
                Write(new Rect(478,242,640,30),delivered+" TRIPS COMPLETED  /  "+missed+" MISSED",18,uiMuted);
                string[] labels={"Distance fares","Quick-delivery tips","Fuel purchased","Damage & recovery","Remittance","Hwindi"};float[] values={gross-tipsTotal,tipsTotal,-fuelCost,-repairCost,-Remittance,-ConductorCost};
                for(int i=0;i<6;i++){Write(new Rect(478,297+i*39,420,35),labels[i],24,uiMuted);Write(new Rect(930,297+i*39,188,35),(values[i]<0?"−$":"$")+Mathf.Abs(values[i]).ToString("F2"),24,paper,FontStyle.Bold,TextAnchor.UpperRight);}
                Fill(new Rect(478,548,640,1),uiMuted);Write(new Rect(478,573,320,38),"TAKE-HOME",20,uiAccent,FontStyle.Bold);Write(new Rect(830,559,288,75),"$"+TakeHome.ToString("F2"),48,paper,FontStyle.Bold,TextAnchor.UpperRight);
                if(Action(new Rect(478,662,405,62),"DRIVE AGAIN",true))Begin();if(Action(new Rect(900,662,218,62),"MENU"))Menu();
                Write(new Rect(478,746,640,60),outcome+"  ·  Clean bonus "+noCrashBonus.ToString("F0")+"  ·  Score "+(gross+noCrashBonus).ToString("F1"),20,uiMuted);
            }
        }
        void DrawHUD()
        {
            Panel(new Rect(40,32,310,116));Fill(new Rect(40,32,4,116),uiAccent);Write(new Rect(62,48,270,24),"KOMBI SHIFT   /   FARES",16,uiMuted,FontStyle.Bold);Write(new Rect(59,81,270,54),"$"+gross.ToString("F2"),42,paper,FontStyle.Bold);
            Panel(new Rect(370,32,330,116));Write(new Rect(394,49,282,28),Rainy?"02  RAINY HIGHWAY":"01  CITY MORNING",18,uiTeal,FontStyle.Bold);Write(new Rect(394,86,282,32),"TARGET $"+FareTarget.ToString("F0")+"  /  CASH $"+wallet.ToString("F2"),22,paper);
            Panel(new Rect(1090,32,470,116));float remaining=Mathf.Max(0,shiftSeconds-elapsed);Write(new Rect(1114,50,200,24),"SHIFT REMAINING",16,uiMuted);Write(new Rect(1114,83,200,50),((int)remaining/60).ToString("00")+":"+((int)remaining%60).ToString("00"),34,paper,FontStyle.Bold);
            Write(new Rect(1333,51,200,24),"FUEL  "+Fuel.ToString("F0")+"%",17,Fuel<20?uiAccent:paper);Meter(new Rect(1333,82,200,6),Fuel/100,uiTeal);Write(new Rect(1333,101,200,24),"CONDITION  "+Condition.ToString("F0")+"%",17,paper);Meter(new Rect(1333,129,200,4),Condition/100,Condition<35?uiAccent:uiTeal);
            if(Action(new Rect(728,32,110,42),"PAUSE"))Pause(true);if(Action(new Rect(850,32,195,42),"NEAREST STOP"))stopIndex=BestStop();
            if(AtFuel&&Action(new Rect(1090,185,220,42),"REPAIR  $3"))RepairAtStation();
            if(Time.unscaledTime<messageUntil){Panel(new Rect(40,169,800,57));Write(new Rect(60,181,760,40),message,21,paper);}
            DrawStopMap();
            DrawManifest();
            Panel(new Rect(1090,153,470,28));Write(new Rect(1102,156,445,22),"BRAKES "+Wear.brakes.ToString("F0")+"%   SUSPENSION "+Wear.suspension.ToString("F0")+"%",14,uiMuted);
            Write(new Rect(1340,535,200,22),"HWINDI",15,uiMuted);Write(new Rect(1340,562,200,27),hwindi?HwindiDescription:"Not hired",17,paper);
            Panel(new Rect(40,671,360,187));Write(new Rect(62,691,316,25),"FARE MULTIPLIER",16,uiMuted,FontStyle.Bold);Write(new Rect(62,728,205,52),fare.ToString("F1")+"x",36,paper,FontStyle.Bold);
            if(Action(new Rect(231,723,64,52),"−"))fare=Mathf.Max(.5f,Mathf.Round((fare-.1f)*10)/10);if(Action(new Rect(306,723,64,52),"+"))fare=Mathf.Min(2,Mathf.Round((fare+.1f)*10)/10);
            fare=Mathf.Round(GUI.HorizontalSlider(new Rect(62,784,308,18),fare,.5f,2)*10)/10;
            Write(new Rect(62,810,316,35),fare>1.6f?"Premium fare - some passengers refuse":"Fare tolerance varies by passenger",18,fare>1.6f?uiAccent:uiMuted);
            Panel(new Rect(430,671,740,132));Write(new Rect(453,689,692,23),unloading?"PASSENGER ALIGHTING":boarding?"PASSENGER BOARDING":HasDropoff()?"NEXT DESTINATION":"NEXT PICKUP",15,uiTeal,FontStyle.Bold);
            float distance=Vector3.Distance(driver.transform.position,Target);Vector3 direction=(Target-driver.transform.position).normalized;bool behind=Vector3.Dot(driver.transform.forward,direction)<-.2f;
            Write(new Rect(453,725,474,41),stopNames[stopIndex],30,paper,FontStyle.Bold);Write(new Rect(453,770,450,24),unloading?"Keep stopped · passenger alighting":boarding?"Brake held · boarding in progress":distance.ToString("F0")+" m  ·  "+(behind?"Turn around safely":aboard?"Follow the route":"Passenger waiting · "+Mathf.Max(0,patience).ToString("F0")+"s"),17,uiMuted);
            if(boarding||unloading)Meter(new Rect(934,739,212,8),boardingProgress,uiTeal);else if(Action(new Rect(934,720,212,56),AtTarget?(HasDropoff()?"E  DROP OFF":"E  BOARD"):AtFuel?"E  BUY FUEL":"E  INTERACT",AtTarget||AtFuel))Interact();
            Panel(new Rect(1320,671,240,187));Write(new Rect(1340,687,200,26),"SPEED",15,uiMuted);Write(new Rect(1334,714,135,78),(driver.Speed*3.6f).ToString("F0"),64,paper,FontStyle.Bold);Write(new Rect(1473,750,70,30),"km/h",19,uiMuted);
            Write(new Rect(1340,815,200,24),driver.Grounded?(driver.IsParked?"BRAKE HOLD":driver.SignedSpeed<-.5f?"REVERSE":"DRIVE") : "SETTLING",16,uiTeal,FontStyle.Bold);
            if(touchControls)DrawTouchControls();
            else if(mouseControls){
                Write(new Rect(432,822,225,25),"HOLD TO DRIVE  ",14,uiMuted);
                bool left=Hold(new Rect(634,815,63,43),"A",ShiftInput.Turn<0),forward=Hold(new Rect(705,815,63,43),"W",ShiftInput.Throttle>0),back=Hold(new Rect(776,815,63,43),"S",ShiftInput.Throttle<0),right=Hold(new Rect(847,815,63,43),"D",ShiftInput.Turn>0),brake=Hold(new Rect(918,815,122,43),"STOP",ShiftInput.Brake||driver.IsParked);
                ShiftInput.Virtual(forward?1:back?-1:0,left?-1:right?1:0,brake);
            }
            if(!Application.isFocused&&!diagnosticSession){Panel(new Rect(490,382,620,112));Write(new Rect(516,404,568,66),"CLICK THE GAME WINDOW\nThen use WASD or the drive buttons.",25,paper,FontStyle.Bold,TextAnchor.MiddleCenter);}
            else Write(new Rect(40,876,1500,22),"WASD / ARROWS  DRIVE     SPACE  STOP / HOLD     H  HORN     E  INTERACT     ESC  PAUSE     R  RECOVER ($1)",13,uiMuted,FontStyle.Normal,TextAnchor.MiddleCenter);
        }
        bool HasDropoff(){foreach(var p in manifest)if(p.destination==stopIndex||p.state==PassengerState.Expired)return true;return false;}
        void DrawStopMap()
        {
            Panel(new Rect(1320,211,240,389));Write(new Rect(1340,229,205,26),Rainy?"HIGHWAY MAP":"CBD MAP",16,uiMuted,FontStyle.Bold);
            if(mapStyle==null)mapStyle=new GUIStyle{font=worldFont,fontSize=13,alignment=TextAnchor.MiddleCenter,wordWrap=true};mapStyle.normal.textColor=paper;
            float first=stops[0].z,last=Rainy?465:145;
            for(int i=0;i<stops.Length;i++){
                float x=Rainy?1340:stops[i].x>20?1455:1340,y=280+Mathf.InverseLerp(first,last,stops[i].z)*150;int seats=0,queue=0;
                foreach(var p in manifest)if(p.destination==i)seats++;foreach(var p in waiting)if(p.origin==i&&p.state==PassengerState.Waiting)queue++;
                var r=new Rect(x,y,Rainy?200:85,Rainy?44:60);Fill(r,i==stopIndex?new Color(.33f,.4f,.4f):new Color(.17f,.2f,.2f));if(GUI.Button(r,stopNames[i]+"\n"+queue+" waiting / "+seats+" drop",mapStyle)){stopIndex=i;PositionPassenger();}
            }
            float px=Rainy?1330:Mathf.Lerp(1330,1450,Mathf.InverseLerp(-5,40,driver.transform.position.x));float py=280+Mathf.InverseLerp(first,last,driver.transform.position.z)*150;Fill(new Rect(px,py,8,8),paper);
            Write(new Rect(1340,492,200,38),"Click a stop to plan your route.",14,uiMuted);
        }
        void DrawManifest()
        {
            if(manifest.Count==0)return;Panel(new Rect(40,247,345,48+manifest.Count*47));Write(new Rect(58,262,310,25),"PASSENGERS "+manifest.Count+" / "+Capacity,16,paper,FontStyle.Bold);
            for(int i=0;i<manifest.Count;i++){var p=manifest[i];bool late=p.state==PassengerState.Expired;float y=300+i*47;Write(new Rect(58,y,310,25),(i+1)+"  "+stopNames[p.destination]+"  "+(late?"LATE":p.remaining.ToString("F0")+"s")+"  $"+p.agreedFare.ToString("F2"),16,late?new Color(.9f,.65f,.5f):paper);Meter(new Rect(58,y+28,308,5),p.remaining/p.maximum,late?new Color(.9f,.65f,.5f):uiTeal);}
        }
        void DrawTouchControls()
        {
            var pad=new Rect(445,485,160,160);Panel(pad);Write(new Rect(452,495,146,24),"STEER / DRIVE",13,uiMuted);Vector2 delta=Vector2.zero;
            if(Event.current.mousePosition.x>=pad.x&&pad.Contains(Event.current.mousePosition)&&Input.GetMouseButton(0))delta=(Event.current.mousePosition-pad.center)/65;
            bool brake=false;
            for(int i=0;i<Input.touchCount;i++){var t=Input.GetTouch(i);if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)continue;Vector3 point=GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(t.position.x,ScreenHeight-t.position.y,0));if(pad.Contains(point))delta=((Vector2)point-pad.center)/65;if(new Rect(918,815,122,43).Contains(point))brake=true;}
            delta=Vector2.ClampMagnitude(delta,1);Fill(new Rect(pad.center.x+delta.x*50-12,pad.center.y+delta.y*50-12,24,24),uiAccent);ShiftInput.Touch(-delta.y,delta.x,brake);if(brake)driver.RequestStop();
            if(Action(new Rect(918,815,122,43),"STOP"))driver.RequestStop();
        }
        bool Hold(Rect rect,string label,bool active)
        {
            if(label=="STOP"&&Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))driver.RequestStop();
            if(quietStyle==null)quietStyle=new GUIStyle{font=worldFont,fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            Fill(rect,active?uiAccent:new Color(.11f,.2f,.23f,.96f));quietStyle.normal.textColor=active?ink:paper;return GUI.RepeatButton(rect,label,quietStyle);
        }
    }
}
