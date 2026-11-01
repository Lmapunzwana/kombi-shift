using System;
using UnityEngine;

namespace KombiShift.Core
{
    // Attach once to a persistent bootstrap object. Other systems subscribe to GameEvents.
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [SerializeField] private GameState initialState = GameState.Menu;
        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = initialState;
            Time.timeScale = 1f;
        }

        public bool TryTransition(GameState next)
        {
            if (!IsAllowed(CurrentState, next)) return false;
            var previous = CurrentState;
            CurrentState = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            GameEvents.RaiseStateChanged(previous, next);
            return true;
        }

        private static bool IsAllowed(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.Menu: return to == GameState.Playing;
                case GameState.Playing: return to == GameState.Paused || to == GameState.ShiftEnd || to == GameState.GameOver;
                case GameState.Paused: return to == GameState.Playing || to == GameState.Menu;
                case GameState.ShiftEnd: return to == GameState.Win || to == GameState.GameOver || to == GameState.Menu;
                case GameState.Win: case GameState.GameOver: return to == GameState.Menu;
                default: return false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; Time.timeScale = 1f; }
        }
    }
}
