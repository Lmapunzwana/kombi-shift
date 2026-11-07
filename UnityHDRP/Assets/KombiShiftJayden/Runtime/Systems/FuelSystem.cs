using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class FuelSystem:MonoBehaviour
    {
        public float Amount{get;set;}public float PurchaseCost{get;private set;}
        LevelDefinition level;GameManager events;
        public void Configure(LevelDefinition value,GameManager bus){level=value;events=bus;Amount=level.startingFuel;PurchaseCost=0;events.PublishFuel(Amount);}
        public void Consume(float metres,float seconds){Amount=Mathf.Max(0,Amount-metres*level.fuelPerMetre-seconds*.018f);events.PublishFuel(Amount);}
        public bool Refill(ref float cash,out float bought,out float cost)
        {
            bought=Mathf.Min(20,100-Amount,Mathf.Max(0,cash)/level.fuelPrice);cost=bought*level.fuelPrice;
            if(bought<.1f){bought=cost=0;return false;}cash-=cost;Amount+=bought;PurchaseCost+=cost;events.PublishFuel(Amount);return true;
        }
    }
}
