using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
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
    ///   5. Draws an IMGUI debug overlay for hot-seat testing.
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

        private CardDatabase?          _db;
        private GameSession?           _session;
        private GameLoop?              _loop;
        private List<IPlayerController>? _controllers;
        private CancellationTokenSource  _cts = new();

        // IMGUI scroll state
        private Vector2 _logScroll;
        private readonly List<string> _log = new(64);
        private bool _guiVisible = true;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            try
            {
                _db = CardDatabase.Load();
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

            var players    = new List<PlayerState>(count);
            _controllers   = new List<IPlayerController>(count);

            var hotSeats   = new List<HotSeatController>();

            for (int i = 0; i < count; i++)
            {
                var ps   = new PlayerState(i, colors[i]);
                players.Add(ps);

                var slot = new PlayerSlot(i, colors[i],
                    i < humans ? PlayerControllerType.LocalHuman : PlayerControllerType.LocalAI);

                IPlayerController ctrl;
                if (i < humans)
                {
                    var hs = new HotSeatController(slot);
                    hotSeats.Add(hs);
                    ctrl = hs;
                }
                else
                {
                    ctrl = new SimpleAIController(slot);
                }
                _controllers.Add(ctrl);
            }

            // Build rule set (all M2 services)
            var rules = new GameRuleSet(
                cardDatabase: _db!,
                setup:        new GameSetupService(_db!),
                harvest:      new SpringRules(),
                crucible:     new CrucibleRules(_db!),
                crafting:     new CraftingRules(_db!),
                winter:       new WinterRules(),
                validator:    new ActionValidator());

            _session = new GameSession(
                sessionId: Guid.NewGuid().ToString(),
                mode:      _gameMode,
                players:   players,
                rules:     rules);

            _session.OnEvent += evt =>
            {
                var msg = $"[{evt.GetType().Name}] {FormatEvent(evt)}";
                Debug.Log(msg);
                _log.Add(msg);
                if (_log.Count > 200) _log.RemoveAt(0);
            };

            _loop = new GameLoop(_session, _controllers);
            _loop.OnLog += msg =>
            {
                Debug.Log($"[GameLoop] {msg}");
                _log.Add($"[Loop] {msg}");
                if (_log.Count > 200) _log.RemoveAt(0);
            };
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

        // ─── IMGUI debug overlay ──────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_session == null) return;

            const float W = 360f;
            const float H = 500f;
            float x = Screen.width - W - 8f;

            GUILayout.BeginArea(new Rect(x, 8f, W, H), GUI.skin.box);

            GUILayout.Label("── Kismeta Debug Panel ──");
            if (GUILayout.Button(_guiVisible ? "Collapse" : "Expand", GUILayout.Height(18)))
                _guiVisible = !_guiVisible;

            if (_guiVisible)
            {
                DrawGameState();
                GUILayout.Space(4);
                DrawPlayers();
                GUILayout.Space(4);
                DrawHotSeatActions();
                GUILayout.Space(4);
                DrawLog();
            }

            GUILayout.EndArea();
        }

        private void DrawGameState()
        {
            if (_session == null) return;
            GUILayout.Label($"Round {_session.Board.RoundNumber}  |  {_session.Phase.CurrentSeason} — {_session.Phase.CurrentStep.Name}");
            GUILayout.Label($"Cosmic Age: {_session.Board.CosmicAgeSign}  |  Deck: {_session.Board.CommonDeckCount}");
            GUILayout.Label($"CardLock: {_session.CardLockActive}  |  Over: {_session.IsOver}");
        }

        private void DrawPlayers()
        {
            if (_session == null) return;
            foreach (var p in _session.Players)
            {
                var isActive = _loop?.ActivePlayerId == p.PlayerId;
                var prefix   = isActive ? "▶ " : "  ";
                var ageStr   = p.IsAgekeeper ? " [AK]" : "";
                GUILayout.Label($"{prefix}P{p.PlayerId} ({p.Color}){ageStr}  Sign:{p.CurrentSign}");
                GUILayout.Label($"    Stone: {p.StonePosition} [{p.StoneState}]");
                GUILayout.Label($"    Hand:{p.Hand.Count} Spread:{p.Spread.Count}  " +
                    $"Sa:{p.GetReagent(ReagentType.Salt)} Su:{p.GetReagent(ReagentType.Sulphur)} " +
                    $"AR:{p.GetReagent(ReagentType.AquaRegia)} V:{p.GetReagent(ReagentType.Vitriol)} " +
                    $"Qk:{p.GetReagent(ReagentType.Quicksilver)}");
                var slotDesc = "";
                foreach (var s in p.CrucibleSlots)
                    slotDesc += $"[{s.State.ToString()[0]}]";
                GUILayout.Label($"    Crucible: {slotDesc}");
            }
        }

        private void DrawHotSeatActions()
        {
            var hs = _loop?.PendingHumanController;
            if (hs == null) return;

            int pid    = hs.Slot.Index;
            var hint   = GetCurrentHint();
            GUILayout.Label($"── Human P{pid} action ({hint}) ──");

            if (hint == ActionHint.Commune)
            {
                if (GUILayout.Button("Commune: All to Spread"))
                {
                    var player   = _session!.Players[pid];
                    var allCards = new List<string>(player.Spread.Count + player.Hand.Count);
                    allCards.AddRange(player.Spread);
                    allCards.AddRange(player.Hand);
                    hs.SubmitCommand(CommuneCommand.AllToSpread(pid, allCards));
                }
            }
            else if (hint == ActionHint.SummerAction)
            {
                if (GUILayout.Button("Craft Salt (3 Spread cards)"))
                    TrySubmitCraftSalt(hs, pid);
                if (GUILayout.Button("Pass"))
                    hs.SubmitCommand(new PassCrucibleActionCommand(pid));
            }
            else if (hint == ActionHint.AutumnAction)
            {
                if (GUILayout.Button("Temper"))
                    hs.SubmitCommand(new TemperCommand(pid));
                if (GUILayout.Button("Fire Stone (slot 0)"))
                    hs.SubmitCommand(new FireStoneCommand(pid, 0));
                if (GUILayout.Button("Pass"))
                    hs.SubmitCommand(new PassCrucibleActionCommand(pid));
            }
            else
            {
                if (GUILayout.Button("Pass"))
                    hs.SubmitCommand(new PassActionCommand(pid));
            }
        }

        private void DrawLog()
        {
            GUILayout.Label("── Log ──");
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(100));
            for (int i = _log.Count - 1; i >= Mathf.Max(0, _log.Count - 20); i--)
                GUILayout.Label(_log[i], GUILayout.ExpandWidth(true));
            GUILayout.EndScrollView();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private ActionHint GetCurrentHint()
        {
            if (_session == null || _loop == null) return ActionHint.None;
            int pid = _loop.ActivePlayerId;
            if (pid < 0) return ActionHint.None;

            // Look at what the loop last requested based on current season
            return _session.Phase.CurrentSeason switch
            {
                Season.Spring => ActionHint.Commune,
                Season.Summer => ActionHint.SummerAction,
                Season.Autumn => ActionHint.AutumnAction,
                _             => ActionHint.None
            };
        }

        private void TrySubmitCraftSalt(HotSeatController hs, int pid)
        {
            var player = _session!.Players[pid];
            if (player.Spread.Count >= 3)
            {
                var cards = player.Spread.GetRange(0, 3);
                hs.SubmitCommand(new CraftReagentCommand(pid, ReagentType.Salt, cards));
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] Not enough Spread cards to craft Salt.");
            }
        }

        private static string FormatEvent(IGameEvent evt) => evt switch
        {
            CosmicAgeSetEvent e     => $"Sign={e.Sign} Planet={e.Planet}",
            ZodiacRolledEvent e     => $"P{e.PlayerId} → {e.Sign}",
            CardsDrawnEvent e       => $"P{e.PlayerId} drew {e.Count}",
            StoneFiredEvent e       => $"P{e.PlayerId} fired at {e.NewPosition}",
            StoneTemperedEvent e    => $"P{e.PlayerId} tempered → {e.NewPosition}",
            GameSetupCompleteEvent e=> $"{e.PlayerCount}p setup done",
            AgeTransitedEvent e     => $"Round {e.NewRoundNumber} | AK→P{e.NewAgekeeperId}",
            GameEndedEvent e        => $"WINNER: P{e.WinnerPlayerId}",
            _                       => ""
        };
    }
}
