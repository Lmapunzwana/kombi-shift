using System;
using KombiShift.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KombiShift.Core
{
    // Temporary integration harness, not the final gameplay or menu UI.
    public sealed class SprintZeroBootstrap : MonoBehaviour
    {
        private GameManager manager;
        private EconomyManager economy;
        private string result = "Smoke checks have not run.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (SceneManager.GetActiveScene().name != "Bootstrap") return;
            if (FindFirstObjectByType<SprintZeroBootstrap>() != null) return;
            new GameObject("Sprint 0 Bootstrap").AddComponent<SprintZeroBootstrap>();
        }

        private void Awake()
        {
            manager = GameManager.Instance;
            if (manager == null) manager = gameObject.AddComponent<GameManager>();
            economy = gameObject.AddComponent<EconomyManager>();
            DontDestroyOnLoad(gameObject);
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Bootstrap Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                DontDestroyOnLoad(cameraObject);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, 520, 600), GUI.skin.box);
            GUILayout.Label("Kombi Shift — Sprint 0 integration harness");
            GUILayout.Label("State: " + manager.CurrentState + " | Time scale: " + Time.timeScale);
            GUILayout.Label("Fares: " + economy.GrossFaresCents + " cents");
            GUILayout.Label("Fuel: " + economy.FuelCostsCents + " | Repairs: " + economy.RepairCostsCents);
            GUILayout.Label("Remittance: " + economy.DailyRemittanceCents);
            GUILayout.Label("Net: " + economy.NetAfterRemittanceCents + " cents");
            foreach (GameState state in Enum.GetValues(typeof(GameState)))
                if (GUILayout.Button("Transition to " + state))
                    result = manager.TryTransition(state) ? "Transition accepted." : "Transition rejected.";
            if (GUILayout.Button("Collect fare: 100 cents")) GameEvents.RaiseFareCollected(100);
            if (GUILayout.Button("Buy fuel: 50 cents")) GameEvents.RaiseFuelPurchased(50);
            if (GUILayout.Button("Repair: 25 cents")) GameEvents.RaiseVehicleRepairPaid(25);
            if (GUILayout.Button("Run smoke checks from Menu")) RunSmokeChecks();
            GUILayout.Label(result);
            GUILayout.EndArea();
        }

        private void RunSmokeChecks()
        {
            if (manager.CurrentState != GameState.Menu)
            {
                result = "Return to Menu first; smoke check resets the shift ledger.";
                return;
            }
            try
            {
                economy.ResetShift();
                Require(!manager.TryTransition(GameState.Win), "Menu must reject Win");
                Require(manager.TryTransition(GameState.Playing), "Menu to Playing");
                Require(manager.TryTransition(GameState.Paused) && Time.timeScale == 0f, "Pause stops scaled time");
                Require(manager.TryTransition(GameState.Playing) && Time.timeScale == 1f, "Resume restores time");
                GameEvents.RaiseFareCollected(1000);
                GameEvents.RaiseFuelPurchased(200);
                GameEvents.RaiseVehicleRepairPaid(100);
                GameEvents.RaiseFareCollected(-100);
                Require(economy.GrossFaresCents == 1000, "Positive fares only");
                Require(economy.FuelCostsCents == 200 && economy.RepairCostsCents == 100, "Expense events");
                Require(economy.NetAfterRemittanceCents == 700 - economy.DailyRemittanceCents, "Net accounting");
                Require(manager.TryTransition(GameState.ShiftEnd), "Playing to ShiftEnd");
                Require(manager.TryTransition(GameState.Win), "ShiftEnd to Win");
                Require(manager.TryTransition(GameState.Menu), "Win to Menu");
                economy.ResetShift();
                Require(economy.GrossFaresCents == 0 && economy.FuelCostsCents == 0 && economy.RepairCostsCents == 0, "Ledger reset");
                result = "PASS: transitions, pause/resume, event accounting, invalid fare and reset.";
                Debug.Log(result);
            }
            catch (Exception error) { result = "FAIL: " + error.Message; Debug.LogException(error); }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
