using System;
using Kismeta.Core.Domain;
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
    public sealed class AutumnSceneController : ScreenController
    {
        public override string ScreenId => ScreenIds.AutumnMain;

        public Action? OnFire;
        public Action? OnTemper;
        public Action? OnManageCards;
        public Action? OnPass;
        public Action? OnLeaveStasis;
        public Action? OnOppose;
        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        bool _inStasis;
        DockZone _dockZone = DockZone.Spread;
        AutumnOverlayHost? _autumnOverlays;
        ContestOverlayHost? _contestOverlays;

        public void ConfigureOverlays(AutumnOverlayHost? autumn, ContestOverlayHost? contest)
        {
            _autumnOverlays = autumn;
            _contestOverlays = contest;
        }

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
        }

        protected override void Wire()
        {
            WireBtn("fire-btn", OnFireClicked);
            WireBtn("temper-btn", OnTemperClicked);
            WireBtn("oppose-btn", OnOpposeClicked);
            WireBtn("manage-cards-btn", OnManageCardsClicked);
            WireBtn("pass-btn", OnPassClicked);
            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id => OnRivalSelected?.Invoke(id));
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

        void WireBtn(string name, Action handler)
        {
            var btn = Btn(name);
            if (btn == null)
            {
                Debug.LogWarning($"[UI] AutumnMain missing button '{name}'.");
                return;
            }
            btn.clicked += () => handler();
        }

        void OnFireClicked()
        {
            if (!CanAutumnAction()) return;
            if (_inStasis)
                OnLeaveStasis?.Invoke();
            else
                OnFire?.Invoke();
        }

        void OnTemperClicked()
        {
            if (CanAutumnAction()) OnTemper?.Invoke();
        }

        void OnManageCardsClicked()
        {
            if (CanAutumnAction()) OnManageCards?.Invoke();
        }

        void OnPassClicked()
        {
            if (CanAutumnAction()) OnPass?.Invoke();
        }

        void OnOpposeClicked()
        {
            if (CanAutumnAction()) OnOppose?.Invoke();
        }

        bool CanAutumnAction() =>
            _bridge != null && _bridge.CanSubmit && _bridge.PendingHint == ActionHint.AutumnAction;

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindPassButton(Root, session, bridge);
            RefreshActionGroupRail();

            var player = session.Players[_localPlayerId];
            bool autumnAction = bridge.CanSubmit && bridge.PendingHint == ActionHint.AutumnAction;
            _inStasis = player.StoneState == StoneState.Stasis;

            var fireBtn = Btn("fire-btn");
            if (fireBtn != null)
            {
                if (_inStasis)
                {
                    fireBtn.text = "Leave Stasis";
                    fireBtn.SetEnabled(autumnAction && AutumnActionBindings.CanLeaveStasis(player));
                }
                else
                {
                    fireBtn.text = "Fire";
                    fireBtn.SetEnabled(autumnAction && AutumnActionBindings.CanFire(session, player));
                }
            }

            Btn("temper-btn")?.SetEnabled(
                autumnAction && !_inStasis && AutumnActionBindings.CanTemper(session, player));
            Btn("oppose-btn")?.SetEnabled(
                autumnAction && !_inStasis && AutumnActionBindings.HasOpposeTargets(session, _localPlayerId));
            Btn("manage-cards-btn")?.SetEnabled(autumnAction);
            Btn("pass-btn")?.SetEnabled(autumnAction);

            CrucibleForgeBindings.ApplyForge(
                El("board-stage"),
                Lbl("stone-label"),
                player,
                AutumnActionBindings.StoneStatusLabel(player));
            CrucibleForgeBindings.ApplyAllPlayerStones(
                El("board-stage"), El("stasis-row"), session, _localPlayerId);
            CrucibleForgeBindings.ApplyCauldronReagents(El("cauldron-mini"), player);

            var stepId = NarrativeStepResolver.ResolveAutumnAction(
                player.StoneState, _autumnOverlays, _contestOverlays);
            NarrativeSlotBindings.BindById(Root, stepId);

            if (Lbl("hint-label") != null)
            {
                Lbl("hint-label")!.text = autumnAction
                    ? (_inStasis ? "In Stasis — pay Salt or wait" : "Your turn at the forge")
                    : "Waiting…";
            }

            RefreshDock();
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(
                El("step-rail"), 3,
                ActionGroupRailBindings.ResolveAutumnActiveGroup(_autumnOverlays, _contestOverlays));
        }
    }
}
