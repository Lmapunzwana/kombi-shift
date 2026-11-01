using System;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        enum HwindiState{NotHired,Riding,Calling,Boarding,TakingFare,Signalling,Entering}
        Transform hwindiActor,hwindiLeftArm,hwindiRightArm,hwindiLeftHand,hwindiRightHand,departingRider,slidingDoor;
        HwindiState hwindiState;float hwindiReadyAt,doorAmount,nextHwindiCall;bool hwindiDepartureRequested,hwindiCollecting;
        AudioSource hwindiVoice;AudioClip idleRecording,hornClip,doorClip,brakeClip,fareClip,callClip,readyClip,fareVoiceClip;
        bool wasBraking;readonly float[] audioMeterSamples=new float[512];float outputRms,peakOutputRms;
        string HwindiDescription=>hwindiState==HwindiState.NotHired?"Not hired":hwindiState==HwindiState.Calling?"Calling passengers":hwindiState==HwindiState.Boarding?"Helping boarding":hwindiState==HwindiState.TakingFare?"Collecting fare":hwindiState==HwindiState.Signalling?"Signalling ready":hwindiState==HwindiState.Entering?"Closing door":"On board";
        void CreateHwindi(Material skin)
        {
            hwindiActor=Person("Hwindi",Vector3.zero,C("686453"),skin);int arm=0;
            foreach(Transform child in hwindiActor)if(child.name=="Arm"){if(arm++==0)hwindiLeftArm=child;else hwindiRightArm=child;}
            hwindiLeftHand=Shape("Left hand",PrimitiveType.Sphere,new Vector3(-.3f,1.3f,0),Vector3.one*.13f,skin,hwindiActor,false).transform;hwindiRightHand=Shape("Right hand",PrimitiveType.Sphere,new Vector3(.3f,1.3f,0),Vector3.one*.13f,skin,hwindiActor,false).transform;
            var cloth=Mat(C("3D423D"));var bag=Mat(C("675746"));
            Box("Conductor shoulder bag",new Vector3(.17f,.87f,.18f),new Vector3(.3f,.4f,.15f),bag,hwindiActor);
            Shape("Cap",PrimitiveType.Sphere,new Vector3(0,1.8f,0),new Vector3(.33f,.13f,.34f),cloth,hwindiActor,false);
            Box("Cap brim",new Vector3(0,1.77f,.18f),new Vector3(.28f,.035f,.18f),cloth,hwindiActor);
        }
        Vector3 OutsideDoor(){var p=driver.transform.TransformPoint(new Vector3(-1.6f,0,.55f));p.y=.02f;return p;}
        void ResetHwindi()
        {
            hwindiDepartureRequested=hwindiCollecting=false;hwindiReadyAt=0;nextHwindiCall=0;doorAmount=0;
            if(slidingDoor!=null)slidingDoor.localPosition=new Vector3(0,0,.45f);
            if(departingRider!=null)departingRider.gameObject.SetActive(false);
            if(hwindiActor!=null){hwindiActor.gameObject.SetActive(hwindi);hwindiActor.position=OutsideDoor();}
            if(hwindiVoice!=null)hwindiVoice.Stop();hwindiState=hwindi?HwindiState.Riding:HwindiState.NotHired;
        }
        public bool RequestDeparture(){hwindiDepartureRequested=true;return !boarding&&!unloading&&Time.time>=hwindiReadyAt&&doorAmount<=.04f;}
        void UpdateHwindi()
        {
            if(hwindiActor==null||driver==null)return;
            hwindiActor.gameObject.SetActive(hwindi);
            if(!IsPlaying){if(hwindi){hwindiActor.position=OutsideDoor();hwindiActor.rotation=driver.transform.rotation;hwindiLeftArm.localRotation=Quaternion.Euler(0,0,12);hwindiRightArm.localRotation=Quaternion.Euler(0,0,-12);}return;}
            if(!driver.IsParked&&!boarding&&!unloading)hwindiDepartureRequested=false;
            if((boarding||unloading))hwindiDepartureRequested=false;

            bool settling=Time.time<hwindiReadyAt;
            bool calling=hwindi&&!aboard&&!boarding&&!unloading&&!settling&&!hwindiDepartureRequested&&driver.IsParked&&Vector3.Distance(driver.transform.position,Target)<16;
            bool outside=boarding||unloading||calling||(settling&&hwindiReadyAt-Time.time>.35f);
            float doorTarget=outside?1:0;
            float previousDoor=doorAmount;doorAmount=Mathf.MoveTowards(doorAmount,doorTarget,Time.deltaTime*2.7f);if(previousDoor>.04f&&doorAmount<=.04f)PlayDoor();
            if(slidingDoor!=null)slidingDoor.localPosition=new Vector3(-.045f*doorAmount,0,.45f-1.4f*doorAmount);
            if(hwindi){
                hwindiState=boarding?HwindiState.Boarding:unloading?HwindiState.TakingFare:calling?HwindiState.Calling:settling?(hwindiCollecting&&hwindiReadyAt-Time.time>.4f?HwindiState.TakingFare:HwindiState.Signalling):doorAmount>.05f?HwindiState.Entering:HwindiState.Riding;
                if(outside){hwindiActor.position=Vector3.Lerp(hwindiActor.position,OutsideDoor(),1-Mathf.Exp(-12*Time.deltaTime));Vector3 facing=rider.position-hwindiActor.position;facing.y=0;if(facing.sqrMagnitude>.01f)hwindiActor.rotation=Quaternion.LookRotation(facing);}
                else {var seated=driver.transform.TransformPoint(new Vector3(-.5f,.12f,.45f));hwindiActor.position=doorAmount>.05f?Vector3.Lerp(hwindiActor.position,seated,1-Mathf.Exp(-10*Time.deltaTime)):seated;hwindiActor.rotation=driver.transform.rotation;}
                float wave=Mathf.Sin(Time.time*7);
                hwindiLeftArm.localPosition=new Vector3(-.29f,calling?1.52f:boarding?1.26f:1f,calling?.08f:boarding?.2f:0);
                hwindiLeftArm.localRotation=Quaternion.Euler(boarding?60:0,0,calling?20+wave*28:boarding?30:unloading?25:0);
                hwindiRightArm.localPosition=new Vector3(.29f,settling?1.4f:unloading?1.25f:1f,unloading?.2f:0);
                hwindiRightArm.localRotation=Quaternion.Euler(settling?wave*20:0,0,settling?-65:unloading?-45:-8);
                hwindiLeftHand.localPosition=hwindiLeftArm.localPosition+hwindiLeftArm.localRotation*Vector3.up*.28f;hwindiRightHand.localPosition=hwindiRightArm.localPosition+hwindiRightArm.localRotation*Vector3.up*.28f;
                if(calling&&Time.time>=nextHwindiCall){nextHwindiCall=Time.time+9;if(callClip!=null)hwindiVoice.PlayOneShot(callClip,.7f);Notify("Hwindi: City rank! One more passenger!");}
            }else hwindiState=HwindiState.NotHired;
        }
        AudioClip MakeEffect(string name,float seconds,Func<float,float,float> sound)
        {
            const int rate=22050;int count=Mathf.CeilToInt(seconds*rate);var data=new float[count];var random=new System.Random(1049);
            for(int i=0;i<count;i++){float t=(float)i/rate;data[i]=Mathf.Clamp(sound(t,(float)random.NextDouble()*2-1),-.8f,.8f);}
            var clip=AudioClip.Create(name,count,1,rate,false);clip.SetData(data,0);clips.Add(clip);return clip;
        }
        void InitializeWorkingAudio()
        {
            AudioListener.volume=1;AudioListener.pause=false;
            effects=gameObject.AddComponent<AudioSource>();music=gameObject.AddComponent<AudioSource>();engine=gameObject.AddComponent<AudioSource>();hwindiVoice=gameObject.AddComponent<AudioSource>();
            effects.spatialBlend=music.spatialBlend=engine.spatialBlend=hwindiVoice.spatialBlend=0;
            idleRecording=Resources.Load<AudioClip>("Audio/BusIdle");callClip=Resources.Load<AudioClip>("Audio/HwindiCall");readyClip=Resources.Load<AudioClip>("Audio/HwindiReady");fareVoiceClip=Resources.Load<AudioClip>("Audio/HwindiFare");
            hornClip=MakeEffect("Twin tone horn",.32f,(t,n)=>(Mathf.Sin(t*2*Mathf.PI*350)+.65f*Mathf.Sin(t*2*Mathf.PI*440))*.27f*Mathf.Min(1,t*100)*Mathf.Min(1,(.32f-t)*80));
            doorClip=MakeEffect("Sliding door and latch",.6f,(t,n)=>n*.2f*Mathf.Sin(t*Mathf.PI/.6f)+n*.55f*Mathf.Exp(-Mathf.Abs(t-.51f)*110));
            brakeClip=MakeEffect("Brake stop",.3f,(t,n)=>n*.15f*Mathf.Exp(-t*14));
            fareClip=MakeEffect("Coins",.34f,(t,n)=>Mathf.Sin(t*2*Mathf.PI*1950)*.3f*(Mathf.Exp(-t*35)+.6f*Mathf.Exp(-Mathf.Abs(t-.15f)*60)));
            cue=fareClip;warning=MakeEffect("Vehicle warning",.18f,(t,n)=>Mathf.Sin(t*2*Mathf.PI*210)*.3f*Mathf.Sin(t*Mathf.PI/.18f));
            musicClip=AmbientLoop();music.clip=musicClip;music.loop=true;music.volume=musicVolume*.4f;music.Play();
            engine.clip=idleRecording!=null?idleRecording:MakeEffect("Diesel fallback",1,(t,n)=>(Mathf.Sin(t*2*Mathf.PI*70)*.25f+n*.08f)*(1+.25f*Mathf.Sin(t*2*Mathf.PI*28)));
            engine.loop=true;engine.volume=0;engine.Play();hwindiVoice.volume=effectsVolume*.65f;hwindiVoice.priority=32;
            foreach(var car in traffic){car.sound=car.transform.gameObject.AddComponent<AudioSource>();car.sound.clip=engine.clip;car.sound.loop=true;car.sound.spatialBlend=1;car.sound.minDistance=3;car.sound.maxDistance=32;car.sound.rolloffMode=AudioRolloffMode.Linear;car.sound.volume=0;car.sound.Play();}
        }
        public void PlayHorn(){if(effects!=null&&hornClip!=null)effects.PlayOneShot(hornClip,Mathf.Max(.25f,effectsVolume)*.65f);}
        void PlayDoor(){effects.PlayOneShot(doorClip,effectsVolume*.6f);}
        void PlayFare(){effects.PlayOneShot(fareClip,effectsVolume*.6f);}
        void PlayCollectionVoice(){if(hwindi&&fareVoiceClip!=null){hwindiVoice.Stop();hwindiVoice.PlayOneShot(fareVoiceClip,.7f);}}
        void PlayReady(){if(readyClip!=null){hwindiVoice.Stop();hwindiVoice.PlayOneShot(readyClip,.7f);}}
        void UpdateEngineSound()
        {
            engine.volume=IsPlaying?effectsVolume*(.25f+Mathf.Abs(driver.Throttle)*.08f+driver.Speed*.003f):0;
            engine.pitch=Mathf.Lerp(engine.pitch,.9f+driver.Speed*.035f+Mathf.Abs(driver.Throttle)*.18f,Time.unscaledDeltaTime*6);
            hwindiVoice.volume=effectsVolume*.65f;
            if(ShiftInput.Brake&&!wasBraking&&driver.Speed>1)effects.PlayOneShot(brakeClip,effectsVolume*.7f);wasBraking=ShiftInput.Brake;
            foreach(var car in traffic)if(car.sound!=null){car.sound.volume=IsPlaying?effectsVolume*.15f:0;car.sound.pitch=car.atStop?.9f:1.08f;}
            AudioListener.GetOutputData(audioMeterSamples,0);float sum=0;foreach(float value in audioMeterSamples)sum+=value*value;outputRms=Mathf.Sqrt(sum/audioMeterSamples.Length);peakOutputRms=Mathf.Max(peakOutputRms,outputRms);
        }
    }
}
