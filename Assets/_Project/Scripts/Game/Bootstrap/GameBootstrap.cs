using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Core.Domain;
using Kismeta.Core.Commands;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using UnityEngine;

namespace Kismeta.Game.Bootstrap
{
    /// <summary>
    /// Entry-point MonoBehaviour. Wires up the full game stack and starts the GameLoop:
    ///   1. Loads the CardDatabase.
    ///   2. Builds a GameRuleSet with all M2 rule services.
    ///   3. Creates players + controllers (human or AI per inspector settings).
    ///   4. Starts the GameLoop as a background Task.
    ///   5. Delegates all IMGUI rendering to <see cref="GameDebugUI"/>.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Number of human (hot-seat) players. Remaining slots fill with AI.")]
        [Range(0, 4)]
        [SerializeField] private int _humanPlayers = 1;

        [Tooltip("Total number of players (2–4).")]
        [Range(2, 4)]
        [SerializeField] private int _totalPlayers = 2;

        [SerializeField] private GameMode _gameMode = GameMode.Quickplay;

        // ─── Runtime state ────────────────────────────────────────────────────────

        private CardDatabase?            _db;
        private CrucibleCodexDatabase?   _codexDb;
        private GameSession?             _session;
        private GameLoop?                _loop;
        private List<IPlayerController>? _controllers;
        private GameDebugUI?             _ui;
        private CancellationTokenSource  _cts = new();

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            try
            {
                _db      = CardDatabase.Load();
                _codexDb = CrucibleCodexDatabase.Load();
                _ui      = gameObject.AddComponent<GameDebugUI>();
                BuildSession();
                _ = StartLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        // ─── Session + loop wiring ────────────────────────────────────────────────

        private void BuildSession()
        {
            int count  = Mathf.Clamp(_totalPlayers, 2, 4);
            int humans = Mathf.Clamp(_humanPlayers, 0, count);

            var colors = new[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Blue, PlayerColor.White };

            var players  = new List<PlayerState>(count);
            _controllers = new List<IPlayerController>(count);

            for (int i = 0; i < count; i++)
            {
                var ps   = new PlayerState(i, colors[i]);
                players.Add(ps);

                var slot = new PlayerSlot(i, colors[i],
                    i < humans ? PlayerControllerType.LocalHuman : PlayerControllerType.LocalAI);

                IPlayerController ctrl = i < humans
                    ? new HotSeatController(slot)
                    : new SimpleAIController(slot);

                _controllers.Add(ctrl);
            }

            var cosmicSvc           = new CosmicEffectService();
            var fateResolver        = new FateCardResolver(_db!);
            var alchemicalValidator = new AlchemicalAlignmentValidator();
            var alignmentService    = new AlignmentService(_db!);
            var rules = new GameRuleSet(
                cardDatabase:        _db!,
                codexDatabase:       _codexDb!,
                setup:               new GameSetupService(_db!),
                harvest:             new SpringRules(_db!, cosmicEffect: cosmicSvc, fateResolver: fateResolver),
                crucible:            new CrucibleRules(_db!, _codexDb!, alchemicalValidator, alignmentService),
                crafting:            new CraftingRules(_db!),
                winter:              new WinterRules(_db!),
                validator:           new ActionValidator(),
                astralHouse:         new AstralHouseService(_db!),
                cosmicEffect:        cosmicSvc,
                fateResolver:        fateResolver,
                alchemicalValidator: alchemicalValidator,
                alignment:           alignmentService,
                combat:              new CombatRules());

            _session = new GameSession(
                sessionId: Guid.NewGuid().ToString(),
                mode:      _gameMode,
                players:   players,
                rules:     rules);

            _session.OnEvent += evt => Debug.Log($"[Event] {evt.GetType().Name}");

            _loop = new GameLoop(_session, _controllers);
            _loop.OnLog += msg => Debug.Log($"[GameLoop] {msg}");

            _ui!.Bind(_session, _loop, _db!, _codexDb);
        }

        private async Task StartLoopAsync(CancellationToken ct)
        {
            if (_loop == null) return;
            try
            {
                await _loop.RunAsync(ct);
            }
            catch (OperationCanceledException) { /* normal shutdown */ }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
