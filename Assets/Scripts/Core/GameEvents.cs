using System;
using UnityEngine;

namespace KombiShift.Core
{
    // Pure C# events: no scene references or dependency on other feature modules.
    public static class GameEvents
    {
        public static event Action<GameState, GameState> StateChanged;
        public static event Action<int> PassengerDelivered;
        public static event Action<int> FareCollected; // integer cents, no floating-point currency
        public static event Action<int> FuelPurchased; // integer cents
        public static event Action<int> VehicleRepairPaid; // integer cents

        // Clear static subscribers when entering Play Mode with domain reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            StateChanged = null;
            PassengerDelivered = null;
            FareCollected = null;
            FuelPurchased = null;
            VehicleRepairPaid = null;
        }

        public static void RaiseStateChanged(GameState previous, GameState current) => StateChanged?.Invoke(previous, current);
        public static void RaisePassengerDelivered(int count) { if (count > 0) PassengerDelivered?.Invoke(count); }
        public static void RaiseFareCollected(int cents) { if (cents > 0) FareCollected?.Invoke(cents); }
        public static void RaiseFuelPurchased(int cents) { if (cents > 0) FuelPurchased?.Invoke(cents); }
        public static void RaiseVehicleRepairPaid(int cents) { if (cents > 0) VehicleRepairPaid?.Invoke(cents); }
    }
}
