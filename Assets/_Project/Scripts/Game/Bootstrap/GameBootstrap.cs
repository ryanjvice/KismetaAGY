using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Data.Loaders;
using UnityEngine;

namespace Kismeta.Game.Bootstrap
{
    /// <summary>
    /// Entry-point MonoBehaviour. On Start it loads the CardDatabase, creates a 2-player
    /// GameSession in Quickplay mode, and logs the initial state to the console.
    /// No UI is created here — this scene only verifies the data and session wiring.
    /// Attach to a GameObject in the Bootstrap scene.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Number of human players (1–4). Remaining slots fill with AI.")]
        [Range(1, 4)]
        [SerializeField] private int _humanPlayers = 1;

        [Tooltip("Total number of players (2–4).")]
        [Range(2, 4)]
        [SerializeField] private int _totalPlayers = 2;

        [SerializeField] private GameMode _gameMode = GameMode.Quickplay;

        private CardDatabase? _db;
        private GameSession?  _session;

        private void Start()
        {
            LoadDatabase();
            CreateSession();
            LogInitialState();
        }

        private void LoadDatabase()
        {
            _db = CardDatabase.Load();
        }

        private void CreateSession()
        {
            int count   = Mathf.Clamp(_totalPlayers, 2, 4);
            int humans  = Mathf.Clamp(_humanPlayers, 0, count);
            var colors  = new[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Blue, PlayerColor.White };

            var players     = new List<PlayerState>(count);
            var controllers = new List<IPlayerController>(count);

            for (int i = 0; i < count; i++)
            {
                var color = colors[i];
                var ps    = new PlayerState(i, color);
                if (i == 0) ps.IsAgekeeper = true;
                players.Add(ps);

                var slot = new PlayerSlot(i, color,
                    i < humans ? PlayerControllerType.LocalHuman : PlayerControllerType.LocalAI);

                IPlayerController ctrl = i < humans
                    ? new HotSeatController(slot)
                    : new SimpleAIController(slot);
                controllers.Add(ctrl);
            }

            _session = new GameSession(
                sessionId: System.Guid.NewGuid().ToString(),
                mode:      _gameMode,
                players:   players);

            _session.OnEvent += e => Debug.Log($"[GameEvent] {e.GetType().Name}");

            _ = new PlayerDirector(_session, controllers);
        }

        private void LogInitialState()
        {
            if (_db == null || _session == null) return;

            Debug.Log($"[Kismeta] CardDatabase loaded: {_db.Count} cards (expected {CardDatabase.ExpectedCardCount})");
            Debug.Log($"[Kismeta] Session '{_session.SessionId}' | Mode: {_session.Mode} | Players: {_session.Players.Count}");
            Debug.Log($"[Kismeta] Phase: {_session.Phase.CurrentSeason} — Step {_session.Phase.CurrentStepIndex + 1}: {_session.Phase.CurrentStep.Name}");
            Debug.Log($"[Kismeta] Bootstrap complete. Ready for first command.");
        }
    }
}
