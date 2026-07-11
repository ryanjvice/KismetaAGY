using System;
using System.Collections.Generic;
using System.Text;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CardLimitsController : ScreenController
    {
        public override string ScreenId => ScreenIds.CardLimits;

        public Action? OnBack;

        readonly HashSet<string> _discardSpread = new();
        readonly HashSet<string> _discardHand = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        string? _actionCardId;
        bool _actionIsSpread;

        VisualElement? _actionOverlay;
        VisualElement? _actionScrim;
        VisualElement? _actionSheet;
        Button? _actionMoveBtn;
        Button? _actionDiscardBtn;
        Button? _actionBackBtn;

        EventCallback<ClickEvent>? _scrimClickHandler;
        string? _zoneFingerprint;
        int _scrimIgnoreUntilFrame;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += OnBackClicked;
            Btn("transit-btn")!.clicked += OnTransit;

            _actionOverlay = El("card-action-overlay");
            _actionScrim = El("card-action-scrim");
            _actionSheet = El("card-action-sheet");
            _actionMoveBtn = Btn("card-action-move-btn");
            _actionDiscardBtn = Btn("card-action-discard-btn");
            _actionBackBtn = Btn("card-action-back-btn");

            _scrimClickHandler = OnScrimClick;
            _actionScrim?.RegisterCallback(_scrimClickHandler);
            _actionMoveBtn!.clicked += OnActionMove;
            _actionDiscardBtn!.clicked += OnActionDiscard;
            _actionBackBtn!.clicked += OnActionBack;

            SetOverlayVisible(false);
        }

        protected override void Unwire()
        {
            SetOverlayVisible(false);

            if (Btn("back-btn") is { } backBtn)
                backBtn.clicked -= OnBackClicked;
            if (Btn("transit-btn") is { } transitBtn)
                transitBtn.clicked -= OnTransit;

            if (_actionScrim != null && _scrimClickHandler != null)
                _actionScrim.UnregisterCallback(_scrimClickHandler);
            if (_actionMoveBtn != null)
                _actionMoveBtn.clicked -= OnActionMove;
            if (_actionDiscardBtn != null)
                _actionDiscardBtn.clicked -= OnActionDiscard;
            if (_actionBackBtn != null)
                _actionBackBtn.clicked -= OnActionBack;

            _actionOverlay = null;
            _actionScrim = null;
            _actionSheet = null;
            _actionMoveBtn = null;
            _actionDiscardBtn = null;
            _actionBackBtn = null;
            _scrimClickHandler = null;
        }

        void OnBackClicked()
        {
            SetOverlayVisible(false);
            OnBack?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            int pid = SummerActionBindings.ResolvePlayerId(session, bridge);

            // Discard submit clears the pending controller; skip rebuild until we route away.
            if (pid < 0)
            {
                SetOverlayVisible(false);
                return;
            }

            if (pid != _playerId)
            {
                _playerId = pid;
                _discardSpread.Clear();
                _discardHand.Clear();
                _zoneFingerprint = null;
                SetOverlayVisible(false);
            }

            if (Root == null) return;
            Rebuild();
            NarrativeSlotBindings.BindById(Root, "winter.limits");
        }

        void Rebuild(bool forceZones = false)
        {
            if (_session == null || Root == null) return;
            if (_playerId < 0 || _playerId >= _session.Players.Count) return;

            var player = _session.Players[_playerId];
            var db = _session.Rules?.CardDatabase;

            PruneStaleSelections(player);
            UpdateMetrics(player);

            var fingerprint = BuildZoneFingerprint(player);
            bool zonesDirty = forceZones || !string.Equals(fingerprint, _zoneFingerprint, StringComparison.Ordinal);

            if (IsActionSheetOpen())
            {
                if (!string.IsNullOrEmpty(_actionCardId) && !CardStillInZone(_actionCardId, _actionIsSpread, player))
                    SetOverlayVisible(false);
                else if (zonesDirty)
                    RefreshActionSheetLabels();
                return;
            }

            if (!zonesDirty) return;

            RebuildZone("spread-cards", player.Spread, _discardSpread, db, isSpread: true);
            RebuildZone("hand-cards", player.Hand, _discardHand, db, isSpread: false);
            _zoneFingerprint = fingerprint;
        }

        static bool CardStillInZone(string cardId, bool isSpread, PlayerState player) =>
            isSpread ? player.Spread.Contains(cardId) : player.Hand.Contains(cardId);

        string BuildZoneFingerprint(PlayerState player)
        {
            var sb = new StringBuilder(128);
            AppendIds(sb, player.Spread);
            sb.Append('|');
            AppendIds(sb, player.Hand);
            sb.Append('|');
            AppendIds(sb, _discardSpread);
            sb.Append('|');
            AppendIds(sb, _discardHand);
            return sb.ToString();
        }

        static void AppendIds(StringBuilder sb, IEnumerable<string> ids)
        {
            bool first = true;
            foreach (var id in ids)
            {
                if (!first) sb.Append(',');
                sb.Append(id);
                first = false;
            }
        }

        void UpdateMetrics(PlayerState player)
        {
            int handLimit = _session != null
                ? PlayerLimitService.GetHandLimit(_session, player)
                : WinterRules.HandLimit;
            int newSpread = player.Spread.Count - _discardSpread.Count;
            int newHand = player.Hand.Count - _discardHand.Count;
            int toDiscard = Math.Max(0, newSpread - WinterRules.SpreadLimit)
                          + Math.Max(0, newHand - handLimit);

            SetLabelText("spread-limit", $"{newSpread} / {WinterRules.SpreadLimit}");
            SetLabelText("hand-limit", $"{newHand} / {handLimit}");
            SetLabelText("hand-tally", $"{newHand} / {handLimit}");
            SetLabelText("spread-tally", $"{newSpread} / {WinterRules.SpreadLimit}");
            SetLabelText("to-discard", toDiscard.ToString());

            bool valid = newSpread <= WinterRules.SpreadLimit && newHand <= handLimit;
            var transit = Btn("transit-btn");
            if (transit != null)
            {
                transit.EnableInClassList("btn--disabled", !valid);
                transit.EnableInClassList("btn--primary", valid);
                transit.SetEnabled(valid);
                transit.text = valid
                    ? "Transit The Age · Pass The Key"
                    : $"Discard {toDiscard} More To Transit";
            }
        }

        void PruneStaleSelections(PlayerState player)
        {
            _discardSpread.RemoveWhere(id => !player.Spread.Contains(id));
            _discardHand.RemoveWhere(id => !player.Hand.Contains(id));
        }

        void RebuildZone(string containerName, IReadOnlyList<string> cardIds,
            HashSet<string> discardSet, ICardDatabase? db, bool isSpread)
        {
            var zone = El(containerName);
            if (zone == null || _session == null) return;
            zone.Clear();

            int kept = cardIds.Count - discardSet.Count;
            int limit = isSpread
                ? WinterRules.SpreadLimit
                : (_session != null ? PlayerLimitService.GetHandLimit(_session, _session.Players[_playerId]) : WinterRules.HandLimit);
            bool zoneOver = kept > limit;
            int overCount = kept - limit;

            foreach (var id in cardIds)
            {
                if (!TapSwapBindings.IsMinorArcana(_session, id)) continue;
                var inst = _session.GetCard(id);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool marked = discardSet.Contains(id);
                bool isOverLimit = !marked && zoneOver && overCount > 0;
                if (isOverLimit) overCount--;

                var chip = CardChipFactory.CreateFromDefinition(def, selected: marked);
                if (isOverLimit)
                    chip.AddToClassList("card-chip--overlimit");

                chip.userData = id;
                WireChipTap(chip, id, isSpread);
                zone.Add(chip);
            }
        }

        void WireChipTap(VisualElement chip, string cardId, bool isSpread)
        {
            chip.pickingMode = PickingMode.Position;
            chip.AddToClassList("card-chip--tappable");
            SetChipChildrenIgnorePicking(chip);

            chip.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                UiMotion.PulseChip(chip);
                ScheduleOpenCardActionSheet(cardId, isSpread);
            });
        }

        static void SetChipChildrenIgnorePicking(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                child.pickingMode = PickingMode.Ignore;
                SetChipChildrenIgnorePicking(child);
            }
        }

        void ScheduleOpenCardActionSheet(string cardId, bool isSpread)
        {
            Root?.schedule.Execute(() => OpenCardActionSheet(cardId, isSpread)).ExecuteLater(0);
        }

        void OpenCardActionSheet(string cardId, bool isSpread)
        {
            if (_session == null || _actionOverlay == null) return;

            var player = _session.Players[_playerId];
            if (!CardStillInZone(cardId, isSpread, player)) return;

            _actionCardId = cardId;
            _actionIsSpread = isSpread;
            bool marked = isSpread ? _discardSpread.Contains(cardId) : _discardHand.Contains(cardId);

            var inst = _session.GetCard(cardId);
            var db = _session.Rules?.CardDatabase;
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;

            var host = El("card-action-chip-host");
            if (host != null)
            {
                host.Clear();
                if (def != null)
                    host.Add(CardChipFactory.CreateFromDefinition(def, selected: marked));
            }

            SetLabelText("card-action-zone-label", isSpread ? "In Spread" : "In Hand");
            SetLabelText("card-action-move-label", isSpread ? "Move to Hand" : "Move to Spread");
            SetLabelText("card-action-discard-label", marked ? "Keep card" : "Mark for discard");

            _scrimIgnoreUntilFrame = Time.frameCount + 1;
            SetOverlayVisible(true);
        }

        void RefreshActionSheetLabels()
        {
            if (string.IsNullOrEmpty(_actionCardId)) return;

            bool marked = _actionIsSpread
                ? _discardSpread.Contains(_actionCardId)
                : _discardHand.Contains(_actionCardId);
            SetLabelText("card-action-discard-label", marked ? "Keep card" : "Mark for discard");
        }

        void OnActionMove()
        {
            if (_bridge == null || string.IsNullOrEmpty(_actionCardId)) return;

            if (_bridge.TryApplySideEffect(new WinterMoveCardCommand(_playerId, _actionCardId, toSpread: !_actionIsSpread)))
            {
                _discardSpread.Remove(_actionCardId);
                _discardHand.Remove(_actionCardId);
                SetOverlayVisible(false);
                _zoneFingerprint = null;
                Rebuild(forceZones: true);
            }
        }

        void OnActionDiscard()
        {
            if (string.IsNullOrEmpty(_actionCardId)) return;

            var discardSet = _actionIsSpread ? _discardSpread : _discardHand;
            if (discardSet.Contains(_actionCardId))
                discardSet.Remove(_actionCardId);
            else
                discardSet.Add(_actionCardId);

            SetOverlayVisible(false);
            _zoneFingerprint = null;
            Rebuild(forceZones: true);
        }

        void OnActionBack() => SetOverlayVisible(false);

        void OnScrimClick(ClickEvent evt)
        {
            if (Time.frameCount <= _scrimIgnoreUntilFrame) return;
            if (!ReferenceEquals(evt.target, _actionScrim)) return;
            SetOverlayVisible(false);
        }

        void SetOverlayVisible(bool visible)
        {
            if (!visible)
                _actionCardId = null;

            if (_actionOverlay == null) return;

            _actionOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _actionOverlay.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;

            if (_actionScrim != null)
                _actionScrim.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            if (_actionSheet != null)
                _actionSheet.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        bool IsActionSheetOpen() =>
            _actionOverlay != null && _actionOverlay.style.display == DisplayStyle.Flex;

        void OnTransit()
        {
            if (_bridge == null || _session == null) return;
            if (_playerId < 0 || _playerId >= _session.Players.Count) return;

            var player = _session.Players[_playerId];
            int newSpread = player.Spread.Count - _discardSpread.Count;
            int newHand = player.Hand.Count - _discardHand.Count;
            int handLimit = PlayerLimitService.GetHandLimit(_session, player);
            if (newSpread > WinterRules.SpreadLimit || newHand > handLimit) return;

            SetOverlayVisible(false);
            if (!_bridge.TrySubmit(new DiscardToLimitCommand(_playerId,
                    new List<string>(_discardSpread), new List<string>(_discardHand))))
                return;

            _discardSpread.Clear();
            _discardHand.Clear();
            _zoneFingerprint = null;
        }

        void SetLabelText(string name, string text)
        {
            var lbl = Lbl(name);
            if (lbl != null) lbl.text = text;
        }
    }
}
