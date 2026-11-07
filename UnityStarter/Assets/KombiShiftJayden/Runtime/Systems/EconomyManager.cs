namespace KombiShift.JaydenStarter {
 // This ledger owns earned income and recorded expenses; station purchases also debit cash.
 public sealed class EconomyManager {
  public float Gross,FuelExpense,RepairExpense;
  public float TakeHome(float remittance,float conductor)=>Gross-FuelExpense-RepairExpense-remittance-conductor;
 }
}
