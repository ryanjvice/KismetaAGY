using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CardModalsController : OverlayController
    {
        public enum Modal
        {
            Inspect, Adept, Fate,
            Moon, LoversTarget, LoversChoice, FoolAltar
        }

        static readonly string[] AllModalRoots =
        {
            "inspect-modal", "adept-modal", "fate-modal",
            "moon-modal", "lovers-target-modal", "lovers-choice-modal", "fool-altar-modal"
        };

        GameSession? _session;
        CommandBridge? _bridge;
        string? _inspectCardId;
        string? _adeptCardId;
        readonly HashSet<string> _payment = new();
        readonly HashSet<string> _moonGiftCards = new();
        readonly Dictionary<ReagentType, int> _moonGiftReagents = new();
        readonly HashSet<string> _foolAltarAlignment = new();
        int _moonGiftRecipientId = -1;
        int _foolAltarSlotIndex = -1;
        string? _swapOutAdeptId;
        int _playerId = -1;
        int _loversDrawerId = -1;

        public Action? OnInspectDone;
        public Action? OnAdeptCompleted;
        public Action? OnFateAccept;
        public Action? OnFateDecisionCompleted;

        protected override void Wire()
        {
            Btn("inspect-close")!.clicked += () => OnInspectDone?.Invoke();
            Btn("inspect-done")!.clicked += () => OnInspectDone?.Invoke();
            Btn("adept-place")!.clicked += OnAdeptPlace;
            Btn("adept-hold")!.clicked += OnAdeptHold;
            Btn("fate-accept")!.clicked += () => OnFateAccept?.Invoke();
            Btn("moon-confirm")!.clicked += OnMoonGiftConfirm;
            Btn("fool-altar-confirm")!.clicked += OnFoolAltarConfirm;
            Btn("lovers-draw-btn")!.clicked += OnLoversDraw;
        }

        public void Show(Modal which)
        {
            if (Root == null) return;
            string active = which switch
            {
                Modal.Inspect => "inspect-modal",
                Modal.Adept => "adept-modal",
                Modal.Fate => "fate-modal",
                Modal.Moon => "moon-modal",
                Modal.LoversTarget => "lovers-target-modal",
                Modal.LoversChoice => "lovers-choice-modal",
                Modal.FoolAltar => "fool-altar-modal",
                _ => "inspect-modal"
            };
            foreach (var name in AllModalRoots)
            {
                var el = El(name);
                if (el != null)
                    el.style.display = name == active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void BindInspect(GameSession session, string cardInstanceId)
        {
            _session = session;
            _inspectCardId = cardInstanceId;
            Show(Modal.Inspect);
            if (Root != null)
                CardInspectBindings.BindInspectModal(El("inspect-modal")!, session, cardInstanceId);
        }

        public void BindAdept(GameSession session, CommandBridge bridge, string adeptInstanceId)
        {
            _session = session;
            _bridge = bridge;
            _adeptCardId = adeptInstanceId;
            _playerId = ResolvePlayerId(session, bridge);
            _payment.Clear();
            _swapOutAdeptId = null;
            Show(Modal.Adept);

            var db = session.Rules?.CardDatabase;
            var inst = session.GetCard(adeptInstanceId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;

            var displayName = !string.IsNullOrEmpty(def?.Name) ? def!.Name : "Adept";
            if (Lbl("adept-name") != null)
                Lbl("adept-name")!.text = displayName;
            if (Lbl("adept-power") != null && def != null)
                Lbl("adept-power")!.text = def.EffectText;

            int adeptSlots = CountAdeptsInArcanum();
            int limit = 2;
            if (Lbl("adept-tip") != null)
            {
                Lbl("adept-tip")!.text = adeptSlots >= limit
                    ? $"Your Arcanum is full — placing {displayName} requires swapping an active Adept."
                    : $"Your Arcanum has {limit - adeptSlots} open slot{(limit - adeptSlots == 1 ? "" : "s")} — placing {displayName} fills one. You can hold up to {limit} active.";
            }

            RebuildAdeptPaymentUi();
            RefreshAdeptButtons();
        }

        public void BindFate(GameSession session, string fateInstanceId, int arcanaNum)
        {
            _session = session;
            Show(Modal.Fate);

            var db = session.Rules?.CardDatabase;
            var inst = session.GetCard(fateInstanceId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;

            if (Lbl("fate-name") != null)
                Lbl("fate-name")!.text = !string.IsNullOrEmpty(def?.Name)
                    ? def!.Name
                    : $"Fate #{arcanaNum}";

            var effectsHost = El("fate-effects");
            if (effectsHost != null && def != null && !string.IsNullOrEmpty(def.EffectText))
            {
                effectsHost.Clear();
                var row = new VisualElement();
                row.AddToClassList("effect-row");
                row.Add(new Label(def.EffectText) { style = { fontSize = 11, whiteSpace = WhiteSpace.Normal } });
                effectsHost.Add(row);
            }
        }

        public void BindMoonGift(GameSession session, CommandBridge bridge, int recipientId)
        {
            _session = session;
            _bridge = bridge;
            _playerId = ResolvePlayerId(session, bridge);
            _moonGiftRecipientId = recipientId;
            _moonGiftCards.Clear();
            _moonGiftReagents.Clear();
            Show(Modal.Moon);

            if (Lbl("moon-sr") != null)
                Lbl("moon-sr")!.text =
                    $"Give a meaningful gift to {PlayerUiNames.ShortName(recipientId)} — cards and/or reagents.";
            RefreshMoonGiftUi();
        }

        public void BindFoolAltarClaim(GameSession session, CommandBridge bridge, int slotIndex)
        {
            _session = session;
            _bridge = bridge;
            _playerId = ResolvePlayerId(session, bridge);
            _foolAltarSlotIndex = slotIndex;
            _foolAltarAlignment.Clear();
            Show(Modal.FoolAltar);

            var altarId = session.Board.FoolAltarCrucibleCardId;
            var db = session.Rules?.CardDatabase;
            var def = altarId != null && session.GetCard(altarId) is { } inst && db != null
                ? db.GetById(inst.DefinitionId)
                : null;

            if (Lbl("fool-altar-name") != null)
                Lbl("fool-altar-name")!.text = def?.Name ?? "Altar Crucible";
            if (Lbl("fool-altar-formula") != null)
                Lbl("fool-altar-formula")!.text = def?.AlchemicalFormula ?? "";
            if (Lbl("fool-altar-slot") != null)
                Lbl("fool-altar-slot")!.text = $"Replacing dormant slot {slotIndex + 1}";

            RefreshFoolAltarUi();
        }

        public void BindLoversTarget(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = ResolvePlayerId(session, bridge);
            Show(Modal.LoversTarget);

            var host = El("lovers-target-buttons");
            if (host == null) return;
            FateDecisionBindings.PopulateLoversTargetButtons(host, session, _playerId, targetId =>
            {
                if (_bridge != null && _playerId >= 0
                    && _bridge.TrySubmit(new FateLoversTargetCommand(_playerId, targetId)))
                    OnFateDecisionCompleted?.Invoke();
            });
        }

        public void BindLoversChoice(GameSession session, CommandBridge bridge, int drawerId)
        {
            _session = session;
            _bridge = bridge;
            _playerId = ResolvePlayerId(session, bridge);
            _loversDrawerId = drawerId;
            Show(Modal.LoversChoice);

            if (Lbl("lovers-choice-sub") != null)
                Lbl("lovers-choice-sub")!.text =
                    $"Reward for {PlayerUiNames.ShortName(drawerId)}";

            var host = El("lovers-reagent-buttons");
            if (host == null) return;
            FateDecisionBindings.PopulateReagentButtons(host, type =>
            {
                if (_bridge != null && _playerId >= 0
                    && _bridge.TrySubmit(new FateLoversChoiceCommand(_playerId, false, type)))
                    OnFateDecisionCompleted?.Invoke();
            });
        }

        void RefreshMoonGiftUi()
        {
            if (_session == null || _playerId < 0) return;
            var cardHost = El("moon-cards");
            var reagentHost = El("moon-reagents");
            if (cardHost != null)
                FateDecisionBindings.PopulateMoonGiftCards(
                    cardHost, _session, _playerId, _moonGiftCards, RefreshMoonGiftUi);
            if (reagentHost != null)
                FateDecisionBindings.PopulateMoonGiftReagents(
                    reagentHost, _moonGiftReagents, _playerId, _session, RefreshMoonGiftUi);

            int reagentTotal = 0;
            foreach (var kv in _moonGiftReagents)
                reagentTotal += kv.Value;

            int total = _moonGiftCards.Count + reagentTotal;
            if (Lbl("moon-count") != null)
                Lbl("moon-count")!.text =
                    $"Gift to {PlayerUiNames.ShortName(_moonGiftRecipientId)}: {total} item{(total == 1 ? "" : "s")} selected";
            Btn("moon-confirm")?.SetEnabled(total >= 1);
        }

        void RefreshFoolAltarUi()
        {
            if (_session == null || _playerId < 0) return;
            var host = El("fool-altar-cards");
            if (host == null) return;
            FateDecisionBindings.PopulateAlignmentCards(
                host, _session, _playerId, _foolAltarAlignment, RefreshFoolAltarUi);
            Btn("fool-altar-confirm")?.SetEnabled(_foolAltarAlignment.Count > 0);
        }

        void OnMoonGiftConfirm()
        {
            if (_bridge == null || _playerId < 0 || _moonGiftRecipientId < 0) return;
            int reagentTotal = 0;
            foreach (var kv in _moonGiftReagents)
                reagentTotal += kv.Value;
            if (_moonGiftCards.Count == 0 && reagentTotal == 0) return;

            var cmd = new FateMoonGiftCommand(
                _playerId, _moonGiftRecipientId,
                new List<string>(_moonGiftCards),
                new Dictionary<ReagentType, int>(_moonGiftReagents));
            if (_bridge.TrySubmit(cmd))
            {
                _moonGiftCards.Clear();
                _moonGiftReagents.Clear();
                OnFateDecisionCompleted?.Invoke();
            }
        }

        void OnFoolAltarConfirm()
        {
            if (_bridge == null || _playerId < 0 || _foolAltarSlotIndex < 0) return;
            if (_foolAltarAlignment.Count == 0) return;

            var cmd = new ClaimFoolAltarCrucibleCommand(
                _playerId, _foolAltarSlotIndex, new List<string>(_foolAltarAlignment));
            if (_bridge.TrySubmit(cmd))
            {
                _foolAltarAlignment.Clear();
                OnFateDecisionCompleted?.Invoke();
            }
        }

        void OnLoversDraw()
        {
            if (_bridge == null || _playerId < 0) return;
            if (_bridge.TrySubmit(new FateLoversChoiceCommand(_playerId, true)))
                OnFateDecisionCompleted?.Invoke();
        }

        static int ResolvePlayerId(GameSession session, CommandBridge bridge)
        {
            if (bridge.PendingController != null)
                return bridge.PendingController.Slot.Index;
            if (bridge.ActivePlayerId >= 0)
                return bridge.ActivePlayerId;
            return session.Players.Count > 0 ? session.Players[0].PlayerId : 0;
        }

        void RebuildAdeptPaymentUi()
        {
            if (Root == null || _session == null || _playerId < 0) return;
            var modal = El("adept-modal");
            if (modal == null) return;

            modal.Q("adept-payment-host")?.RemoveFromHierarchy();

            var body = modal.Q(className: "card-modal__body");
            if (body == null) return;

            var host = new VisualElement { name = "adept-payment-host" };
            host.AddToClassList("adept-payment");

            var eyebrow = new Label("payment — select 3 cards");
            eyebrow.AddToClassList("eyebrow");
            host.Add(eyebrow);

            var chips = new VisualElement();
            chips.AddToClassList("adept-payment__chips");

            var player = _session.Players[_playerId];
            var db = _session.Rules!.CardDatabase;

            foreach (var id in player.Spread)
                AddPaymentChip(chips, id, db);
            foreach (var id in player.Hand)
                AddPaymentChip(chips, id, db);

            host.Add(chips);

            int adeptCount = CountAdeptsInArcanum();
            if (adeptCount >= 2)
            {
                host.Add(new Label("Arcanum full — tap an Adept to swap out")
                {
                    style =
                    {
                        fontSize = 9,
                        marginTop = 6,
                        whiteSpace = WhiteSpace.Normal
                    }
                });
                var swapHost = new VisualElement();
                swapHost.AddToClassList("adept-payment__swap");
                foreach (var aid in player.Arcanum)
                {
                    var adef = db.GetById(_session.GetCard(aid)?.DefinitionId ?? "");
                    if (adef?.MajorArcanaType != MajorArcanaType.Adept) continue;
                    bool sel = _swapOutAdeptId == aid;
                    string captured = aid;
                    var btn = new Button(() =>
                    {
                        _swapOutAdeptId = _swapOutAdeptId == captured ? null : captured;
                        RebuildAdeptPaymentUi();
                        RefreshAdeptButtons();
                    }) { text = adef.EffectText.Split('\n')[0] };
                    btn.AddToClassList("btn");
                    btn.AddToClassList("btn--secondary");
                    btn.style.whiteSpace = WhiteSpace.Normal;
                    btn.EnableInClassList("btn--primary", sel);
                    swapHost.Add(btn);
                }
                host.Add(swapHost);
            }

            var actionsRow = El("adept-actions");
            if (actionsRow != null && actionsRow.parent == body)
                body.Insert(body.IndexOf(actionsRow), host);
            else
                body.Add(host);
        }

        void AddPaymentChip(VisualElement chips, string id, ICardDatabase db)
        {
            var inst = _session!.GetCard(id);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null || def.IsMajorArcana) return;

            bool sel = _payment.Contains(id);
                var chip = CardChipFactory.CreateFromDefinition(def, selected: sel);
            string captured = id;
            chip.RegisterCallback<ClickEvent>(_ =>
            {
                if (_payment.Contains(captured))
                    _payment.Remove(captured);
                else if (_payment.Count < 3)
                    _payment.Add(captured);
                RebuildAdeptPaymentUi();
                RefreshAdeptButtons();
            });
            chips.Add(chip);
        }

        int CountAdeptsInArcanum()
        {
            if (_session == null || _playerId < 0) return 0;
            var db = _session.Rules?.CardDatabase;
            int count = 0;
            foreach (var aid in _session.Players[_playerId].Arcanum)
            {
                var adef = db?.GetById(_session.GetCard(aid)?.DefinitionId ?? "");
                if (adef?.MajorArcanaType == MajorArcanaType.Adept) count++;
            }
            return count;
        }

        void RefreshAdeptButtons()
        {
            if (_session == null || _playerId < 0) return;

            bool arcanumFull = CountAdeptsInArcanum() >= 2;
            bool canPlace = _payment.Count == 3
                && !string.IsNullOrEmpty(_adeptCardId)
                && (!arcanumFull || _swapOutAdeptId != null);

            Btn("adept-place")?.SetEnabled(canPlace);
            Btn("adept-hold")?.SetEnabled(!string.IsNullOrEmpty(_adeptCardId));
        }

        void OnAdeptPlace()
        {
            if (_bridge == null || _playerId < 0 || string.IsNullOrEmpty(_adeptCardId) || _payment.Count != 3)
                return;

            var payment = new List<string>(_payment);
            string? swap = CountAdeptsInArcanum() >= 2 ? _swapOutAdeptId : null;

            if (_bridge.TrySubmit(new BuyAdeptCommand(_playerId, _adeptCardId, payment, swap)))
                OnAdeptCompleted?.Invoke();
        }

        void OnAdeptHold()
        {
            if (_bridge == null || _playerId < 0 || string.IsNullOrEmpty(_adeptCardId)) return;
            if (_bridge.TrySubmit(new DeclineAdeptCommand(_playerId, _adeptCardId)))
                OnAdeptCompleted?.Invoke();
        }
    }
}
