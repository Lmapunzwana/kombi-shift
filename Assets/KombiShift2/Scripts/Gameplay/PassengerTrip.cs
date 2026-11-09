using System;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    public enum TripState { Onboard, Requested, Missed, Alighting, Completed }
    [Serializable]
    public sealed class PassengerTrip
    {
        public StationPassenger passenger;
        public int boardingStation, destination;
        public int fare = 2;
        public float happiness = 100f, patienceSeconds = 240f, boardedAt;
        public TripState state;
        public bool requested, missed;
        public float closestDistance = float.MaxValue;
        public void Adjust(float amount) => happiness = Mathf.Clamp(happiness + amount, 0f, 100f);
        public bool Request(float distance, float radius)
        {
            if (requested || state == TripState.Completed || distance > radius) return false;
            requested = true; state = TripState.Requested; return true;
        }
        public bool CheckMissed(float distance, float radius)
        {
            closestDistance = Mathf.Min(closestDistance, distance);
            if (!requested || missed || closestDistance > radius || distance < radius * 1.6f) return false;
            missed = true; state = TripState.Missed; Adjust(-20f); return true;
        }
    }
}
