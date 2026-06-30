using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class WinterHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.WinterHub;

        public System.Action? OnOpenCraft;
        public System.Action? OnOpenWager;
        public System.Action? OnOpenCardTable;
        public System.Action? OnOpenActiveEffects;
        public System.Action? OnOpenCrucibleCodex;
        public System.Action? OnOpenProtectiveWards;
        public System.Action<string>? OnInspectCard;
        public System.Action<int>? OnRivalSelected;

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;
        WinterUnlockZoneBindings.ZoneBindState _unlockZones;

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
            WinterUnlockZoneBindings.Reset(ref _unlockZones);
        }

        protected override void Wire()
        {
            Btn("craft-btn")!.clicked += () => OnOpenCraft?.Invoke();
            Btn("wager-btn")!.clicked += () => OnOpenWager?.Invoke();
            Btn("pass-btn")!.clicked += OnPass;
            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke(),
                OnOpenCrucibleCodex = () => OnOpenCrucibleCodex?.Invoke(),
                OnOpenProtectiveWards = () => OnOpenProtectiveWards?.Invoke()
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

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            int resolvedPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (resolvedPlayerId != _localPlayerId)
                WinterUnlockZoneBindings.Reset(ref _unlockZones);
            _localPlayerId = resolvedPlayerId;
            if (Root == null) return;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindPassButton(Root, session, bridge);
            MainSceneBindings.BindStepRail(
                El("step-rail"), session.Phase.CurrentStepIndex, 4, "step__dot--active");

            RefreshDock();

            bool winterAction = bridge.PendingHint == ActionHint.WinterAction;
            BindUnlockZones(winterAction);
            BindWinterCta(session, bridge, winterAction);
            NarrativeSlotBindings.BindById(Root, "winter.unlock");
        }

        void BindUnlockZones(bool winterAction)
        {
            var unlockStage = El("unlock-stage");
            unlockStage?.EnableInClassList("winter-hub__unlock--hidden", !winterAction);

            if (!winterAction || _session == null || _bridge == null || unlockStage == null)
                return;

            WinterUnlockZoneBindings.BindZones(
                ref _unlockZones, unlockStage, _session, _localPlayerId, OnTapMove);
        }

        void OnTapMove(string cardId, bool fromSpread)
        {
            if (_bridge == null) return;
            _bridge.TrySubmit(new WinterMoveCardCommand(_localPlayerId, cardId, toSpread: !fromSpread));
        }

        void BindWinterCta(GameSession session, CommandBridge bridge, bool winterAction)
        {
            var player = session.Players[_localPlayerId];
            bool canWager = winterAction && player.FatefulWagerSign == ZodiacSign.None;

            SetCtaVisible("craft-btn", winterAction);
            SetCtaVisible("wager-btn", canWager);
            SetCtaVisible("limits-btn", false);
            SetCtaVisible("transit-btn", false);
        }

        void SetCtaVisible(string name, bool visible)
        {
            var btn = Btn(name);
            if (btn == null) return;
            btn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnPass() => _bridge?.SubmitPass();
    }
}
