using UnityEngine;
namespace KombiShift.JaydenStarter {
 public sealed partial class ShiftGame {
  public string ScreenName=>screen.ToString(); public string RivalStateName=>rival==null?"Not ready":rival.Current.ToString();
  public int OnboardCount=>manifest.Count;public float GrossFares=>gross;public float Cash=>wallet;public float ShiftElapsed=>elapsed;
  public void EditorStartCity(){Rainy=false;Begin();}public void EditorStartHighway(){Rainy=true;Begin();}
  public void EditorGoToStop(int index){if(!Application.isPlaying||driver==null)return;stopIndex=Mathf.Clamp(index,0,stops.Length-1);driver.Body.position=Target+Vector3.up*.3f;driver.Body.rotation=Quaternion.identity;driver.ParkNow();fuelPreviousPosition=driver.Body.position;PositionPassenger();}
  public void EditorService()=>Interact();public void EditorDamage()=>Damage(15);
  public void EditorRestore(){Condition=100;Wear.ResetWear();fuelSystem.Amount=100;Fuel=100;wallet=25;}
  public void EditorPause()=>Pause(IsPlaying);public void EditorRunChecks()=>StartCoroutine(RunSmokeCheck());
  // Art catalog generation uses the same authored geometry as gameplay, then exports persistent assets.
  public void BuildArtPreview(){BuildWorld();}
 }
}
