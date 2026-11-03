using UnityEngine;
namespace KombiShift.JaydenStarter {
 public sealed class VehicleWear:MonoBehaviour {
  [Range(0,100)]public float brakes=100,suspension=100;
  public float BrakeStrength=>Mathf.Lerp(.55f,1,brakes/100);
  public float GripStrength=>Mathf.Lerp(.6f,1,suspension/100);
  public void ResetWear(){brakes=suspension=100;}
  public void Brake(float speed,float dt){if(speed>4)brakes=Mathf.Max(0,brakes-speed*dt*.025f);}
  public void Impact(float damage){suspension=Mathf.Max(0,suspension-damage*.7f);brakes=Mathf.Max(0,brakes-damage*.15f);}
 }
}
