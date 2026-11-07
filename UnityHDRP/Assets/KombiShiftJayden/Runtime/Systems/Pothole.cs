using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class Pothole:MonoBehaviour
    {
        ShiftGame game;float damage,nextHit;
        public void Configure(ShiftGame owner,float amount){game=owner;damage=amount;nextHit=0;}
        void OnTriggerEnter(Collider other){var car=other.GetComponent<KombiDriver>();if(car!=null&&game.IsPlaying&&car.Speed>4&&Time.time>=nextHit){nextHit=Time.time+2;game.Damage(damage);}}
    }
}
