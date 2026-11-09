using UnityEngine;

namespace KombiShift2.Gameplay
{
    [CreateAssetMenu(menuName = "Kombi Shift/Stage Settings")]
    public sealed class KombiStageSettings : ScriptableObject
    {
        [Min(60f)] public float duration = 600f;
        [Min(1)] public int deliveryTarget = 1;
        [Min(1f)] public float rivalSpeed = 6f;
        [Min(0f)] public float rivalHeadStart = 45f;
        [Range(.1f,1f)] public float trafficFraction = .6f;
        [Min(1)] public int fare = 2;
        public static KombiStageSettings Load(KombiStage stage) => Resources.Load<KombiStageSettings>(stage == KombiStage.Morning ? "MorningShift" : "RushHour");
    }
}
