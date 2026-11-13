using KombiShift2.Gameplay;
using UnityEngine;

namespace KombiShift2.Vehicle
{
    public enum KombiTurnSignal { Off, Left, Right }

    // Project-owned keyboard integration; RCC retains lamp animation, flares and audio.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RCCP_CarController))]
    public sealed class KombiTurnIndicators : MonoBehaviour
    {
        RCCP_CarController vehicle;
        RCCP_Lights lights;
        Light[] indicatorLamps=System.Array.Empty<Light>();
        KombiShiftGameDirector director;
        public KombiTurnSignal Signal { get; private set; }

        void Awake()
        {
            vehicle=GetComponent<RCCP_CarController>();lights=GetComponentInChildren<RCCP_Lights>(true);
            var found=new System.Collections.Generic.List<Light>();
            foreach(var lamp in GetComponentsInChildren<RCCP_Light>(true))
                if(lamp.lightType==RCCP_Light.LightType.IndicatorLeftLight || lamp.lightType==RCCP_Light.LightType.IndicatorRightLight)
                {var source=lamp.GetComponent<Light>();if(source!=null)found.Add(source);}
            indicatorLamps=found.ToArray();
        }
        bool CanUse()
        {
            if(director==null)director=FindAnyObjectByType<KombiShiftGameDirector>();
            return isActiveAndEnabled && vehicle!=null && Time.timeScale>0f &&
                director!=null && !director.Paused && !director.Finished;
        }
        void Update()
        {
            if(!CanUse())return;
            if(Input.GetKeyDown(KeyCode.X))Cancel();
            else if(Input.GetKeyDown(KeyCode.Q))ToggleLeft();
            else if(Input.GetKeyDown(KeyCode.E))ToggleRight();
        }
        void LateUpdate()
        {
            if(director!=null && director.Finished)Cancel();
            Apply();
        }
        public void ToggleLeft(){if(CanUse())Select(Signal==KombiTurnSignal.Left?KombiTurnSignal.Off:KombiTurnSignal.Left);}
        public void ToggleRight(){if(CanUse())Select(Signal==KombiTurnSignal.Right?KombiTurnSignal.Off:KombiTurnSignal.Right);}
        public void Cancel(){Select(KombiTurnSignal.Off);}
        void Select(KombiTurnSignal signal){Signal=signal;Apply();}
        void Apply()
        {
            if(vehicle==null)return;
            vehicle.indicatorsLeftLights=Signal==KombiTurnSignal.Left;
            vehicle.indicatorsRightLights=Signal==KombiTurnSignal.Right;
            vehicle.indicatorsAllLights=false;
            // Clear the rendered lamps too: RCC can retain its last illuminated blink frame.
            if(Signal==KombiTurnSignal.Off)
                foreach(var lamp in indicatorLamps)if(lamp!=null){lamp.intensity=0f;lamp.enabled=false;}
            // External RCC driving input does not forward dashboard flags to the lamp manager.
            if(lights!=null)
            {
                lights.indicatorsLeft=Signal==KombiTurnSignal.Left;
                lights.indicatorsRight=Signal==KombiTurnSignal.Right;
                lights.indicatorsAll=false;
            }
        }
        void OnDisable(){Cancel();}
    }
}
