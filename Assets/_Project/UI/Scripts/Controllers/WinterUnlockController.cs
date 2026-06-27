using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
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
        bool _zonesBuilt;
        VisualElement? _builtForRoot;
        string _lastLayoutKey = "";

        protected override void Wire()
        {
            var doneBtn = Btn("unlock-done-btn");
            if (doneBtn != null)
                doneBtn.clicked += () => OnDone?.Invoke();
        }

        protected override void Unwire()
        {
            _zonesBuilt = false;
            _builtForRoot = null;
            _lastLayoutKey = "";
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

            RenderZonesIfNeeded(spread, hand);
            NarrativeSlotBindings.BindById(Root, "winter.unlock");
        }

        static List<string> CollectMinor(GameSession session, IEnumerable<string> ids)
        {
            var list = new List<string>();
            foreach (var id in ids)
                if (TapSwapBindings.IsMinorArcana(session, id)) list.Add(id);
            return list;
        }

        void RenderZonesIfNeeded(IReadOnlyList<string> spread, IReadOnlyList<string> hand)
        {
            if (Root == null || _session == null) return;

            var layoutKey = LayoutKey(spread, hand);
            if (_zonesBuilt && ReferenceEquals(Root, _builtForRoot) && layoutKey == _lastLayoutKey)
                return;

            TapSwapBindings.RebuildZones(Root, _session, spread, hand,
                _session.Board.CosmicAgeSign, OnTapMove);
            _zonesBuilt = true;
            _builtForRoot = Root;
            _lastLayoutKey = layoutKey;
        }

        static string LayoutKey(IReadOnlyList<string> spread, IReadOnlyList<string> hand)
            => string.Join(",", spread) + "|" + string.Join(",", hand);

        void OnTapMove(string cardId, bool fromSpread)
        {
            if (_bridge == null) return;
            _bridge.TrySubmit(new WinterMoveCardCommand(_playerId, cardId, toSpread: !fromSpread));
        }
    }
}
