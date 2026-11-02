using System;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class GameManager:MonoBehaviour
    {
        public event Action<float> OnFareEarned,OnFuelChanged,OnVehicleDamaged;
        public event Action<string> OnShiftEnded;
        public LevelDefinition Level{get;private set;}public bool Ended{get;private set;}
        public void StartShift(LevelDefinition level){Level=level;Ended=false;}
        public void PublishFare(float amount)=>OnFareEarned?.Invoke(amount);
        public void PublishFuel(float amount)=>OnFuelChanged?.Invoke(amount);
        public void PublishDamage(float amount)=>OnVehicleDamaged?.Invoke(amount);
        public string Evaluate(float banked,float fuel,float health,float elapsed)
        {
            if(Ended)return null;if(health<=0)return "Vehicle unusable";if(fuel<=0)return "Out of fuel";
            if(banked>=Level.target)return "Fare target reached!";
            if(elapsed>=Level.seconds)return "Shift ended below target";return null;
        }
        public void End(string reason){if(Ended)return;Ended=true;OnShiftEnded?.Invoke(reason);}
    }
}
