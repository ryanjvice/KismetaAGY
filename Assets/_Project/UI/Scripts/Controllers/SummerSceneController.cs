using System;
using System.Collections.Generic;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
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

        // Top contest buttons (open target-selecting overlays).
        public Action? OnTrade;
        public Action? OnDuel;
        public Action? OnGambit;
        public Action? OnOpposition;
        public Action? OnPass;

        // Per-rival roster shortcuts.
        public Action<int>? OnTradeRival;
        public Action<int>? OnDuelRival;
        public Action<int>? OnGambitRival;
        public Action<int>? OnOpposeRival;

        // Inventory / table.
        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;
        CardTableBindings.SortMode _sort = CardTableBindings.SortMode.Threat;
        readonly HashSet<int> _expandedBlocks = new();
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

            HookSort("sort-threat", CardTableBindings.SortMode.Threat);
            HookSort("sort-turn", CardTableBindings.SortMode.Turn);
            HookSort("sort-arcanum", CardTableBindings.SortMode.Arcanum);

            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id => OnRivalSelected?.Invoke(id));
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

        void HookSort(string btnName, CardTableBindings.SortMode mode)
        {
            Btn(btnName)?.RegisterCallback<ClickEvent>(_ =>
            {
                _sort = mode;
                foreach (var n in new[] { "sort-threat", "sort-turn", "sort-arcanum" })
                    Btn(n)?.EnableInClassList("table-action--active", n == btnName);
                RefreshRoster();
            });
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

        void ToggleDetail(int playerId)
        {
            if (!_expandedBlocks.Add(playerId))
                _expandedBlocks.Remove(playerId);
            RefreshRoster();
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

            Btn("sort-threat")?.EnableInClassList("table-action--active", _sort == CardTableBindings.SortMode.Threat);
            RefreshRoster();

            var stepId = NarrativeStepResolver.ResolveSummerAction(_summerOverlays, _contestOverlays);
            NarrativeSlotBindings.BindById(
                Root,
                stepId,
                mask: NarrativeSlotMask.Beat | NarrativeSlotMask.Stakes | NarrativeSlotMask.Charge);

            HeaderOverlayBindings.ApplyHeaderPad(Root);
        }

        void RefreshRoster()
        {
            if (Root == null || _session == null) return;
            CardTableBindings.Populate(
                Root, _session, _localPlayerId, _session.Phase.CurrentSeason, _sort,
                _expandedBlocks, focusPlayerId: -1,
                onInspect: OnInspectCard,
                onDuel: id => OnDuelRival?.Invoke(id),
                onGambit: id => OnGambitRival?.Invoke(id),
                onTrade: id => OnTradeRival?.Invoke(id),
                onToggleDetail: ToggleDetail,
                onOppose: id => OnOpposeRival?.Invoke(id));
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(
                El("step-rail"), 4,
                ActionGroupRailBindings.ResolveSummerActiveGroup(_summerOverlays, _contestOverlays));
        }
    }
}
