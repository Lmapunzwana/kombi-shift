using UnityEngine;
namespace KombiShift.JaydenStarter {
 [CreateAssetMenu(menuName="Kombi Shift/Game tuning")]
 public sealed class GameTuning:ScriptableObject {
  [Header("Vehicle - metres, seconds, degrees")][Min(1)]public float acceleration=13,maximumSpeed=20,steering=92;
  [Header("City")][Min(30)]public float citySeconds=300;[Min(1)]public float cityTarget=18;[Range(1,100)]public float cityFuel=100;[Range(0,9)]public int cityTraffic=9;
  [Header("Highway")][Min(30)]public float highwaySeconds=300;[Min(1)]public float highwayTarget=28;[Range(1,100)]public float highwayFuel=45;[Range(0,9)]public int highwayTraffic=4;
  [Header("People and economy")][Range(.5f,2)]public float patienceScale=1;[Range(.5f,2)]public float rivalAggressionScale=1;
  public void Apply(LevelDefinition l,bool wet){l.seconds=wet?highwaySeconds:citySeconds;l.target=wet?highwayTarget:cityTarget;l.startingFuel=wet?highwayFuel:cityFuel;l.trafficCount=wet?highwayTraffic:cityTraffic;l.waitingPatience*=patienceScale;l.aggression*=rivalAggressionScale;}
 }
}
