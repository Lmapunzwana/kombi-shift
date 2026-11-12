using System.Collections;
using KombiShift2.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KombiShift2.UI
{
    public sealed class KombiMainMenu : MonoBehaviour
    {
        const float H=900;
        Texture2D pixel;Font displayFont,bodyFont;
        GUIStyle logo,section,body,cardTitle,cardDetail,tiny;
        AudioSource audioSource;AudioClip hoverClip,clickClip;
        readonly float[] highlights=new float[3];
        int selected=-1;Vector3 lastMouse;float enterTime,transition,volume,sensitivity,cameraDistance;
        KombiTripMarket market;
        bool settings,controls,starting;int lastSound=-1;
        readonly string[] titles={"TRIP MARKET","HOW TO PLAY","SETTINGS"};
        readonly string[] details={"CHOOSE A CITY ROUTE","KEYBOARD + MOUSE","AUDIO + CAMERA"};
        public int SelectedItem=>selected;
        public bool Starting=>starting;
        public bool SettingsOpen=>settings;
        public bool ControlsOpen=>controls;
        public float TransitionProgress=>transition;
        public void ClosePanel(){settings=controls=false;}
        public float Highlight(int index)=>highlights[index];
        public static Rect CardRect(int index,float width)=>new(58+index*320,H-226,304,112);

        void Awake()
        {
            Time.timeScale=1;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
            enterTime=Time.unscaledTime;lastMouse=Input.mousePosition;
            displayFont=Resources.Load<Font>("Fonts/BarlowCondensed-ExtraBoldItalic");bodyFont=Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.playOnAwake=false;
            hoverClip=Sound("Menu focus",720f,.045f);clickClip=Sound("Menu select",300f,.11f);
            volume=PlayerPrefs.GetFloat("KombiShift.MasterVolume",.8f);AudioListener.volume=volume;
            sensitivity=PlayerPrefs.GetFloat("Kombi.CameraSensitivity",1.6f);cameraDistance=PlayerPrefs.GetFloat("Kombi.CameraDistance",7.6f);selected=0;
        }
        void Update()
        {
            float step=Time.unscaledDeltaTime;
            for(int i=0;i<3;i++)highlights[i]=Mathf.MoveTowards(highlights[i],selected==i&&!settings&&!controls?1:0,step*7f);
            if(starting || (market!=null&&market.IsOpen))return;
            if(Input.GetKeyDown(KeyCode.Escape)){settings=controls=false;return;}
            if(settings||controls)return;
            if(Input.GetKeyDown(KeyCode.F2)) KombiChallenge.Selected = KombiChallenge.Selected == KombiStage.Morning ? KombiStage.RushHour : KombiStage.Morning;
            if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.Tab)||Input.GetKeyDown(KeyCode.DownArrow))Focus((selected+1)%3);
            if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.UpArrow))Focus((selected+2+3)%3);
            if(Input.GetKeyDown(KeyCode.Return))Activate(selected<0?0:selected);
        }
        public void Focus(int index)
        {
            if(index==selected)return;selected=index;
            if(index>=0&&lastSound!=index){audioSource.PlayOneShot(hoverClip,.16f);lastSound=index;}
        }
        public void Activate(int index)
        {
            if(starting)return;audioSource.PlayOneShot(clickClip,.3f);
            if(index==0)
            {
                if(market==null){market=gameObject.AddComponent<KombiTripMarket>();market.StartRequested+=()=>StartCoroutine(StartShift());}
                market.Open();
            }
            else if(index==1)controls=true;
            else settings=true;
        }
        IEnumerator StartShift()
        {
            starting=true;
            while(transition<1f){transition+=Time.unscaledDeltaTime*2.5f;yield return null;}
            var load=SceneManager.LoadSceneAsync("KombiShift_FirstShift");
            while(!load.isDone)yield return null;
        }
        void OnGUI()
        {
            if(market!=null&&market.IsOpen)return;
            Styles();float scale=Mathf.Min(Screen.width/1280f,Screen.height/H);
            float w=Screen.width/scale,h=Screen.height/scale;
            var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);GUI.color=Color.white;
            float appear=Mathf.Clamp01((Time.unscaledTime-enterTime)*1.8f);
            // Sparse race-menu framing leaves the live vehicle visible.
            Fill(new Rect(0,0,w,6),new Color(.92f,.63f,.18f));
            Fill(new Rect(0,6,w,88),new Color(.025f,.038f,.044f,.83f));
            Text(new Rect(58,28,350,30),"K / S     HARARE, ZIMBABWE",section,new Color(.9f,.91f,.84f));
            Text(new Rect(w-390,28,330,30),KombiChallenge.Name(KombiChallenge.Selected),section,new Color(.93f,.67f,.26f));
            Text(new Rect(54,116+(1-appear)*20,600,132),"KOMBI",logo,new Color(.95f,.96f,.91f,appear));
            Text(new Rect(54,218+(1-appear)*20,600,132),"SHIFT",logo,new Color(.95f,.66f,.22f,appear));
            Fill(new Rect(60,372,48,4),new Color(.95f,.66f,.22f));
            Text(new Rect(60,397,440,40),"BE FIRST AT THE RANK.",section,Color.white);
            Text(new Rect(60,442,400,60),"Pick up the passengers. Outrun your rivals.\nBring the fares home.",body,new Color(.74f,.81f,.81f));
            Text(new Rect(w-390,115,330,30),"TOYOTA HIACE  /  COMMUTER",tiny,new Color(.82f,.86f,.85f));
            Text(new Rect(w-390,150,330,36),"CITY  /  ROUTES",section,Color.white);
            float rowY=h-226;
            if(!starting&&!settings&&!controls)
            {
                Rect stageButton = new Rect(60,rowY-68,620,42);
                if(GUI.Button(stageButton, "F2 / CHANGE STAGE: " + KombiChallenge.Name(KombiChallenge.Selected), section))
                    KombiChallenge.Selected = KombiChallenge.Selected == KombiStage.Morning ? KombiStage.RushHour : KombiStage.Morning;
                Text(new Rect(60,rowY-105,750,30), $"{KombiChallenge.Seconds(KombiChallenge.Selected)/60:0} MIN   /   DELIVER {KombiChallenge.MinimumPassengers(KombiChallenge.Selected)}+   /   BEST ${KombiChallenge.Best(KombiChallenge.Selected)}", tiny, Color.white);
            }
            bool pointerMoved=(Input.mousePosition-lastMouse).sqrMagnitude>.1f;lastMouse=Input.mousePosition;
            if(!settings&&!controls&&!starting)
            {
                int over=-1;
                for(int i=0;i<3;i++)
                {
                    Rect rect=CardRect(i,w);rect.y=rowY;
                    if(rect.Contains(Event.current.mousePosition))over=i;
                    DrawCard(i,rect);
                }
                if(pointerMoved&&over>=0)Focus(over);
            }
            else if(!starting){for(int i=0;i<3;i++){Rect rect=CardRect(i,w);rect.y=rowY;DrawCardBackground(i,rect,.15f);}}
            Fill(new Rect(0,h-72,w,72),new Color(.02f,.03f,.036f,.95f));
            Text(new Rect(58,h-55,850,30),"ENTER  SELECT      /      ARROWS  NAVIGATE      /      ESC  BACK",tiny,new Color(.72f,.79f,.79f));
            Rect exit=new Rect(w-160,h-55,100,30);
            GUI.color=exit.Contains(Event.current.mousePosition)?new Color(.95f,.66f,.22f):Color.white;
            if(!starting&&GUI.Button(exit,"EXIT",tiny))Quit();GUI.color=Color.white;
            if(settings||controls)Modal(w,h);
            if(starting)
            {
                Fill(new Rect(0,0,w,h),new Color(.015f,.024f,.029f,Mathf.Clamp01(transition)));
                Text(new Rect(w*.5f-220,h*.5f-36,440,62),"STARTING YOUR SHIFT",cardTitle,Color.white);
                Fill(new Rect(w*.5f-220,h*.5f+40,440,4),new Color(.2f,.25f,.26f));
                Fill(new Rect(w*.5f-220,h*.5f+40,440*Mathf.Clamp01(transition),4),new Color(.95f,.66f,.22f));
            }
            GUI.matrix=previous;GUI.color=Color.white;
        }
        void DrawCard(int index,Rect rect)
        {
            float t=highlights[index];rect.y-=t*7;
            DrawCardBackground(index,rect,t);
            bool hover=rect.Contains(Event.current.mousePosition);
            if(hover&&Event.current.type==EventType.MouseDown&&Event.current.button==0){Focus(index);Activate(index);Event.current.Use();}
            Text(new Rect(rect.x+20,rect.y+10,250,20),"0"+(index+1)+"  /  "+(index==0?"DRIVE":index==1?"LEARN":"TUNE"),tiny,Color.Lerp(new Color(.61f,.71f,.72f),new Color(.18f,.20f,.16f),t));
            Text(new Rect(rect.x+18,rect.y+34,270,42),titles[index],cardTitle,Color.Lerp(Color.white,new Color(.05f,.085f,.09f),t));
            Text(new Rect(rect.x+20,rect.y+82,270,21),(index==0 ? KombiChallenge.Name(KombiChallenge.Selected) : details[index]),cardDetail,Color.Lerp(new Color(.70f,.78f,.79f),new Color(.2f,.25f,.22f),t));
        }
        void DrawCardBackground(int index,Rect rect,float amount)
        {
            Fill(rect,Color.Lerp(new Color(.035f,.058f,.067f,.97f),new Color(.97f,.70f,.30f,1),amount));
            Fill(new Rect(rect.x,rect.y,4,rect.height),index==0||amount>.1f?new Color(.98f,.68f,.24f):new Color(.22f,.34f,.36f));
            Fill(new Rect(rect.x+20,rect.yMax-2,Mathf.Lerp(20,rect.width-40,amount),2),new Color(.98f,.68f,.24f));
        }
        void Modal(float w,float h)
        {
            Fill(new Rect(0,0,w,h),new Color(.01f,.02f,.025f,.85f));
            Rect panel=new(w*.5f-320,h*.5f-236,640,472);Fill(panel,new Color(.04f,.065f,.076f));
            Fill(new Rect(panel.x,panel.y,640,4),new Color(.96f,.66f,.22f));
            Text(new Rect(panel.x+34,panel.y+26,560,54),settings?"SETTINGS":"HOW TO PLAY",cardTitle,Color.white);
            if(settings)
            {
                Slider(panel,110,"MASTER VOLUME",ref volume,0,1);AudioListener.volume=volume;
                Slider(panel,208,"MOUSE SENSITIVITY",ref sensitivity,.5f,3f);
                Slider(panel,306,"CHASE DISTANCE",ref cameraDistance,4.5f,12f);
            }
            else
            {
                Text(new Rect(panel.x+34,panel.y+105,565,300),"W / S     Accelerate, brake and reverse\nA / D     Steer       SPACE     Handbrake\nRMB DRAG     Orbit or look around\nMOUSE WHEEL     Zoom chase camera\nC / CAMERA BUTTON     Change view\n\nStop below 2 km/h. Hold Space while boarding.\nListen for stop requests; stop to let passengers alight.\nChoose a route in Trip Market. Fares pay on delivery.\nT repairs at stops. Fines reduce earnings and XP.\nF2 on title changes stage. Escape pauses. R restarts.",body,new Color(.84f,.89f,.88f));
            }
            Rect back=new(panel.x+34,panel.yMax-58,572,36);
            Fill(back,back.Contains(Event.current.mousePosition)?new Color(.72f,.48f,.15f):new Color(.13f,.24f,.27f));
            if(GUI.Button(back,settings?"SAVE & RETURN":"RETURN",section))
            {
                PlayerPrefs.SetFloat("KombiShift.MasterVolume",volume);PlayerPrefs.SetFloat("Kombi.CameraSensitivity",sensitivity);PlayerPrefs.SetFloat("Kombi.CameraDistance",cameraDistance);PlayerPrefs.Save();
                audioSource.PlayOneShot(clickClip,.25f);settings=controls=false;
            }
        }
        void Slider(Rect p,float y,string name,ref float value,float min,float max)
        {
            Text(new Rect(p.x+34,p.y+y,500,30),name+"   "+(max==1?Mathf.RoundToInt(value*100)+"%":value.ToString("0.0")),tiny,new Color(.8f,.87f,.85f));
            value=GUI.HorizontalSlider(new Rect(p.x+34,p.y+y+40,570,28),value,min,max);
        }
        void Styles()
        {
            if(logo!=null)return;
            logo=Style(displayFont,132);section=Style(bodyFont,27);body=Style(bodyFont,20);cardTitle=Style(displayFont,38);cardDetail=Style(bodyFont,17);tiny=Style(bodyFont,20);
        }
        static GUIStyle Style(Font font,int size){var style=new GUIStyle(GUI.skin.label){font=font,fontSize=size,wordWrap=false};style.normal.textColor=Color.white;return style;}
        void Text(Rect rect,string value,GUIStyle style,Color color){if(style==section||style==tiny||style==cardDetail)rect.height=Mathf.Max(rect.height,style.lineHeight+4f);GUI.color=color;GUI.Label(rect,value,style);GUI.color=Color.white;}
        void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,pixel);GUI.color=Color.white;}
        static AudioClip Sound(string name,float frequency,float duration)
        {
            int count=(int)(22050*duration);var values=new float[count];
            for(int i=0;i<count;i++){float t=i/22050f;values[i]=Mathf.Sin(t*frequency*Mathf.PI*2)*Mathf.Exp(-t/duration*8f)*.24f;}
            var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(values,0);return clip;
        }
        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy(){if(pixel!=null)Destroy(pixel);if(hoverClip!=null)Destroy(hoverClip);if(clickClip!=null)Destroy(clickClip);}
    }
}
