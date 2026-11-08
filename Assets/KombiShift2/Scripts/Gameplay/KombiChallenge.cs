using UnityEngine;

namespace KombiShift2.Gameplay
{
    public enum KombiStage { Morning, RushHour }
    // Two challenge configurations share the city, prefabs and tested route.
    public static class KombiChallenge
    {
        public static KombiStage Selected = KombiStage.Morning;
        public static string Name(KombiStage stage) => stage == KombiStage.Morning ? "MORNING SHIFT" : "RUSH HOUR";
        public static int MinimumPassengers(KombiStage stage) => stage == KombiStage.Morning ? 1 : 6;
        public static float Seconds(KombiStage stage) => stage == KombiStage.Morning ? 600f : 420f;
        public static float RivalSpeed(KombiStage stage) => stage == KombiStage.Morning ? 6f : 9f;
        public static float RivalDelay(KombiStage stage) => stage == KombiStage.Morning ? 45f : 8f;
        public static int Best(KombiStage stage) => PlayerPrefs.GetInt("KombiShift2.Best." + stage, 0);
        public static void SaveBest(KombiStage stage, int earnings)
        {
            PlayerPrefs.SetInt("KombiShift2.Best." + stage, Mathf.Max(Best(stage), earnings));
            PlayerPrefs.Save();
        }
    }
}
