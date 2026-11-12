using KombiShift2.Gameplay;
using TMPro;
using UnityEngine;

namespace KombiShift2.UI
{
    // A scene-owned, non-interactive overlay. It reads production physics, never simulated UI values.
    public sealed class KombiSpeedometer : MonoBehaviour
    {
        RCCP_CarController vehicle;
        KombiShiftGameDirector director;
        Rigidbody body;
        Canvas canvas;
        RectTransform panel;
        KombiSpeedometerDial dial;
        TMP_FontAsset font, displayFont;
        TextMeshProUGUI speedText, distanceText, averageText, maximumText, elapsedText, stoppedText, gearText;
        float nextText, nextBuildAttempt;
        bool ready;
        public float SpeedKph {get;private set;}
        public float DistanceMetres {get;private set;}
        public float ElapsedSeconds {get;private set;}
        public float StoppedSeconds {get;private set;}
        public float MaximumSpeedKph {get;private set;}
        public float AverageSpeedKph => ElapsedSeconds>0f ? DistanceMetres/ElapsedSeconds*3.6f : 0f;

        public void Configure(RCCP_CarController car,KombiShiftGameDirector shift)
        {
            if(vehicle!=car)
            {DistanceMetres=ElapsedSeconds=StoppedSeconds=MaximumSpeedKph=SpeedKph=0f;}
            vehicle=car;director=shift;body=car!=null?car.GetComponent<Rigidbody>():null;
            if(!ready)TryBuild();
        }
        void TryBuild()
        {
            if(ready || Time.unscaledTime<nextBuildAttempt)return;
            nextBuildAttempt=Time.unscaledTime+1f;
            // Never create TMP components before their shared settings/shaders are installed.
            // In the Editor this also avoids opening TMP's importer from a running scene.
            if(Resources.Load<TMP_Settings>("TMP Settings")==null)return;
            var source=Resources.Load<Font>("Fonts/BarlowCondensed-SemiBold");
            var display=Resources.Load<Font>("Fonts/BarlowCondensed-ExtraBoldItalic");
            if(source==null || display==null || Shader.Find("TextMeshPro/Mobile/Distance Field")==null)return;
            if(canvas!=null)Destroy(canvas.gameObject);
            font=TMP_FontAsset.CreateFontAsset(source);displayFont=TMP_FontAsset.CreateFontAsset(display);
            if(font==null || displayFont==null){Release(font);Release(displayFont);font=displayFont=null;return;}

            var go=new GameObject("Speedometer Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
            go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,800);scaler.matchWidthOrHeight=.5f;
            var holder=new GameObject("Orange trip speedometer",typeof(RectTransform),typeof(CanvasRenderer));holder.transform.SetParent(go.transform,false);
            panel=holder.GetComponent<RectTransform>();panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,0);panel.sizeDelta=new Vector2(250,348);panel.localScale=Vector3.one*.7f;panel.anchoredPosition=new Vector2(-18,18);
            font.material.EnableKeyword("OUTLINE_ON");displayFont.material.EnableKeyword("OUTLINE_ON");
            font.material.SetFloat(ShaderUtilities.ID_OutlineWidth,.18f);displayFont.material.SetFloat(ShaderUtilities.ID_OutlineWidth,.18f);
            var graphic=new GameObject("Segmented orange dial",typeof(RectTransform),typeof(CanvasRenderer),typeof(KombiSpeedometerDial));graphic.transform.SetParent(panel,false);
            var rect=graphic.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(250,250);rect.anchoredPosition=Vector2.zero;
            dial=graphic.GetComponent<KombiSpeedometerDial>();dial.raycastTarget=false;
            Label("Unit",new Vector2(125,-69),new Vector2(90,22),"km/h",17,new Color(.72f,.73f,.72f));
            speedText=Label("Actual speed",new Vector2(125,-117),new Vector2(142,68),"0",62,Color.white,true);
            Label("Minimum",new Vector2(50,-184),new Vector2(50,26),"0",21,Color.white);
            Label("Maximum scale",new Vector2(201,-184),new Vector2(54,26),"240",21,Color.white);
            distanceText=Label("Trip distance",new Vector2(125,-214),new Vector2(174,25),"0.0 m",20,new Color(.65f,.67f,.65f));
            gearText=Label("Gear",new Vector2(125,-242),new Vector2(150,20),"N  /  PARKED",13,new Color(.65f,.67f,.65f));
            Label("Average label",new Vector2(62,-271),new Vector2(114,17),"AVG. SPEED",13,new Color(.64f,.66f,.64f));
            Label("Maximum label",new Vector2(190,-271),new Vector2(114,17),"MAX. SPEED",13,new Color(.64f,.66f,.64f));
            averageText=Label("Average value",new Vector2(62,-288),new Vector2(114,20),"0 km/h",17,Color.white);
            maximumText=Label("Maximum value",new Vector2(190,-288),new Vector2(114,20),"0 km/h",17,Color.white);
            Label("Time label",new Vector2(62,-311),new Vector2(114,17),"TIME",13,new Color(.64f,.66f,.64f));
            Label("Stopped label",new Vector2(190,-311),new Vector2(114,17),"STOP TIME",13,new Color(.64f,.66f,.64f));
            elapsedText=Label("Elapsed value",new Vector2(62,-328),new Vector2(114,20),"00:00",17,Color.white);
            stoppedText=Label("Stopped value",new Vector2(190,-328),new Vector2(114,20),"00:00",17,Color.white);
            ready=true;
        }
        TextMeshProUGUI Label(string name,Vector2 position,Vector2 size,string content,float pointSize,Color color,bool display=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(panel,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=go.GetComponent<TextMeshProUGUI>();text.font=display&&displayFont!=null?displayFont:font;
            text.text=content;text.fontSize=pointSize;text.color=color;text.alignment=TextAlignmentOptions.Center;
            text.textWrappingMode=TextWrappingModes.NoWrap;text.raycastTarget=false;text.margin=Vector4.zero;
            return text;
        }
        void FixedUpdate()
        {
            if(vehicle==null||director==null||director.Paused||director.Finished||Time.timeScale==0f)return;
            // Integrating velocity avoids counting spawn/recovery teleports as distance.
            Vector3 velocity=body!=null?body.linearVelocity:Vector3.zero;velocity.y=0;
            SpeedKph=Mathf.Max(0f,vehicle.absoluteSpeed);
            DistanceMetres+=velocity.magnitude*Time.fixedDeltaTime;
            ElapsedSeconds+=Time.fixedDeltaTime;
            if(SpeedKph<.5f)StoppedSeconds+=Time.fixedDeltaTime;
            MaximumSpeedKph=Mathf.Max(MaximumSpeedKph,SpeedKph);
        }
        void LateUpdate()
        {
            if(!ready){TryBuild();if(!ready)return;}
            canvas.enabled=vehicle!=null&&director!=null&&!director.Paused;
            if(!canvas.enabled)return;
            // Safe-area insets are screen pixels; convert to the scaled canvas coordinate system.
            var safe=Screen.safeArea;
            panel.anchoredPosition=new Vector2(-(Screen.width-safe.xMax)/canvas.scaleFactor-18,safe.yMin/canvas.scaleFactor+18);
            dial.SetSpeed(SpeedKph);
            if(Time.unscaledTime<nextText)return;nextText=Time.unscaledTime+.1f;
            speedText.SetText("{0:0}",SpeedKph);
            distanceText.text=DistanceMetres<1000f?$"{DistanceMetres:0.0} m":$"{DistanceMetres/1000f:0.00} km";
            averageText.text=$"{AverageSpeedKph:0} km/h";maximumText.text=$"{MaximumSpeedKph:0} km/h";
            elapsedText.text=Clock(ElapsedSeconds);stoppedText.text=Clock(StoppedSeconds);
            gearText.text=vehicle.direction<0?"R  /  REVERSE":SpeedKph<.5f?"N  /  PARKED":$"D{Mathf.Max(1,vehicle.currentGear+1)}  /  DRIVING";
        }
        static string Clock(float seconds)
        {int value=Mathf.FloorToInt(seconds);return value>=3600?$"{value/3600:0}:{value/60%60:00}:{value%60:00}":$"{value/60:00}:{value%60:00}";}
        void OnEnable()
        {
            if(vehicle==null)
            {
                var hud=GetComponent<KombiNavigationHud>();
                if(hud!=null){vehicle=hud.Vehicle;director=hud.Director;body=vehicle!=null?vehicle.GetComponent<Rigidbody>():null;}
            }
            if(canvas!=null)canvas.enabled=true;
        }
        void OnDisable(){if(canvas!=null)canvas.enabled=false;}
        void OnDestroy()
        {
            if(canvas!=null)Destroy(canvas.gameObject);
            Release(font);Release(displayFont);
        }
        static void Release(TMP_FontAsset asset)
        {
            if(asset==null)return;
            if(asset.material!=null)Destroy(asset.material);
            foreach(var texture in asset.atlasTextures)if(texture!=null)Destroy(texture);
            Destroy(asset);
        }
    }
}
