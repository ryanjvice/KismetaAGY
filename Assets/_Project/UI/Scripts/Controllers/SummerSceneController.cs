using System;
using System.Collections.Generic;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerSceneController : ScreenController
    {
        public override string ScreenId => ScreenIds.SummerMain;

        public Action? OnTrade;
        public Action? OnDuel;
        public Action? OnGambit;
        public Action? OnOpposition;
        public Action? OnPass;

        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        readonly HashSet<int> _expandedBlocks = new();
        DockZone _dockZone = DockZone.Spread;
        SummerOverlayHost? _summerOverlays;
        ContestOverlayHost? _contestOverlays;

        public void ConfigureOverlays(SummerOverlayHost? summer, ContestOverlayHost? contest)
        {
            _summerOverlays = summer;
            _contestOverlays = contest;
        }

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
        }

        protected override void Wire()
        {
            WireBtn("trade-btn", () => OnTrade?.Invoke());
            WireBtn("duel-btn", () => OnDuel?.Invoke());
            WireBtn("gambit-btn", () => OnGambit?.Invoke());
            WireBtn("opposition-btn", () => OnOpposition?.Invoke());
            WireBtn("pass-btn", () => OnPass?.Invoke());
            WireBtn("info-btn", () => SetInfoPopup(true));
            WireBtn("info-close-btn", () => SetInfoPopup(false));

            var scrim = El("info-scrim");
            if (scrim != null)
                scrim.RegisterCallback<ClickEvent>(_ => SetInfoPopup(false));

            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id =>
            {
                ExpandRival(id);
                RefreshRoster();
                OnRivalSelected?.Invoke(id);
            });
        }

        void WireBtn(string name, Action handler)
        {
            var btn = Btn(name);
            if (btn == null)
            {
                Debug.LogWarning($"[UI] SummerMain missing button '{name}'.");
                return;
            }
            btn.clicked += () => handler();
        }

        void OnHandToggle()
        {
            _dockZone = _dockZone == DockZone.Hand ? DockZone.Spread : DockZone.Hand;
            RefreshDock();
        }

        void OnArcanumToggle()
        {
            _dockZone = _dockZone == DockZone.Arcanum ? DockZone.Spread : DockZone.Arcanum;
            RefreshDock();
        }

        void RefreshDock()
        {
            if (_session != null)
                InventoryOverlayBindings.RefreshInventory(Root, _session, _localPlayerId, _dockZone, OnInspectCard);
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindPassButton(Root, session, bridge);
            RefreshActionGroupRail();
            RefreshDock();

            bool summerAction = bridge.CanSubmit && bridge.PendingHint == ActionHint.SummerAction;
            Btn("trade-btn")?.SetEnabled(summerAction);
            Btn("duel-btn")?.SetEnabled(summerAction);
            Btn("gambit-btn")?.SetEnabled(summerAction);
            Btn("opposition-btn")?.SetEnabled(
                summerAction && AutumnActionBindings.HasOpposeTargets(session, _localPlayerId));

            RefreshRoster();

            var stepId = NarrativeStepResolver.ResolveSummerAction(_summerOverlays, _contestOverlays);
            NarrativeSlotBindings.BindById(Root, stepId, mask: NarrativeSlotMask.Beat);

            var infoPopup = El("summer-info-popup");
            if (infoPopup != null)
            {
                NarrativeSlotBindings.BindById(
                    infoPopup,
                    stepId,
                    mask: NarrativeSlotMask.Stakes | NarrativeSlotMask.Charge);
            }

            HeaderOverlayBindings.ApplyHeaderPad(Root);
        }

        void SetInfoPopup(bool visible)
        {
            var popup = El("summer-info-popup");
            if (popup != null)
                popup.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshRoster()
        {
            if (Root == null || _session == null) return;
            SummerRosterBindings.Populate(
                Root,
                _session,
                _localPlayerId,
                _expandedBlocks,
                cardId => OnInspectCard?.Invoke(cardId),
                ToggleRivalDetail);
        }

        void ToggleRivalDetail(int playerId)
        {
            if (!_expandedBlocks.Add(playerId))
                _expandedBlocks.Remove(playerId);
            RefreshRoster();
        }

        public void ExpandRival(int playerId)
        {
            if (playerId >= 0)
                _expandedBlocks.Add(playerId);
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(
                El("step-rail"), 4,
                ActionGroupRailBindings.ResolveSummerActiveGroup(_summerOverlays, _contestOverlays));
        }
    }
}
