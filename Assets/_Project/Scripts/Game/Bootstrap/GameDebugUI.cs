using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Fullscreen IMGUI debug panel for hot-seat play. Bound by <see cref="GameBootstrap"/>
    /// after the session and loop are created. All interaction is via three columns:
    ///   Left  — Board state + all-player summary
    ///   Middle — Active player's cards, crucible, and context-sensitive action buttons
    ///   Right  — Scrollable event log
    /// </summary>
    public sealed class GameDebugUI : MonoBehaviour
    {
        // ─── Bound references ─────────────────────────────────────────────────────

        private GameSession?             _session;
        private GameLoop?                _loop;
        private CardDatabase?            _db;
        private ICrucibleCodexDatabase?  _codexDb;

        // ─── Card selection state ─────────────────────────────────────────────────

        private readonly HashSet<string> _selectedCards = new();
        private ActionHint _lastHint = ActionHint.None;

        // Commune zone assignments (only used while ActionHint == Commune)
        private readonly List<string> _communeSpread = new();
        private readonly List<string> _communeHand   = new();
        private bool _communeInit;

        // Summer Trade state
        private int _tradeTargetId = -1;
        private readonly HashSet<string> _tradeOfferIds   = new();
        private readonly HashSet<string> _tradeRequestIds = new();

        // Winter Fateful Wager state
        private ZodiacSign _wagerSign = ZodiacSign.None;
        private readonly HashSet<string> _wagerCards = new();

        // Winter discard-to-limit selection
        private readonly HashSet<string> _discardSpread = new();
        private readonly HashSet<string> _discardHand   = new();

        // ─── Log ──────────────────────────────────────────────────────────────────

        private readonly List<string> _log = new(200);
        private Vector2 _logScroll;

        // ─── Scroll positions ─────────────────────────────────────────────────────

        private Vector2 _spreadScroll;
        private Vector2 _handScroll;
        private Vector2 _arcanumScroll;
        private Vector2 _crucibleScroll;
        private Vector2 _actionsScroll;
        private Vector2 _playersScroll;
        private Vector2 _commSpreadScroll;
        private Vector2 _commHandScroll;

        // ─── Error feedback ───────────────────────────────────────────────────────

        private string _lastError = "";
        private float  _errorExpiry;

        // ─── Public API ───────────────────────────────────────────────────────────

        public void Bind(GameSession session, GameLoop loop, CardDatabase db,
            ICrucibleCodexDatabase? codexDb = null)
        {
            _session = session;
            _loop    = loop;
            _db      = db;
            _codexDb = codexDb;

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
            GUILayout.Label($"   Stone: {p.StonePosition}  [{p.StoneState}]" +
                (p.StoneWardCount > 0 ? $"  🛡️×{p.StoneWardCount}" : "") +
                (p.ReturnedFromStasisThisRound ? "  (stasis return)" : ""));
            GUILayout.Label($"   Hand:{p.Hand.Count} Spr:{p.Spread.Count} Arc:{p.Arcanum.Count}");
            GUILayout.Label(
                $"   Sa:{p.GetReagent(ReagentType.Salt)} " +
                $"Su:{p.GetReagent(ReagentType.Sulphur)} " +
                $"AR:{p.GetReagent(ReagentType.AquaRegia)} " +
                $"V:{p.GetReagent(ReagentType.Vitriol)} " +
                $"Qk:{p.GetReagent(ReagentType.Quicksilver)}");

            // Cauldrons
            string cauldr = $"   Cauldrons: " +
                $"🪄{(p.IsCauldronLit(Suit.Wands)     ? "[lit]" : "[ ]")} " +
                $"🍷{(p.IsCauldronLit(Suit.Cups)       ? "[lit]" : "[ ]")} " +
                $"🪙{(p.IsCauldronLit(Suit.Pentacles)  ? "[lit]" : "[ ]")} " +
                $"🗡️{(p.IsCauldronLit(Suit.Swords)     ? "[lit]" : "[ ]")}";
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
            if (_session!.IsOver)
            {
                DrawGameOverPanel();
                return;
            }

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
                _wagerSign   = ZodiacSign.None;
                _wagerCards.Clear();
                _discardSpread.Clear();
                _discardHand.Clear();
                _tradeTargetId = -1;
                _tradeOfferIds.Clear();
                _tradeRequestIds.Clear();
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
            else if (hint == ActionHint.AdeptDecision)
            {
                DrawAdeptDecisionPanel(hs, pid, player);
            }
            else if (hint == ActionHint.FateMoonDecision)
            {
                DrawFateMoonPanel(hs, pid, player, colH);
            }
            else if (hint == ActionHint.FateReagentChoice)
            {
                DrawFateReagentChoicePanel(hs, pid);
            }
            else if (hint == ActionHint.FateLoversChoice)
            {
                DrawFateLoversChoicePanel(hs, pid);
            }
            else if (hint == ActionHint.FateLoversTargetPick)
            {
                DrawFateLoversTargetPickPanel(hs, pid);
            }
            else
            {
                // Fixed-height card bar (3 equal columns): Spread | Hand | Arcanum
                float headerH  = 24f;
                const float CARD_BAR_H = 200f;
                DrawCardLists(pid, player, CARD_BAR_H, colW);

                // Remaining height — crucible + actions, gains space vs. the old 44% split
                float bottomH = colH - headerH - CARD_BAR_H - 6f;
                GUILayout.Space(4f);
                _actionsScroll = GUILayout.BeginScrollView(_actionsScroll,
                    GUILayout.Height(bottomH));
                DrawCrucibleSection(pid, player, 0f); // 0 = natural height inside scroll
                GUILayout.Space(4f);
                GUILayout.Label($"── ACTIONS ({hint}) ──");
                GUILayout.Label($"Selected: {_selectedCards.Count} card(s)");
                GUILayout.Space(2f);
                DrawActionPanel(hs, pid, player, hint);
                GUILayout.EndScrollView();
            }
        }

        private void DrawGameOverPanel()
        {
            GUILayout.Space(20f);

            var prevSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 22;
            GUILayout.Label("══ GAME OVER ══");
            GUI.skin.label.fontSize = prevSize;

            GUILayout.Space(8f);

            if (_session!.WinnerPlayerId.HasValue)
            {
                int wid = _session.WinnerPlayerId.Value;
                var wp  = _session.Players[wid];
                GUILayout.Label($"Winner: P{wid} ({wp.Color})");
            }
            else
            {
                GUILayout.Label("Game ended — no winner recorded.");
            }

            GUILayout.Space(10f);
            GUILayout.Label("── Final Stone Positions ──");
            foreach (var p in _session.Players)
                GUILayout.Label($"P{p.PlayerId} ({p.Color}): position {p.StonePosition} [{p.StoneState}]");

            GUILayout.Space(10f);
            GUILayout.Label("── Reagents at End ──");
            foreach (var p in _session.Players)
                GUILayout.Label(
                    $"P{p.PlayerId}: " +
                    $"Sa:{p.GetReagent(ReagentType.Salt)} " +
                    $"Su:{p.GetReagent(ReagentType.Sulphur)} " +
                    $"AR:{p.GetReagent(ReagentType.AquaRegia)} " +
                    $"V:{p.GetReagent(ReagentType.Vitriol)} " +
                    $"Qk:{p.GetReagent(ReagentType.Quicksilver)}");

            GUILayout.Space(16f);
            GUILayout.Label("Press ■ Stop in the Unity toolbar to exit Play Mode.");
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
            else
            {
                GUILayout.Label("Waiting for game loop…");
            }
        }

        // ── Card lists ───────────────────────────────────────────────────────────

        private void DrawCardLists(int pid, PlayerState player, float barH, float barW)
        {
            const float COL_GAP    = 6f;
            const float LABEL_H    = 20f;
            float colW = (barW - COL_GAP * 2f) / 3f;
            float scrollH = barH - LABEL_H;

            // Arcanum counts (needed for Arcanum column header)
            int adeptCount = 0, fateCount = 0, adeptLimit = 2;
            if (_db != null && _session != null)
            {
                foreach (var id in player.Arcanum)
                {
                    var cardInst = _session.GetCard(id);
                    var cardDef  = cardInst != null ? _db.GetById(cardInst.DefinitionId) : null;
                    if (cardDef?.MajorArcanaType == MajorArcanaType.Adept)    adeptCount++;
                    else if (cardDef?.MajorArcanaType == MajorArcanaType.Fate) fateCount++;
                    if (cardDef?.ArcanaNumber == 9) adeptLimit = 3; // The Hermit
                }
            }

            GUILayout.BeginHorizontal();

            // ── Column 1: Spread ────────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(colW));
            GUILayout.Label($"── SPREAD ({player.Spread.Count}) ──");
            _spreadScroll = GUILayout.BeginScrollView(_spreadScroll, GUILayout.Height(scrollH));
            foreach (var id in player.Spread)
                DrawCardToggle(id);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(COL_GAP);

            // ── Column 2: Hand ──────────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(colW));
            GUILayout.Label($"── HAND ({player.Hand.Count}) ──");
            _handScroll = GUILayout.BeginScrollView(_handScroll, GUILayout.Height(scrollH));
            foreach (var id in player.Hand)
                DrawCardToggle(id);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(COL_GAP);

            // ── Column 3: Arcanum ───────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(colW));
            GUILayout.Label($"── ARCANUM — Adepts: {adeptCount}/{adeptLimit}  Fates: {fateCount} ──");
            _arcanumScroll = GUILayout.BeginScrollView(_arcanumScroll, GUILayout.Height(scrollH));
            foreach (var id in player.Arcanum)
            {
                bool arrested = player.ArrestedAdepts.Contains(id);
                string arrestTag = arrested ? "  [ARRESTED]" : "";
                GUILayout.Label($"  ★ {CardLabel(id)}{arrestTag}");
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
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

        // scrollH == 0 means render at natural height (when already inside a parent scroll view)
        private void DrawCrucibleSection(int pid, PlayerState player, float scrollH)
        {
            string codexLabel = player.AssignedCodex != CodexVariant.None
                ? $"── YOUR CRUCIBLE CARDS  [Codex {player.AssignedCodex}] ──"
                : "── YOUR CRUCIBLE CARDS ──";
            GUILayout.Label(codexLabel);

            void DrawSlots()
            {
                for (int i = 0; i < player.CrucibleSlots.Count; i++)
                {
                    var slot = player.CrucibleSlots[i];
                    var inst = _session!.GetCard(slot.CardInstanceId);
                    var def  = inst != null ? _db?.GetById(inst.DefinitionId) : null;

                    // Card name is always visible; alchemical formula only once Active.
                    string cardName = def != null ? $"[{def.CrucibleGroup}] {def.EffectType}" : "?";
                    string coalStr  = slot.HasCoal ? " ·Coal" : "";
                    string wardStr  = slot.WardCount > 0 ? $" Ward×{slot.WardCount}" : "";
                    GUILayout.Label($"Slot {i}: {slot.State,-10} {cardName}{coalStr}{wardStr}");

                    // Codex activation formula for this slot (always visible to owner).
                    if (_codexDb != null && player.AssignedCodex != CodexVariant.None)
                    {
                        var formula = _codexDb.GetFormula(player.AssignedCodex, i);
                        if (formula != null)
                            GUILayout.Label($"   Codex formula: {formula.DisplayName}");
                    }

                    // Alchemical formula: hidden while Dormant, revealed once Active.
                    if (slot.State >= CrucibleCardState.Active && def != null && def.AlchemicalFormula != "")
                        GUILayout.Label($"   Alchemical Formula: {def.AlchemicalFormula}");
                    else if (slot.State == CrucibleCardState.Dormant)
                        GUILayout.Label("   Alchemical Formula: [hidden until activated]");
                }
            }

            if (scrollH > 0f)
            {
                _crucibleScroll = GUILayout.BeginScrollView(_crucibleScroll, GUILayout.Height(scrollH));
                DrawSlots();
                GUILayout.EndScrollView();
            }
            else
            {
                DrawSlots();
            }
        }

        // ── Commune panel ────────────────────────────────────────────────────────

        private void DrawCommunePanel(HotSeatController hs, int pid, PlayerState player, float colH)
        {
            // Lazy-initialise: only minor arcana start in Spread column (Major Arcana live in Arcanum)
            if (!_communeInit)
            {
                _communeSpread.Clear();
                _communeHand.Clear();
                foreach (var id in player.Spread) if (IsMinorArcana(id)) _communeSpread.Add(id);
                foreach (var id in player.Hand)   if (IsMinorArcana(id)) _communeSpread.Add(id);
                _communeInit = true;
            }

            GUILayout.Label($"── COMMUNE — Assign {_communeSpread.Count + _communeHand.Count} card(s) ──");
            if (player.Arcanum.Count > 0)
                GUILayout.Label($"Arcanum ({player.Arcanum.Count}): " +
                    string.Join(", ", player.Arcanum.ConvertAll(CardLabel)) +
                    " — already placed");
            GUILayout.Label($"Hand limit: 5  (currently → Hand: {_communeHand.Count})");

            // ── Action controls pinned at the top so they're always visible ──────
            GUILayout.Space(4f);
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

            bool tooManyHand = _communeHand.Count > 5;
            if (tooManyHand)
            {
                var prev = GUI.contentColor;
                GUI.contentColor = Color.yellow;
                GUILayout.Label($"⚠ Hand limit is 5 — move {_communeHand.Count - 5} card(s) to Spread.");
                GUI.contentColor = prev;
            }

            GUI.enabled = !tooManyHand;
            if (GUILayout.Button("✔ End Turn — Confirm Commune", GUILayout.Height(30f)))
            {
                var spreadIds = new List<string>(_communeSpread);
                var handIds   = new List<string>(_communeHand);
                _communeInit  = false;
                SubmitAction(hs, new CommuneCommand(pid, spreadIds, handIds));
            }
            GUI.enabled = true;

            // ── Scrollable card lists below ───────────────────────────────────────
            GUILayout.Space(6f);
            // Reserve ~110px for header + buttons above; split remainder equally
            float listH = (colH - 140f) * 0.5f;

            GUILayout.Label($"→ SPREAD ({_communeSpread.Count})  [click to move to Hand]");
            _commSpreadScroll = GUILayout.BeginScrollView(_commSpreadScroll, GUILayout.Height(listH));
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

            GUILayout.Label($"→ HAND ({_communeHand.Count})  [click to move to Spread]");
            _commHandScroll = GUILayout.BeginScrollView(_commHandScroll, GUILayout.Height(listH));
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
        }

        // ── Adept decision panel ──────────────────────────────────────────────────

        // Tracks the Adept instance ID the player chose to evict when swapping
        private string? _swapOutAdeptId;

        private void DrawAdeptDecisionPanel(HotSeatController hs, int pid, PlayerState player)
        {
            string adeptId = _loop?.PendingCardId ?? "";
            var inst = _session?.GetCard(adeptId);
            var def  = inst != null ? _db?.GetById(inst.DefinitionId) : null;

            GUILayout.Label("── ADEPT CARD DRAWN ──");
            GUILayout.Space(6f);

            if (def != null)
            {
                GUILayout.Label($"Card:   ★{def.ArcanaNumber}  {def.EffectType}");
                GUILayout.Label($"Sign:   {def.Sign}   Planet: {def.Planet}");
                GUILayout.Label($"Effect: {def.EffectText}");
            }
            else
            {
                GUILayout.Label("(Unknown Adept card)");
            }

            GUILayout.Space(8f);

            // Determine which Arcanum slots are Adepts vs Fate cards
            var arcanumAdepts = new List<string>();
            if (_db != null && _session != null)
            {
                foreach (var id in player.Arcanum)
                {
                    var cardInst = _session.GetCard(id);
                    var cardDef  = cardInst != null ? _db.GetById(cardInst.DefinitionId) : null;
                    if (cardDef?.MajorArcanaType == MajorArcanaType.Adept)
                        arcanumAdepts.Add(id);
                }
            }

            // Determine Arcanum limit (The Hermit bumps it from 2 → 3)
            int arcanaLimit = 2;
            if (_db != null && _session != null)
            {
                foreach (var id in player.Arcanum)
                {
                    var cardInst = _session.GetCard(id);
                    var cardDef  = cardInst != null ? _db.GetById(cardInst.DefinitionId) : null;
                    if (cardDef?.ArcanaNumber == 9) { arcanaLimit = 3; break; }
                }
            }

            bool arcanumFull = arcanumAdepts.Count >= arcanaLimit;
            GUILayout.Label($"Arcanum: {arcanumAdepts.Count} / {arcanaLimit} Adept(s)");
            GUILayout.Label("Cost to purchase: discard 3 cards from your Spread or Hand.");

            // ── Swap-out selector (only shown when Arcanum is at capacity) ─────────
            if (arcanumFull)
            {
                GUILayout.Space(4f);
                GUILayout.Label("── ARCANUM FULL — Choose one to swap out: ──");
                foreach (var id in arcanumAdepts)
                {
                    var cardInst = _session?.GetCard(id);
                    var cardDef  = cardInst != null ? _db?.GetById(cardInst.DefinitionId) : null;
                    string tag   = _swapOutAdeptId == id ? "▶ " : "  ";
                    string lbl   = tag + (cardDef != null
                        ? $"★{cardDef.ArcanaNumber} {cardDef.EffectType} ({cardDef.Sign})"
                        : id);
                    if (GUILayout.Button(lbl))
                        _swapOutAdeptId = (_swapOutAdeptId == id) ? null : id;
                }
                GUILayout.Space(4f);
            }
            else
            {
                _swapOutAdeptId = null; // clear if Arcanum no longer full
            }

            // ── Payment card selector (minor arcana only) ─────────────────────────
            GUILayout.Space(4f);
            GUILayout.Label("── SELECT 3 PAYMENT CARDS (minor arcana only) ──");
            var allCards = new List<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) if (IsMinorArcana(id)) allCards.Add(id);
            foreach (var id in player.Hand)   if (IsMinorArcana(id)) allCards.Add(id);

            _actionsScroll = GUILayout.BeginScrollView(_actionsScroll, GUILayout.Height(140f));
            foreach (var id in allCards)
            {
                bool sel = _selectedCards.Contains(id);
                string label = (sel ? "✔ " : "  ") + CardLabel(id);
                if (GUILayout.Button(label))
                {
                    if (sel) _selectedCards.Remove(id);
                    else     _selectedCards.Add(id);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            GUILayout.Label($"Payment: {_selectedCards.Count} / 3 selected");

            // Buy requires 3 payment cards; if Arcanum is full, also requires a swap target
            bool canBuy = _selectedCards.Count == 3 && def != null
                          && (!arcanumFull || _swapOutAdeptId != null);

            GUILayout.Space(6f);
            GUI.enabled = canBuy;
            string buyLabel = arcanumFull
                ? $"Buy  (swap out {CardLabel(_swapOutAdeptId ?? "?")})"
                : "Buy Adept  (discard 3 selected)";
            if (GUILayout.Button(buyLabel))
            {
                var payment = _selectedCards.ToList();
                string? swapOut = arcanumFull ? _swapOutAdeptId : null;
                _selectedCards.Clear();
                _swapOutAdeptId = null;
                SubmitAction(hs, new BuyAdeptCommand(pid, adeptId, payment, swapOut));
            }
            GUI.enabled = true;

            GUILayout.Space(4f);
            if (GUILayout.Button("Discard  (skip purchase)"))
            {
                _selectedCards.Clear();
                _swapOutAdeptId = null;
                SubmitAction(hs, new DeclineAdeptCommand(pid, adeptId));
            }
        }

        // ── Fate: Moon decision ───────────────────────────────────────────────────

        // Scroll position for Moon's 4-card offer list
        private Vector2 _fateMoonScroll;
        private readonly HashSet<string> _moonKeep = new();

        private void DrawFateMoonPanel(HotSeatController hs, int pid, PlayerState player, float colH)
        {
            GUILayout.Label("── THE MOON — Keep 2 of 4 ──");
            GUILayout.Space(4f);
            GUILayout.Label("Select exactly 2 cards to keep. The rest return to the bottom of the deck.");
            GUILayout.Space(4f);

            // Show only the 4 Moon-specific drawn cards (tracked on BoardState)
            var moonCards = _session?.Board.FateMoonDrawnCardIds ?? new System.Collections.Generic.List<string>();

            if (moonCards.Count == 0)
            {
                GUILayout.Label("(Waiting for Moon cards to be drawn…)");
                return;
            }

            _fateMoonScroll = GUILayout.BeginScrollView(_fateMoonScroll, GUILayout.Height(200f));
            foreach (var id in moonCards)
            {
                bool sel = _moonKeep.Contains(id);
                string label = (sel ? "★ " : "  ") + CardLabel(id);
                if (GUILayout.Button(label))
                {
                    if (sel) _moonKeep.Remove(id);
                    else     _moonKeep.Add(id);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label($"Keeping: {_moonKeep.Count} / 2");

            GUI.enabled = _moonKeep.Count == 2;
            if (GUILayout.Button("Confirm Keep"))
            {
                var keep = _moonKeep.ToList();
                _moonKeep.Clear();
                SubmitAction(hs, new FateMoonDecisionCommand(pid, keep));
            }
            GUI.enabled = true;
        }

        // ── Fate: Reagent choice (Fool / Lovers) ──────────────────────────────────

        private void DrawFateReagentChoicePanel(HotSeatController hs, int pid)
        {
            GUILayout.Label("── FATE: Choose 1 Reagent ──");
            GUILayout.Space(6f);
            GUILayout.Label("Pick one Reagent to receive:");
            GUILayout.Space(4f);

            var reagents = new[]
            {
                ReagentType.Salt, ReagentType.Sulphur, ReagentType.AquaRegia,
                ReagentType.Vitriol, ReagentType.Quicksilver
            };
            foreach (var r in reagents)
            {
                if (GUILayout.Button($"Take 1 {r}"))
                    SubmitAction(hs, new FateReagentChoiceCommand(pid, r));
            }
        }

        private void DrawFateLoversTargetPickPanel(HotSeatController hs, int pid)
        {
            GUILayout.Label("── THE LOVERS ──");
            GUILayout.Space(4f);
            GUILayout.Label("Pick an opponent to choose your reward:");
            GUILayout.Space(6f);

            if (_session == null) return;
            foreach (var opp in _session.Players)
            {
                if (opp.PlayerId == pid) continue;
                if (GUILayout.Button($"P{opp.PlayerId} chooses my reward"))
                    SubmitAction(hs, new FateLoversTargetCommand(pid, opp.PlayerId));
            }
        }

        private void DrawFateLoversChoicePanel(HotSeatController hs, int pid)
        {
            GUILayout.Label("── THE LOVERS ──");
            GUILayout.Space(4f);
            GUILayout.Label("Choose the drawer's reward (only they receive it):");
            GUILayout.Space(6f);

            if (GUILayout.Button("Drawer Draws 2 Cards"))
                SubmitAction(hs, new FateLoversChoiceCommand(pid, drawCards: true));

            GUILayout.Space(4f);
            GUILayout.Label("  — or —  Drawer Receives 1 Reagent:");

            var reagents = new[]
            {
                ReagentType.Salt, ReagentType.Sulphur, ReagentType.AquaRegia,
                ReagentType.Vitriol, ReagentType.Quicksilver
            };
            foreach (var r in reagents)
            {
                if (GUILayout.Button($"Drawer receives 1 {r}"))
                    SubmitAction(hs, new FateLoversChoiceCommand(pid, drawCards: false, r));
            }
        }

        // ── Action panel (Summer / Autumn / None) ────────────────────────────────

        private void DrawActionPanel(HotSeatController hs, int pid, PlayerState player, ActionHint hint)
        {
            if (hint == ActionHint.SummerAction)
                DrawSummerActions(hs, pid, player);
            else if (hint == ActionHint.AutumnAction)
                DrawAutumnActions(hs, pid, player);
            else if (hint == ActionHint.WinterAction)
                DrawWinterActions(hs, pid, player);
            else if (hint == ActionHint.DiscardToLimit)
                DrawDiscardToLimitPanel(hs, pid, player);
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

            // Activate Crucible (one button per Dormant slot; formula shown in tooltip label)
            GUILayout.Label($"Activate Crucible [{selCount} selected]:");
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Dormant) continue;

                string formulaLabel = "?";
                if (_codexDb != null && player.AssignedCodex != CodexVariant.None)
                {
                    var formula = _codexDb.GetFormula(player.AssignedCodex, i);
                    if (formula != null) formulaLabel = formula.DisplayName;
                }

                int captured = i;
                GUI.enabled = selCount >= 1;
                if (GUILayout.Button($"Activate Slot {captured}  [{formulaLabel}]"))
                    SubmitAction(hs, new ActivateCrucibleCommand(pid, captured, selList));
            }
            GUI.enabled = true;

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

            // Build Astral House — only on current sign, needs 2 planet-matching cards, unplaced houses remaining
            DrawBuildHouseButton(hs, pid, player, selList);

            // Place Card Ward — for Active slots (no selection needed; choose reagent per slot)
            bool hasActiveSlots = player.CrucibleSlots.Exists(s =>
                s.State == CrucibleCardState.Active || s.State == CrucibleCardState.Fired);
            if (hasActiveSlots)
            {
                GUILayout.Space(4f);
                GUILayout.Label("── Place Card Ward (spend 1 Reagent on slot) ──");
                for (int i = 0; i < player.CrucibleSlots.Count; i++)
                {
                    var slot = player.CrucibleSlots[i];
                    if (slot.State != CrucibleCardState.Active && slot.State != CrucibleCardState.Fired) continue;
                    int captured = i;
                    GUILayout.Label($"  Slot {captured} [{slot.State}]  Wards: {slot.WardCount}");
                    GUILayout.BeginHorizontal();
                    foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
                    {
                        int have = player.GetReagent(rt);
                        GUI.enabled = have > 0;
                        if (GUILayout.Button($"{rt}({have})"))
                            SubmitAction(hs, new PlaceCardWardCommand(pid, captured, rt));
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }

            // ── Trade ────────────────────────────────────────────────────────────
            GUILayout.Space(4f);
            DrawTradePanel(hs, pid, player);

            // ── Duel ─────────────────────────────────────────────────────────────
            GUILayout.Space(4f);
            GUILayout.Label("── Duel (ante 1 selected Spread card) ──");
            bool canDuel = selCount == 1 && _session != null;
            GUILayout.BeginHorizontal();
            if (_session != null)
            {
                foreach (var opp in _session.Players)
                {
                    if (opp.PlayerId == pid) continue;
                    GUI.enabled = canDuel;
                    if (GUILayout.Button($"Duel P{opp.PlayerId}"))
                    {
                        var anteId = selList[0];
                        SubmitAction(hs, new InitiateDuelCommand(pid, opp.PlayerId, anteId));
                    }
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            // ── Gambit ───────────────────────────────────────────────────────────
            GUILayout.Space(2f);
            GUILayout.Label("── Gambit (offer 1 selected Active/Arcanum card) ──");
            bool selIsOfferable = selCount == 1 && (
                player.CrucibleSlots.Exists(s => s.CardInstanceId == (selCount > 0 ? selList[0] : "") && s.State == CrucibleCardState.Active)
                || (selCount > 0 && player.Arcanum.Contains(selList[0])));
            GUILayout.BeginHorizontal();
            if (_session != null)
            {
                foreach (var opp in _session.Players)
                {
                    if (opp.PlayerId == pid) continue;
                    GUI.enabled = selIsOfferable;
                    if (GUILayout.Button($"Gambit P{opp.PlayerId}"))
                        SubmitAction(hs, new InitiateGambitCommand(pid, opp.PlayerId, selList[0]));
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            // ── Free Arrested ─────────────────────────────────────────────────────
            bool hasArrested = player.CrucibleSlots.Exists(s => s.State == CrucibleCardState.Arrested);
            if (hasArrested)
            {
                GUILayout.Space(2f);
                GUILayout.Label($"── Free Arrested Slot (costs 1 Salt, have {player.GetReagent(ReagentType.Salt)}) ──");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < player.CrucibleSlots.Count; i++)
                {
                    var slot = player.CrucibleSlots[i];
                    if (slot.State != CrucibleCardState.Arrested) continue;
                    int captured = i;
                    GUI.enabled = player.GetReagent(ReagentType.Salt) >= 1;
                    if (GUILayout.Button($"Free Slot {captured}"))
                        SubmitAction(hs, new FreeArrestedCommand(pid, captured));
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            // ── Refresh Arrested Adept (Tower fate) ───────────────────────────────
            if (player.ArrestedAdepts.Count > 0)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"── Refresh Arrested Adept  (costs 1 Salt, have {player.GetReagent(ReagentType.Salt)}) ──");
                GUILayout.BeginHorizontal();
                foreach (var adeptId in player.ArrestedAdepts.ToList())
                {
                    GUI.enabled = player.GetReagent(ReagentType.Salt) >= 1;
                    if (GUILayout.Button($"Refresh {CardLabel(adeptId)}"))
                        SubmitAction(hs, new RefreshAdeptCommand(pid, adeptId));
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("Pass"))
                SubmitAction(hs, new PassCrucibleActionCommand(pid));
        }

        // ── Trade panel (Summer) ──────────────────────────────────────────────────

        private void DrawTradePanel(HotSeatController hs, int pid, PlayerState player)
        {
            GUILayout.Label("── Trade (exchange Spread cards with an opponent) ──");

            if (_session == null) return;

            // Target player selector
            GUILayout.Label($"Target: {(_tradeTargetId < 0 ? "none" : $"P{_tradeTargetId}")}");
            GUILayout.BeginHorizontal();
            foreach (var opp in _session.Players)
            {
                if (opp.PlayerId == pid) continue;
                GUI.backgroundColor = _tradeTargetId == opp.PlayerId ? Color.yellow : Color.white;
                if (GUILayout.Button($"P{opp.PlayerId}"))
                {
                    _tradeTargetId = opp.PlayerId;
                    _tradeOfferIds.Clear();
                    _tradeRequestIds.Clear();
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            if (_tradeTargetId < 0) return;

            var target = _session.Players[_tradeTargetId];

            // Offer: cards from own Spread
            GUILayout.Label("Offer (your Spread — click to toggle):");
            GUILayout.BeginHorizontal();
            foreach (var id in player.Spread.ToList())
            {
                if (!IsMinorArcana(id)) continue;
                bool sel = _tradeOfferIds.Contains(id);
                GUI.backgroundColor = sel ? Color.green : Color.white;
                if (GUILayout.Button(CardLabel(id)))
                {
                    if (sel) _tradeOfferIds.Remove(id); else _tradeOfferIds.Add(id);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Request: cards from target's Spread
            GUILayout.Label($"Request (P{_tradeTargetId}'s Spread — click to toggle):");
            GUILayout.BeginHorizontal();
            foreach (var id in target.Spread.ToList())
            {
                if (!IsMinorArcana(id)) continue;
                bool sel = _tradeRequestIds.Contains(id);
                GUI.backgroundColor = sel ? Color.cyan : Color.white;
                if (GUILayout.Button(CardLabel(id)))
                {
                    if (sel) _tradeRequestIds.Remove(id); else _tradeRequestIds.Add(id);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            bool validTrade = _tradeOfferIds.Count > 0 || _tradeRequestIds.Count > 0;
            GUI.enabled = validTrade;
            if (GUILayout.Button($"Execute Trade  (give {_tradeOfferIds.Count} / receive {_tradeRequestIds.Count})"))
            {
                SubmitAction(hs, new DirectTradeCommand(pid, _tradeTargetId,
                    _tradeOfferIds.ToList(), _tradeRequestIds.ToList()));
                _tradeTargetId = -1;
                _tradeOfferIds.Clear();
                _tradeRequestIds.Clear();
            }
            GUI.enabled = true;
        }

        private void DrawBuildHouseButton(HotSeatController hs, int pid, PlayerState player,
            List<string> selList)
        {
            GUILayout.Space(2f);
            GUILayout.Label("── Build Astral House ──");

            var sign   = player.CurrentSign;
            bool hasHouses = player.UnplacedAstralHouses > 0;
            bool signFree  = true;
            if (_session != null)
                foreach (var p in _session.Players)
                    if (p.PlayerId != pid && p.AstralHouses.Contains(sign))
                    { signFree = false; break; }

            bool alreadyBuilt = player.AstralHouses.Contains(sign);
            bool canBuild     = hasHouses && signFree && !alreadyBuilt && selList.Count == 1;

            string houseLabel = !hasHouses
                ? "Build House  (no tokens left)"
                : !signFree
                    ? $"Build House on {sign}  (sign taken)"
                    : alreadyBuilt
                        ? $"Build House on {sign}  (already built)"
                        : $"Build House on {sign}  (select 1 planet-matching card)";

            GUI.enabled = canBuild;
            if (GUILayout.Button(houseLabel))
                SubmitAction(hs, new BuildAstralHouseCommand(pid, sign,
                    selList.Count >= 1 ? new List<string> { selList[0] } : selList));
            GUI.enabled = true;
        }

        private void DrawAutumnActions(HotSeatController hs, int pid, PlayerState player)
        {
            int selCount = _selectedCards.Count;
            var selList  = _selectedCards.ToList();

            // ── Stone status ──────────────────────────────────────────────────────
            GUILayout.Label($"Stone: {player.StonePosition}  [{player.StoneState}]" +
                (player.StoneWardCount > 0 ? $"  🛡️ {player.StoneWardCount}" : "") +
                (player.ReturnedFromStasisThisRound ? "  (Returned from Stasis — cannot Temper)" : ""));
            GUILayout.Space(4f);

            // ── Fire Stone ────────────────────────────────────────────────────────
            // Fire requires: stone at Mantle, slot Active, selection = alignment cards
            bool canFire = player.StonePosition.IsMantle;
            GUILayout.Label($"Fire Stone  [{selCount} alignment cards selected from Spread]:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                GUI.enabled = slot.State == CrucibleCardState.Active && canFire;
                int captured = i;

                // Show the formula string for this slot if available
                string formulaHint = "";
                if (_db != null && _session != null)
                {
                    var ci  = _session.GetCard(slot.CardInstanceId);
                    var def = ci != null ? _db.GetById(ci.DefinitionId) : null;
                    if (def != null) formulaHint = $" ({def.AlchemicalFormula})";
                }

                if (GUILayout.Button($"Fire Slot {captured}{formulaHint}"))
                    SubmitAction(hs, new FireStoneCommand(pid, captured, selList));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // ── Temper ────────────────────────────────────────────────────────────
            bool canTemper = player.StoneState == StoneState.Forging
                          && player.StonePosition.IsForge
                          && !player.ReturnedFromStasisThisRound;
            GUI.enabled = canTemper;
            if (GUILayout.Button($"Temper Stone  → {player.StonePosition.Advance()}"))
                SubmitAction(hs, new TemperCommand(pid));
            GUI.enabled = true;

            // ── Leave Stasis ──────────────────────────────────────────────────────
            GUI.enabled = player.StoneState == StoneState.Stasis;
            if (GUILayout.Button($"Leave Stasis  (costs 2 Salt, have {player.GetReagent(ReagentType.Salt)})  → {player.StonePosition}"))
                SubmitAction(hs, new LeaveStasisCommand(pid));
            GUI.enabled = true;

            GUILayout.Space(2f);

            // ── Place Forge Ward ──────────────────────────────────────────────────
            GUILayout.Space(2f);
            bool canPlaceForgeWard = player.StoneState == StoneState.Forging;
            GUI.enabled = canPlaceForgeWard;
            GUILayout.Label($"Place Forge Ward (current: {player.StoneWardCount}) — spend 1 Reagent:");
            GUILayout.BeginHorizontal();
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
            {
                int have = player.GetReagent(rt);
                GUI.enabled = canPlaceForgeWard && have > 0;
                if (GUILayout.Button($"{rt}({have})"))
                    SubmitAction(hs, new PlaceStoneWardCommand(pid, rt));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            // ── Oppose — one button per opponent who is Forging ───────────────────
            GUILayout.Space(2f);
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

        // ── Winter Activities (free-action pool) ─────────────────────────────────

        private void DrawWinterActions(HotSeatController hs, int pid, PlayerState player)
        {
            GUILayout.Label("── Winter Activities ──");

            // ── Card movement: Hand → Spread ──────────────────────────────────────
            GUILayout.Label("Move card to Spread:");
            GUILayout.BeginHorizontal();
            if (_session != null)
            {
                foreach (var id in player.Hand.ToList())
                {
                    if (!IsMinorArcana(id)) continue;
                    string label = CardLabel(id);
                    if (GUILayout.Button($"→ Spread  {label}"))
                        SubmitAction(hs, new WinterMoveCardCommand(pid, id, toSpread: true));
                }
            }
            GUILayout.EndHorizontal();

            // ── Card movement: Spread → Hand ──────────────────────────────────────
            GUILayout.Label("Move card to Hand:");
            GUILayout.BeginHorizontal();
            if (_session != null)
            {
                foreach (var id in player.Spread.ToList())
                {
                    if (!IsMinorArcana(id)) continue;
                    string label = CardLabel(id);
                    if (GUILayout.Button($"→ Hand  {label}"))
                        SubmitAction(hs, new WinterMoveCardCommand(pid, id, toSpread: false));
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // ── Craft Salt ────────────────────────────────────────────────────────
            var selList  = _selectedCards.ToList();
            int selCount = selList.Count;
            GUI.enabled = selCount >= 3;
            if (GUILayout.Button($"Craft Salt  ({selCount}/3 sel)"))
                SubmitAction(hs, new CraftReagentCommand(pid, ReagentType.Salt, selList));
            GUI.enabled = true;

            // ── Craft Elemental ───────────────────────────────────────────────────
            Suit? wUSuit = GetUniformSuit(selList);
            bool wCauldronLit = wUSuit.HasValue && player.IsCauldronLit(wUSuit.Value);
            GUI.enabled = selCount >= 3 && wUSuit.HasValue && wCauldronLit;
            string wCraftLabel = wUSuit.HasValue
                ? $"Craft {wUSuit.Value} Reagent  (cauldron {(wCauldronLit ? "lit" : "UNLIT")})"
                : "Craft Elemental  (select 3+ same-suit)";
            if (GUILayout.Button(wCraftLabel))
            {
                var rtype = SuitToReagent(wUSuit!.Value);
                SubmitAction(hs, new CraftReagentCommand(pid, rtype, selList));
            }
            GUI.enabled = true;

            GUILayout.Space(4f);

            // ── Fateful Wager sub-panel ───────────────────────────────────────────
            bool alreadyWagered = player.FatefulWagerSign != ZodiacSign.None;
            GUILayout.Label($"── Fateful Wager {(alreadyWagered ? $"[PLACED on {player.FatefulWagerSign}]" : "")} ──");

            if (alreadyWagered)
            {
                GUILayout.Label($"  Wagered {player.FatefulWagerCards.Count} card(s) on {player.FatefulWagerSign}. Resolved next Spring.");
            }
            else
            {
                // Sign picker
                GUILayout.Label($"Predict sign: [{_wagerSign}]");
                GUILayout.BeginHorizontal();
                foreach (ZodiacSign sign in Enum.GetValues(typeof(ZodiacSign)))
                {
                    if (sign == ZodiacSign.None) continue;
                    GUI.backgroundColor = _wagerSign == sign ? Color.yellow : Color.white;
                    if (GUILayout.Button(sign.ToString().Substring(0, 3), GUILayout.Width(36)))
                        _wagerSign = sign;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Card multi-selector (minor arcana from Hand + Spread)
                GUILayout.Label("Toggle cards to wager:");
                GUILayout.BeginHorizontal();
                foreach (var id in player.Spread.Concat(player.Hand).ToList())
                {
                    if (!IsMinorArcana(id)) continue;
                    bool toggled = _wagerCards.Contains(id);
                    GUI.backgroundColor = toggled ? Color.cyan : Color.white;
                    string label = CardLabel(id);
                    if (GUILayout.Button(label))
                    {
                        if (toggled) _wagerCards.Remove(id); else _wagerCards.Add(id);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUI.enabled = _wagerSign != ZodiacSign.None && _wagerCards.Count > 0;
                if (GUILayout.Button($"Place Wager on {_wagerSign}  ({_wagerCards.Count} cards)"))
                {
                    SubmitAction(hs, new PlaceFatefulWagerCommand(pid, _wagerSign,
                        _wagerCards.ToList()));
                    _wagerSign = ZodiacSign.None;
                    _wagerCards.Clear();
                }
                GUI.enabled = true;
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("Pass Turn"))
                SubmitAction(hs, new PassActionCommand(pid));
        }

        // ── Winter Discard-to-Limit ──────────────────────────────────────────────

        private void DrawDiscardToLimitPanel(HotSeatController hs, int pid, PlayerState player)
        {
            GUILayout.Label("── Discard to Limit ──");
            GUILayout.Label($"Spread: {player.Spread.Count}/5  Hand: {player.Hand.Count}/5");
            GUILayout.Label("Select cards to DISCARD (click to toggle), then confirm.");

            // Spread cards
            GUILayout.Label("Spread:");
            GUILayout.BeginHorizontal();
            foreach (var id in player.Spread.ToList())
            {
                if (!IsMinorArcana(id)) continue;
                bool sel = _discardSpread.Contains(id);
                GUI.backgroundColor = sel ? new Color(1f, 0.4f, 0.4f) : Color.white;
                if (GUILayout.Button(CardLabel(id)))
                {
                    if (sel) _discardSpread.Remove(id); else _discardSpread.Add(id);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Hand cards
            GUILayout.Label("Hand:");
            GUILayout.BeginHorizontal();
            foreach (var id in player.Hand.ToList())
            {
                if (!IsMinorArcana(id)) continue;
                bool sel = _discardHand.Contains(id);
                GUI.backgroundColor = sel ? new Color(1f, 0.4f, 0.4f) : Color.white;
                if (GUILayout.Button(CardLabel(id)))
                {
                    if (sel) _discardHand.Remove(id); else _discardHand.Add(id);
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            int newSpread = player.Spread.Count - _discardSpread.Count;
            int newHand   = player.Hand.Count   - _discardHand.Count;
            bool valid    = newSpread <= WinterRules.SpreadLimit && newHand <= WinterRules.HandLimit;

            GUILayout.Label($"After discard → Spread: {newSpread}/5  Hand: {newHand}/5");

            GUI.enabled = valid;
            if (GUILayout.Button("Confirm Discard"))
            {
                SubmitAction(hs, new DiscardToLimitCommand(pid,
                    _discardSpread.ToList(), _discardHand.ToList()));
                _discardSpread.Clear();
                _discardHand.Clear();
            }
            GUI.enabled = true;
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

        // ── Card helpers ─────────────────────────────────────────────────────────

        /// <summary>Returns true when the card instance is a minor arcana (not Fate or Adept).</summary>
        private bool IsMinorArcana(string instanceId)
        {
            var inst = _session?.GetCard(instanceId);
            var def  = inst != null ? _db?.GetById(inst.DefinitionId) : null;
            return def != null && !def.IsMajorArcana;
        }

        // ── Card label resolution ────────────────────────────────────────────────

        private string CardLabel(string instanceId)
        {
            var inst = _session?.GetCard(instanceId);
            if (inst == null) return $"[?:{instanceId[..Math.Min(8, instanceId.Length)]}]";
            var def  = _db?.GetById(inst.DefinitionId);
            if (def == null) return $"[?:{inst.DefinitionId}]";

            if (def.IsCrucible)
                return $"[{def.CrucibleGroup}] {def.EffectType}";

            if (def.IsMajorArcana)
                return $"★{def.ArcanaNumber} {def.EffectType}";

            // Minor arcana — suit emoji, rank, suit name, planet, variant
            string planet  = def.Planet != Planet.None ? $" [{def.Planet}]" : "";
            string variant = def.Variant == CardVariant.Two ? " (II)" : "";
            return $"{SuitGlyph(def.Suit)} {def.Rank} {def.Suit}{planet}{variant}";
        }

        private static string SuitGlyph(Suit suit) => suit switch
        {
            Suit.Wands     => "🪄",
            Suit.Cups      => "🍷",
            Suit.Pentacles => "🪙",
            Suit.Swords    => "🗡️",
            _              => "·"
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
            CosmicAgeSetEvent e         => $"[CosmicAge] Sign={e.Sign} Planet={e.Planet}",
            CosmicEffectAppliedEvent e  => $"[CosmicFx] {e.Sign}: {e.EffectSummary}",
            ZodiacRolledEvent e         => $"[Zodiac] P{e.PlayerId} → {e.Sign}",
            CardsDrawnEvent e           => $"[Harvest] P{e.PlayerId} drew {e.Count}",
            AdeptPurchasedEvent e       => $"[Adept] P{e.PlayerId} bought {e.AdeptCardId}",
            AdeptDeclinedEvent e        => $"[Adept] P{e.PlayerId} declined {e.AdeptCardId}",
            AstralHouseBuiltEvent e     => $"[House] P{e.PlayerId} built on {e.Sign}",
            FatefulWagerPlacedEvent e   => $"[Wager] P{e.PlayerId} placed {e.CardCount} card(s) on {e.PredictedSign}",
            FatefulWagerResolvedEvent e => $"[Wager] P{e.PlayerId} {(e.Won ? "WON" : "LOST")} vs CosmicAge={e.Sign} ({e.CardCount} cards)",
            FateResolvedEvent e         => $"[Fate] P{e.PlayerId} drew ★{e.ArcanaNum}",
            StoneFiredEvent e           => $"[Fire] P{e.PlayerId} → pos {e.NewPosition}",
            StoneTemperedEvent e        => $"[Temper] P{e.PlayerId} → pos {e.NewPosition}",
            OppositionResolvedEvent e   => $"[Oppose] Att:P{e.AttackerId}({e.AttackAlign}+{e.AttackRoll}) Def:P{e.DefenderId}({e.DefendAlign}+{e.DefendRoll}) → Loser:P{e.LoserId}",
            DuelResolvedEvent e         => $"[Duel] P{e.AttackerId}({e.AttackRoll}) vs P{e.DefenderId}({e.DefendRoll}) → Winner:P{e.WinnerId}",
            GambitResolvedEvent e       => $"[Gambit] P{e.AttackerId}({e.AttackRoll}) vs P{e.DefenderId}({e.DefendRoll}) → Winner:P{e.WinnerId}",
            CodexAssignedEvent e        => $"[Codex] P{e.PlayerId} assigned Codex {e.Codex}",
            GameSetupCompleteEvent e    => $"[Setup] {e.PlayerCount}p ready",
            AgeTransitedEvent e         => $"[Transit] Round {e.NewRoundNumber} AK→P{e.NewAgekeeperId}",
            GameEndedEvent e            => $"[GAME OVER] Winner: P{e.WinnerPlayerId}",
            PhaseChangedEvent e         => $"[Phase] {e.Season} step {e.StepIndex}",
            _                           => $"[{evt.GetType().Name}]"
        };
    }
}
