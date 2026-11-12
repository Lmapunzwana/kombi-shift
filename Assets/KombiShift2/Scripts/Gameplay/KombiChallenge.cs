using UnityEngine;

namespace KombiShift2.Gameplay
{
    public enum KombiStage { Morning, RushHour }
    // Two challenge configurations share the city, prefabs and tested route.
    public static class KombiChallenge
    {
        public static KombiStage Selected = KombiStage.Morning;
        public static string Name(KombiStage stage) => stage == KombiStage.Morning ? "MORNING SHIFT" : "RUSH HOUR";
        public static int MinimumPassengers(KombiStage stage) => new KombiShiftConfiguration(stage).DeliveryTarget;
        public static float Seconds(KombiStage stage) => new KombiShiftConfiguration(stage).Duration;
        public static float RivalSpeed(KombiStage stage) => new KombiShiftConfiguration(stage).RivalSpeed;
        public static float RivalDelay(KombiStage stage) => new KombiShiftConfiguration(stage).RivalDelay;
        public static int Best(KombiStage stage) => PlayerPrefs.GetInt("KombiShift2.Best." + stage, 0);
        public static void SaveBest(KombiStage stage, int earnings)
        {
            PlayerPrefs.SetInt("KombiShift2.Best." + stage, Mathf.Max(Best(stage), earnings));
            PlayerPrefs.Save();
        }
    }
}
