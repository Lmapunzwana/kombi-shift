using System;
using System.Collections.Generic;

namespace KombiShift2.Gameplay
{
    // Integer cents keep fractional delivery fares exact. The director preserves the public API.
    public sealed class KombiShiftLedger
    {
        readonly HashSet<PassengerTrip> paid = new();
        long grossCents, fineCents, repairCents, bonusCents;
        public float GrossEarnings => grossCents / 100f;
        public float NetEarnings => (grossCents - fineCents - repairCents + bonusCents) / 100f;
        public int RepairExpenses => (int)(repairCents / 100);
        public float TimeBonus => bonusCents / 100f;
        public bool CreditDelivery(PassengerTrip trip)
        {
            if (trip == null || trip.state != TripState.Completed || !paid.Add(trip)) return false;
            grossCents += Cents(trip.fare);
            return true;
        }
        public bool TryRepair(int amount)
        {
            long cost = (long)amount * 100;
            if (amount < 0 || grossCents - fineCents - repairCents + bonusCents < cost) return false;
            repairCents += cost;
            return true;
        }
        public void Fine(int amount) => fineCents += (long)Math.Max(0, amount) * 100;
        public void AwardTimeBonus(float seconds) => bonusCents = (long)Math.Round(Math.Max(0f, seconds) * .08f, MidpointRounding.ToEven) * 100;
        static long Cents(float amount) => float.IsNaN(amount) || float.IsInfinity(amount) ? 0 : (long)Math.Round(Math.Max(0f, amount) * 100.0, MidpointRounding.AwayFromZero);
    }
}
