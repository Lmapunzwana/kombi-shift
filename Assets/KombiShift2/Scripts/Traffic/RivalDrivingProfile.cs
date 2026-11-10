using System;
using UnityEngine;

namespace KombiShift2.Traffic
{
    public enum RivalPersonality { Conservative, Competitive, Aggressive }
    [Serializable]
    public sealed class RivalDrivingProfile
    {
        public RivalPersonality personality = RivalPersonality.Conservative;
        [Min(0f)] public float passengerValue = 8f, travelCost = .12f, congestionPenalty = 4f, reservationPenalty = 30f;
        [Min(1f)] public float reassessSeconds = 4f, switchAdvantage = 3f, followingSeconds = 2f;
        [Range(0f, 35f)] public float maxSteering = 32f;
        [Min(.1f)] public float acceleration = 2f, braking = 4f;
        public bool allowOvertaking;
        public float speedMultiplier = 1f;
        public static RivalDrivingProfile For(RivalPersonality kind) => new() {
            personality = kind,
            allowOvertaking = kind != RivalPersonality.Conservative,
            speedMultiplier = kind == RivalPersonality.Aggressive ? 1.42f : kind == RivalPersonality.Competitive ? 1.28f : .9f,
            followingSeconds = kind == RivalPersonality.Aggressive ? .8f : kind == RivalPersonality.Competitive ? 1f : 2f,
            reassessSeconds = kind == RivalPersonality.Aggressive ? 1f : kind == RivalPersonality.Competitive ? 1.5f : 4f,
            switchAdvantage = kind == RivalPersonality.Aggressive ? 1f : kind == RivalPersonality.Competitive ? 2f : 3f,
            acceleration = kind == RivalPersonality.Aggressive ? 4.2f : kind == RivalPersonality.Competitive ? 3.4f : 2f,
            passengerValue = kind == RivalPersonality.Aggressive ? 14f : kind == RivalPersonality.Competitive ? 11f : 8f };
        public float Utility(int demand, int seats, float distance, float speed, int congestion, bool reserved)
            => Mathf.Min(demand, seats) * passengerValue - distance / Mathf.Max(1f, speed) * travelCost - congestion * congestionPenalty - (reserved ? reservationPenalty : 0f);
    }
}
