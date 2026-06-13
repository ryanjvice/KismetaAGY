using System;
using System.Collections.Generic;
using System.Linq;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Data.Loaders;
using UnityEngine;

namespace Kismeta.Game.Bootstrap
{
    /// <summary>
    /// Fullscreen IMGUI debug panel for hot-seat play. Bound by <see cref="GameBootstrap"/>
    /// after the session and loop are created. All interaction is via three columns:
    ///   Left  — Board state + all-player summary
    ///   Middle — Active player's cards, crucible, and context-sensitive action buttons
    ///   Right  — Scrollable event log
    /// </summary>
    public sealed class GameDebugUI : MonoBehaviour
    {
        // ─── Bound references ─────────────────────────────────────────────────────

        private GameSession?  _session;
        private GameLoop?     _loop;
        private CardDatabase? _db;

        // ─── Card selection state ─────────────────────────────────────────────────

        private readonly HashSet<string> _selectedCards = new();
        private ActionHint _lastHint = ActionHint.None;

        // Commune zone assignments (only used while ActionHint == Commune)
        private readonly List<string> _communeSpread = new();
        private readonly List<string> _communeHand   = new();
        private bool _communeInit;

        // ─── Log ──────────────────────────────────────────────────────────────────

        private readonly List<string> _log = new(200);
        private Vector2 _logScroll;

        // ─── Scroll positions ─────────────────────────────────────────────────────

        private Vector2 _spreadScroll;
        private Vector2 _handScroll;
        private Vector2 _crucibleScroll;
        private Vector2 _playersScroll;
        private Vector2 _commSpreadScroll;
        private Vector2 _commHandScroll;

        // ─── Error feedback ───────────────────────────────────────────────────────

        private string _lastError = "";
        private float  _errorExpiry;

        // ─── Public API ───────────────────────────────────────────────────────────

        public void Bind(GameSession session, GameLoop loop, CardDatabase db)
        {
            _session = session;
            _loop    = loop;
            _db      = db;

            session.OnEvent += evt => AddLog(FormatEvent(evt));
            loop.OnLog      += msg => AddLog($"[Loop] {msg}");
        }

        // ─── OnGUI entry ──────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_session == null) return;

            const float LEFT_W  = 270f;
            const float RIGHT_W = 270f;
            float h   = Screen.height - 20f;
            float mw  = Screen.width - LEFT_W - RIGHT_W;

            // Left — board + players
            GUILayout.BeginArea(new Rect(0f, 0f, LEFT_W, h), GUI.skin.box);
            DrawLeftColumn();
            GUILayout.EndArea();

            // Middle — active player + actions
            GUILayout.BeginArea(new Rect(LEFT_W, 0f, mw, h), GUI.skin.box);
            DrawMiddleColumn(mw, h);
            GUILayout.EndArea();

            // Right — event log
            GUILayout.BeginArea(new Rect(LEFT_W + mw, 0f, RIGHT_W, h), GUI.skin.box);
            DrawRightColumn(h);
            GUILayout.EndArea();
        }

        // ─── Left column ──────────────────────────────────────────────────────────

        private void DrawLeftColumn()
        {
            GUILayout.Label("── BOARD ──");
            var board = _session!.Board;
            GUILayout.Label($"Round {board.RoundNumber}  |  {_session.Phase.CurrentSeason} — {_session.Phase.CurrentStep.Name}");
            GUILayout.Label($"Cosmic Age: {board.CosmicAgeSign}");
            GUILayout.Label($"Deck: {board.CommonDeckCount}  Disc: {board.CommonDiscardCount}");
            GUILayout.Label($"CardLock: {(_session.CardLockActive ? "YES" : "no")}  Over: {_session.IsOver}");

            GUILayout.Space(6f);
            GUILayout.Label("── PLAYERS ──");

            _playersScroll = GUILayout.BeginScrollView(_playersScroll);
            foreach (var p in _session.Players)
                DrawPlayerRow(p);
            GUILayout.EndScrollView();
        }

        private void DrawPlayerRow(PlayerState p)
        {
            bool isActive = _loop?.ActivePlayerId == p.PlayerId;
            string prefix = isActive ? "▶ " : "  ";
            string ak     = p.IsAgekeeper ? " [AK]" : "";
            GUILayout.Label($"{prefix}P{p.PlayerId} ({p.Color}){ak}");
            GUILayout.Label($"   Sign: {p.CurrentSign}");
            GUILayout.Label($"   Stone: {p.StonePosition} [{p.StoneState}]");
            GUILayout.Label($"   Hand:{p.Hand.Count} Spr:{p.Spread.Count} Arc:{p.Arcanum.Count}");
            GUILayout.Label(
                $"   Sa:{p.GetReagent(ReagentType.Salt)} " +
                $"Su:{p.GetReagent(ReagentType.Sulphur)} " +
                $"AR:{p.GetReagent(ReagentType.AquaRegia)} " +
                $"V:{p.GetReagent(ReagentType.Vitriol)} " +
                $"Qk:{p.GetReagent(ReagentType.Quicksilver)}");

            // Cauldrons
            string cauldr = $"   Cauldrons: " +
                $"♦{(p.IsCauldronLit(Suit.Wands)     ? "[lit]" : "[ ]")} " +
                $"♥{(p.IsCauldronLit(Suit.Cups)       ? "[lit]" : "[ ]")} " +
                $"♣{(p.IsCauldronLit(Suit.Pentacles)  ? "[lit]" : "[ ]")} " +
                $"♠{(p.IsCauldronLit(Suit.Swords)     ? "[lit]" : "[ ]")}";
            GUILayout.Label(cauldr);

            // Crucible slots (compact)
            string slots = "   Crucible:";
            foreach (var s in p.CrucibleSlots)
                slots += $" [{s.State.ToString()[0]}]";
            GUILayout.Label(slots);

            GUILayout.Label($"   Houses: {p.AstralHouses.Count}");
            GUILayout.Space(4f);
        }

        // ─── Middle column ────────────────────────────────────────────────────────

        private void DrawMiddleColumn(float colW, float colH)
        {
            var hs = _loop?.PendingHumanController;

            if (hs == null)
            {
                DrawWaitingPanel();
                return;
            }

            int pid     = hs.Slot.Index;
            var hint    = _loop!.PendingHint;

            // Reset selection when the hint (i.e. action context) changes
            if (hint != _lastHint)
            {
                _selectedCards.Clear();
                _communeInit = false;
                _lastHint    = hint;
            }

            var player = _session!.Players[pid];
            GUILayout.Label($"── YOUR TURN — P{pid} ({player.Color}) ── {hint} ──");

            if (_lastError != "" && Time.time < _errorExpiry)
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label($"⚠ {_lastError}");
                GUI.contentColor = prevColor;
            }

            if (hint == ActionHint.Commune)
            {
                DrawCommunePanel(hs, pid, player, colH);
            }
            else
            {
                // Card lists occupy the top portion; actions below
                float cardH     = colH * 0.42f;
                float crucibleH = colH * 0.18f;

                DrawCardLists(pid, player, cardH);
                GUILayout.Space(4f);
                DrawCrucibleSection(pid, player, crucibleH);
                GUILayout.Space(4f);
                GUILayout.Label($"── ACTIONS ({hint}) ──");
                GUILayout.Label($"Selected: {_selectedCards.Count} card(s)");
                GUILayout.Space(2f);
                DrawActionPanel(hs, pid, player, hint);
            }
        }

        private void DrawWaitingPanel()
        {
            GUILayout.Label("── WAITING ──");
            if (_session == null || _loop == null)
            {
                GUILayout.Label("Session not started.");
                return;
            }

            int active = _loop.ActivePlayerId;
            if (active >= 0 && active < _session.Players.Count)
            {
                var p = _session.Players[active];
                GUILayout.Label($"AI player P{active} ({p.Color}) is thinking…");
            }
            else if (_session.IsOver)
            {
                GUILayout.Label($"Game over — winner: P{_session.WinnerPlayerId}");
            }
            else
            {
                GUILayout.Label("Waiting for game loop…");
            }
        }

        // ── Card lists ───────────────────────────────────────────────────────────

        private void DrawCardLists(int pid, PlayerState player, float totalH)
        {
            float halfH = totalH * 0.5f;

            GUILayout.Label($"── SPREAD ({player.Spread.Count}) ──");
            _spreadScroll = GUILayout.BeginScrollView(_spreadScroll, GUILayout.Height(halfH - 20f));
            foreach (var id in player.Spread)
                DrawCardToggle(id);
            GUILayout.EndScrollView();

            GUILayout.Label($"── HAND ({player.Hand.Count}) ──");
            _handScroll = GUILayout.BeginScrollView(_handScroll, GUILayout.Height(halfH - 20f));
            foreach (var id in player.Hand)
                DrawCardToggle(id);
            GUILayout.EndScrollView();
        }

        private void DrawCardToggle(string instanceId)
        {
            bool selected = _selectedCards.Contains(instanceId);
            string label  = $"{(selected ? "[x]" : "[ ]")} {CardLabel(instanceId)}";
            if (GUILayout.Button(label, GUI.skin.label))
            {
                if (selected)
                    _selectedCards.Remove(instanceId);
                else
                    _selectedCards.Add(instanceId);
            }
        }

        // ── Crucible section ─────────────────────────────────────────────────────

        private void DrawCrucibleSection(int pid, PlayerState player, float scrollH)
        {
            GUILayout.Label("── YOUR CRUCIBLE CARDS ──");
            _crucibleScroll = GUILayout.BeginScrollView(_crucibleScroll, GUILayout.Height(scrollH));
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                var inst = _session!.GetCard(slot.CardInstanceId);
                var def  = inst != null ? _db?.GetById(inst.DefinitionId) : null;
                string name     = def != null ? $"[{def.CrucibleGroup}] {def.ActivationFormula}" : "?";
                string coalStr  = slot.HasCoal ? " ·Coal" : "";
                string wardStr  = slot.WardCount > 0 ? $" Ward×{slot.WardCount}" : "";
                GUILayout.Label($"Slot {i}: {slot.State,-10} {name}{coalStr}{wardStr}");
                if (def != null && def.AlchemicalFormula != "")
                    GUILayout.Label($"   Formula: {def.AlchemicalFormula}");
            }
            GUILayout.EndScrollView();
        }

        // ── Commune panel ────────────────────────────────────────────────────────

        private void DrawCommunePanel(HotSeatController hs, int pid, PlayerState player, float colH)
        {
            // Lazy-initialise: all owned cards start in Spread column
            if (!_communeInit)
            {
                _communeSpread.Clear();
                _communeHand.Clear();
                _communeSpread.AddRange(player.Spread);
                _communeSpread.AddRange(player.Hand);
                _communeInit = true;
            }

            GUILayout.Label($"── COMMUNE — Assign {_communeSpread.Count + _communeHand.Count} card(s) ──");
            GUILayout.Label($"Hand limit: 5  (currently → Hand: {_communeHand.Count})");
            GUILayout.Space(4f);

            float halfH = (colH - 160f) * 0.5f;

            // Spread column
            GUILayout.Label($"→ SPREAD ({_communeSpread.Count})  [click to move to Hand]");
            _commSpreadScroll = GUILayout.BeginScrollView(_commSpreadScroll, GUILayout.Height(halfH));
            for (int i = _communeSpread.Count - 1; i >= 0; i--)
            {
                string id = _communeSpread[i];
                if (GUILayout.Button($"[ ] {CardLabel(id)} →Hand"))
                {
                    _communeSpread.RemoveAt(i);
                    _communeHand.Add(id);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);

            // Hand column
            GUILayout.Label($"→ HAND ({_communeHand.Count})  [click to move to Spread]");
            _commHandScroll = GUILayout.BeginScrollView(_commHandScroll, GUILayout.Height(halfH));
            for (int i = _communeHand.Count - 1; i >= 0; i--)
            {
                string id = _communeHand[i];
                if (GUILayout.Button($"[H] {CardLabel(id)} →Spread"))
                {
                    _communeHand.RemoveAt(i);
                    _communeSpread.Add(id);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6f);

            // Quick-assign helpers
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All to Spread"))
            {
                _communeSpread.AddRange(_communeHand);
                _communeHand.Clear();
            }
            if (GUILayout.Button("All to Hand"))
            {
                _communeHand.AddRange(_communeSpread);
                _communeSpread.Clear();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            bool tooManyHand = _communeHand.Count > 5;
            if (tooManyHand)
            {
                var prev = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label($"⚠ Hand limit is 5 — move {_communeHand.Count - 5} card(s) to Spread.");
                GUI.contentColor = prev;
            }

            GUI.enabled = !tooManyHand;
            if (GUILayout.Button("✔ Confirm Commune", GUILayout.Height(30f)))
            {
                var spreadIds = new List<string>(_communeSpread);
                var handIds   = new List<string>(_communeHand);
                _communeInit  = false;
                SubmitAction(hs, new CommuneCommand(pid, spreadIds, handIds));
            }
            GUI.enabled = true;
        }

        // ── Action panel (Summer / Autumn / None) ────────────────────────────────

        private void DrawActionPanel(HotSeatController hs, int pid, PlayerState player, ActionHint hint)
        {
            if (hint == ActionHint.SummerAction)
                DrawSummerActions(hs, pid, player);
            else if (hint == ActionHint.AutumnAction)
                DrawAutumnActions(hs, pid, player);
            else
            {
                GUILayout.Label("Waiting…");
                if (GUILayout.Button("Pass"))
                    SubmitAction(hs, new PassActionCommand(pid));
            }
        }

        private void DrawSummerActions(HotSeatController hs, int pid, PlayerState player)
        {
            int selCount = _selectedCards.Count;
            var selList  = _selectedCards.ToList();

            // Activate Crucible (one button per Dormant slot)
            GUILayout.Label("Activate Crucible (need 3+ selected):");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                GUI.enabled = selCount >= 3 && slot.State == CrucibleCardState.Dormant;
                int captured = i;
                if (GUILayout.Button($"Slot {captured}"))
                    SubmitAction(hs, new ActivateCrucibleCommand(pid, captured, selList));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Craft Salt (need 3+ selected)
            GUI.enabled = selCount >= 3;
            if (GUILayout.Button($"Craft Salt  ({selCount}/3 sel)"))
                SubmitAction(hs, new CraftReagentCommand(pid, ReagentType.Salt, selList));
            GUI.enabled = true;

            // Craft Elemental (need 3+ same-suit selected AND cauldron lit)
            Suit? uSuit = GetUniformSuit(selList);
            bool cauldronLit = uSuit.HasValue && player.IsCauldronLit(uSuit.Value);
            GUI.enabled = selCount >= 3 && uSuit.HasValue && cauldronLit;
            string craftLabel = uSuit.HasValue
                ? $"Craft {uSuit.Value} Reagent  (cauldron {(cauldronLit ? "lit" : "UNLIT")})"
                : "Craft Elemental  (select 3+ same-suit)";
            if (GUILayout.Button(craftLabel))
            {
                var rtype = SuitToReagent(uSuit!.Value);
                SubmitAction(hs, new CraftReagentCommand(pid, rtype, selList));
            }
            GUI.enabled = true;

            GUILayout.Space(4f);
            if (GUILayout.Button("Pass"))
                SubmitAction(hs, new PassCrucibleActionCommand(pid));
        }

        private void DrawAutumnActions(HotSeatController hs, int pid, PlayerState player)
        {
            int selCount = _selectedCards.Count;
            var selList  = _selectedCards.ToList();

            // Fire Stone — one button per Active slot
            GUILayout.Label("Fire Stone (select Active slot):");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                GUI.enabled = slot.State == CrucibleCardState.Active;
                int captured = i;
                if (GUILayout.Button($"Fire Slot {captured}"))
                    SubmitAction(hs, new FireStoneCommand(pid, captured));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // Temper — enabled when stone is Forging
            GUI.enabled = player.StoneState == StoneState.Forging;
            if (GUILayout.Button($"Temper Stone  [{player.StoneState}]"))
                SubmitAction(hs, new TemperCommand(pid));
            GUI.enabled = true;

            // Leave Stasis — enabled when stone is in Stasis
            GUI.enabled = player.StoneState == StoneState.Stasis;
            if (GUILayout.Button($"Leave Stasis  (costs 2 Salt, have {player.GetReagent(ReagentType.Salt)})"))
                SubmitAction(hs, new LeaveStasisCommand(pid));
            GUI.enabled = true;

            GUILayout.Space(2f);

            // Oppose — one button per opponent who is Forging
            GUILayout.Label("Oppose (select Forging opponent):");
            GUILayout.BeginHorizontal();
            foreach (var opp in _session!.Players)
            {
                if (opp.PlayerId == pid) continue;
                GUI.enabled = opp.StoneState == StoneState.Forging;
                if (GUILayout.Button($"Oppose P{opp.PlayerId}"))
                    SubmitAction(hs, new InitiateOppositionCommand(pid, opp.PlayerId));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // Craft Salt
            GUI.enabled = selCount >= 3;
            if (GUILayout.Button($"Craft Salt  ({selCount}/3 sel)"))
                SubmitAction(hs, new CraftReagentCommand(pid, ReagentType.Salt, selList));
            GUI.enabled = true;

            // Craft Elemental
            Suit? uSuit = GetUniformSuit(selList);
            bool cauldronLit = uSuit.HasValue && player.IsCauldronLit(uSuit.Value);
            GUI.enabled = selCount >= 3 && uSuit.HasValue && cauldronLit;
            string craftLabel = uSuit.HasValue
                ? $"Craft {uSuit.Value} Reagent  (cauldron {(cauldronLit ? "lit" : "UNLIT")})"
                : "Craft Elemental  (select 3+ same-suit)";
            if (GUILayout.Button(craftLabel))
            {
                var rtype = SuitToReagent(uSuit!.Value);
                SubmitAction(hs, new CraftReagentCommand(pid, rtype, selList));
            }
            GUI.enabled = true;

            GUILayout.Space(4f);
            if (GUILayout.Button("Pass"))
                SubmitAction(hs, new PassCrucibleActionCommand(pid));
        }

        // ─── Right column (event log) ─────────────────────────────────────────────

        private void DrawRightColumn(float colH)
        {
            GUILayout.Label("── EVENT LOG ──");
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(colH - 30f));
            for (int i = _log.Count - 1; i >= 0; i--)
                GUILayout.Label(_log[i], GUILayout.ExpandWidth(true));
            GUILayout.EndScrollView();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void SubmitAction(HotSeatController hs, IGameCommand cmd)
        {
            _selectedCards.Clear();
            _communeInit = false;
            hs.SubmitCommand(cmd);
        }

        private void ShowError(string msg)
        {
            _lastError  = msg;
            _errorExpiry = Time.time + 4f;
        }

        private void AddLog(string msg)
        {
            _log.Add(msg);
            if (_log.Count > 200) _log.RemoveAt(0);
        }

        // ── Card label resolution ────────────────────────────────────────────────

        private string CardLabel(string instanceId)
        {
            var inst = _session?.GetCard(instanceId);
            if (inst == null) return $"[?:{instanceId[..Math.Min(8, instanceId.Length)]}]";
            var def  = _db?.GetById(inst.DefinitionId);
            if (def == null) return $"[?:{inst.DefinitionId}]";

            if (def.IsCrucible)
                return $"[{def.CrucibleGroup}] {def.ActivationFormula}";

            if (def.IsMajorArcana)
                return $"★{def.ArcanaNumber} {def.EffectType}";

            // Minor arcana
            return $"{SuitGlyph(def.Suit)} {def.Rank}";
        }

        private static char SuitGlyph(Suit suit) => suit switch
        {
            Suit.Wands     => '♦',
            Suit.Cups      => '♥',
            Suit.Pentacles => '♣',
            Suit.Swords    => '♠',
            _              => '·'
        };

        // ── Crafting helpers ─────────────────────────────────────────────────────

        private Suit? GetUniformSuit(IReadOnlyList<string> ids)
        {
            if (ids.Count == 0) return null;
            Suit? suit = null;
            foreach (var id in ids)
            {
                var inst = _session?.GetCard(id);
                if (inst == null) return null;
                var def = _db?.GetById(inst.DefinitionId);
                if (def == null || def.Suit == Suit.None) return null;
                if (suit == null) suit = def.Suit;
                else if (suit != def.Suit) return null;
            }
            return suit;
        }

        private static ReagentType SuitToReagent(Suit suit) => suit switch
        {
            Suit.Wands     => ReagentType.Sulphur,
            Suit.Cups      => ReagentType.AquaRegia,
            Suit.Pentacles => ReagentType.Vitriol,
            Suit.Swords    => ReagentType.Quicksilver,
            _              => ReagentType.Salt
        };

        // ── Event formatting ─────────────────────────────────────────────────────

        private string FormatEvent(IGameEvent evt) => evt switch
        {
            CosmicAgeSetEvent e      => $"[CosmicAge] Sign={e.Sign} Planet={e.Planet}",
            ZodiacRolledEvent e      => $"[Zodiac] P{e.PlayerId} → {e.Sign}",
            CardsDrawnEvent e        => $"[Harvest] P{e.PlayerId} drew {e.Count}",
            StoneFiredEvent e        => $"[Fire] P{e.PlayerId} → pos {e.NewPosition}",
            StoneTemperedEvent e     => $"[Temper] P{e.PlayerId} → pos {e.NewPosition}",
            OppositionResolvedEvent e=> $"[Oppose] Att:{e.AttackerId} Def:{e.DefenderId} Loser:{e.LoserId} ({e.AttackRoll}v{e.DefendRoll})",
            GameSetupCompleteEvent e => $"[Setup] {e.PlayerCount}p ready",
            AgeTransitedEvent e      => $"[Transit] Round {e.NewRoundNumber} AK→P{e.NewAgekeeperId}",
            GameEndedEvent e         => $"[GAME OVER] Winner: P{e.WinnerPlayerId}",
            PhaseChangedEvent e      => $"[Phase] {e.Season} step {e.StepIndex}",
            _                        => $"[{evt.GetType().Name}]"
        };
    }
}
