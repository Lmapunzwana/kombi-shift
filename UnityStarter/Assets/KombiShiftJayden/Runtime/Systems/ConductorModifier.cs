namespace KombiShift.JaydenStarter {
 public static class ConductorModifier {
  public const float DailyCost=3;
  public static float BoardingSeconds(bool hired)=>hired?.72f:1.35f;
 }
}
