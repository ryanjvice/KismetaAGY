using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
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

        protected override void Wire()
        {
            var doneBtn = Btn("unlock-done-btn");
            if (doneBtn != null)
                doneBtn.clicked += () => OnDone?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = bridge.ActivePlayerId;
            if (Root == null) return;

            var player = session.Players[_playerId];
            var spread = CollectMinor(session, player.Spread);
            var hand = CollectMinor(session, player.Hand);

            TapSwapBindings.RebuildZones(Root, session, spread, hand,
                session.Board.CosmicAgeSign, OnTapMove);
        }

        static List<string> CollectMinor(GameSession session, IEnumerable<string> ids)
        {
            var list = new List<string>();
            foreach (var id in ids)
                if (TapSwapBindings.IsMinorArcana(session, id)) list.Add(id);
            return list;
        }

        void OnTapMove(string cardId, bool fromSpread)
        {
            if (_bridge == null) return;
            _bridge.TrySubmit(new WinterMoveCardCommand(_playerId, cardId, toSpread: !fromSpread));
        }
    }
}
