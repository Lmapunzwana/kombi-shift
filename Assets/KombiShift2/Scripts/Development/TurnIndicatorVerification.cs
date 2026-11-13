#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KombiShift2.Gameplay;
using KombiShift2.Vehicle;
using UnityEngine;

public sealed class TurnIndicatorVerification : MonoBehaviour
{
    [Serializable] sealed class Result
    {
        public string unity,scope="Live RCC lamp integration using production Q/E/X handlers; not a human keyboard playtest";
        public int frameStart,frameEnd,leftLamps,rightLamps;
        public List<string> passed=new(),failed=new();
    }
    static TurnIndicatorVerification active;
    readonly Result result=new();
    KombiTurnIndicators indicators;RCCP_CarController car;string folder;
    RCCP_Light[] left,right;
    void Awake(){if(active!=null&&active!=this)Destroy(active.gameObject);active=this;}
    void Check(bool condition,string name){(condition?result.passed:result.failed).Add(name);}
    IEnumerator Start()
    {
        Application.runInBackground=true;
        folder=Path.GetFullPath("Docs/Validation/TurnIndicators");Directory.CreateDirectory(folder);
        result.unity=Application.unityVersion;result.frameStart=Time.frameCount;
        Debug.Log("KOMBI_INDICATORS_VERIFICATION_START");
        yield return null;yield return null;
        var director=FindAnyObjectByType<KombiShiftGameDirector>();
        car=director!=null?director.Vehicle:null;
        if(car==null){Check(false,"Driving-scene player exists");Save();yield break;}
        indicators=car.GetComponent<KombiTurnIndicators>();
        Check(indicators!=null,"Driving bootstrap installs exactly one indicator controller");
        if(indicators==null){Save();yield break;}
        Check(car.GetComponents<KombiTurnIndicators>().Length==1,"No duplicate indicator controller");
        var lamps=car.GetComponentsInChildren<RCCP_Light>(true);
        left=lamps.Where(l=>l.lightType==RCCP_Light.LightType.IndicatorLeftLight).ToArray();
        right=lamps.Where(l=>l.lightType==RCCP_Light.LightType.IndicatorRightLight).ToArray();
        result.leftLamps=left.Length;result.rightLamps=right.Length;
        Check(left.Length>=2&&right.Length>=2,"Existing front and rear indicator lamps are present on both sides");
        indicators.Cancel();yield return new WaitForSeconds(.7f);
        indicators.ToggleLeft();
        Check(car.indicatorsLeftLights&&!car.indicatorsRightLights&&!car.indicatorsAllLights,"Q handler selects only left");
        yield return Observe(left,right,"left");
        indicators.ToggleRight();
        Check(car.indicatorsRightLights&&!car.indicatorsLeftLights,"E handler switches directly from left to right");
        yield return Observe(right,left,"right");
        indicators.ToggleRight();
        Check(indicators.Signal==KombiTurnSignal.Off&&!car.indicatorsRightLights,"Pressing active direction again cancels");
        indicators.ToggleLeft();indicators.Cancel();
        Check(!car.indicatorsLeftLights&&!car.indicatorsRightLights&&!car.indicatorsAllLights,"X handler cancels both sides");
        director.SetPaused(true);indicators.ToggleLeft();
        Check(indicators.Signal==KombiTurnSignal.Off,"Paused controls do not select a signal");
        director.SetPaused(false);
        indicators.ToggleRight();indicators.enabled=false;
        Check(!car.indicatorsLeftLights&&!car.indicatorsRightLights,"Disabling controller clears indicators");
        indicators.enabled=true;
        yield return new WaitForSeconds(.8f);
        yield return new WaitForEndOfFrame();
        Check(left.Concat(right).All(l=>Power(l)<.05f),"All actual lamps extinguish after cancellation");
        Check(Time.frameCount>result.frameStart+20,"The observation advanced real rendered frames");
        result.frameEnd=Time.frameCount;Save();
    }
    static float Power(RCCP_Light lamp)
    {var light=lamp.GetComponent<Light>();return light!=null&&light.enabled?light.intensity:0f;}
    IEnumerator Observe(RCCP_Light[] selected,RCCP_Light[] other,string side)
    {
        yield return new WaitForSeconds(.8f);
        float min=float.PositiveInfinity,max=0f,opposite=0f;bool captured=false;
        float[] minimum=Enumerable.Repeat(float.PositiveInfinity,selected.Length).ToArray(),maximum=new float[selected.Length];
        float end=Time.time+3f;
        while(Time.time<end)
        {
            for(int i=0;i<selected.Length;i++)
            {float power=Power(selected[i]);minimum[i]=Mathf.Min(minimum[i],power);maximum[i]=Mathf.Max(maximum[i],power);}
            float value=selected.Length>0?selected.Max(Power):0f;
            min=Mathf.Min(min,value);max=Mathf.Max(max,value);
            opposite=Mathf.Max(opposite,other.Length>0?other.Max(Power):0f);
            if(!captured&&value>.1f)
            {
                captured=true;yield return new WaitForEndOfFrame();
                var shot=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(folder,side+"-indicator.png"),shot.EncodeToPNG());Destroy(shot);
            }
            yield return null;
        }
        Check(max>.1f&&min<max*.5f,side+" actual lamps cycle on and off");
        Check(opposite<.05f,side+" does not illuminate the opposite side");
        Check(selected.Length>=2&&Enumerable.Range(0,selected.Length).All(i=>maximum[i]>.1f&&minimum[i]<maximum[i]*.5f),side+" front and rear lamps each flash");
        Debug.Log($"KOMBI_INDICATORS_LAMPS {side} min={min} max={max} opposite={opposite}");
    }
    void Save()
    {
        if(indicators!=null)indicators.Cancel();
        File.WriteAllText(Path.Combine(folder,"live-verification.json"),JsonUtility.ToJson(result,true));
        Debug.Log("KOMBI_INDICATORS_VERIFICATION_DONE "+JsonUtility.ToJson(result));
    }
    void OnDestroy(){if(active==this)active=null;}
}
#endif
