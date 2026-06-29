using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class WinterUnlockController : ScreenController
    {
        public override string ScreenId => ScreenIds.WinterUnlock;

        public Action? OnDone;

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        WinterUnlockZoneBindings.ZoneBindState _unlockZones;

        protected override void Wire()
        {
            var doneBtn = Btn("unlock-done-btn");
            if (doneBtn != null)
                doneBtn.clicked += () => OnDone?.Invoke();
        }

        protected override void Unwire()
        {
            WinterUnlockZoneBindings.Reset(ref _unlockZones);
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = bridge.ActivePlayerId;
            if (Root == null) return;

            WinterUnlockZoneBindings.BindZones(
                ref _unlockZones, Root, session, _playerId, OnTapMove);
            NarrativeSlotBindings.BindById(Root, "winter.unlock");
        }

        void OnTapMove(string cardId, bool fromSpread)
        {
            if (_bridge == null) return;
            _bridge.TrySubmit(new WinterMoveCardCommand(_playerId, cardId, toSpread: !fromSpread));
        }
    }
}
