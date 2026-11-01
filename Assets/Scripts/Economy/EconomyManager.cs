using UnityEngine;
using KombiShift.Core;

namespace KombiShift.Economy
{
    // Sprint 0 contract; actual tuning and remittance calculation are Sprint 1 work.
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int dailyRemittanceCents = 3000; // illustrative only; rebalance
        public int GrossFaresCents { get; private set; }
        public int FuelCostsCents { get; private set; }
        public int RepairCostsCents { get; private set; }
        public int DailyRemittanceCents => dailyRemittanceCents;
        public int NetAfterRemittanceCents => GrossFaresCents - FuelCostsCents - RepairCostsCents - DailyRemittanceCents;

        private void OnEnable()
        {
            GameEvents.FareCollected += OnFare;
            GameEvents.FuelPurchased += OnFuel;
            GameEvents.VehicleRepairPaid += OnRepair;
        }
        private void OnDisable()
        {
            GameEvents.FareCollected -= OnFare;
            GameEvents.FuelPurchased -= OnFuel;
            GameEvents.VehicleRepairPaid -= OnRepair;
        }
        private void OnFare(int cents) => GrossFaresCents += cents;
        private void OnFuel(int cents) => FuelCostsCents += cents;
        private void OnRepair(int cents) => RepairCostsCents += cents;
        public void ResetShift() { GrossFaresCents = 0; FuelCostsCents = 0; RepairCostsCents = 0; }
    }
}
