using System;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;

namespace Kismeta.UI.Controllers
{
    public sealed class CrucibleCodexReferenceController : OverlayController
    {
        public Action? OnClose;

        GameSession? _session;
        CommandBridge? _bridge;
        GameLoop? _loop;
        int _bindKey = int.MinValue;
        int _localPlayerId = -1;

        protected override void Wire()
        {
            Btn("crucible-codex-close")!.clicked += () => OnClose?.Invoke();
            ApplyHostLayout(tall: true);
        }

        protected override void Unwire()
        {
            ApplyHostLayout(tall: false);
            _bindKey = int.MinValue;
            _localPlayerId = -1;
        }

        void ApplyHostLayout(bool tall)
        {
            Root?.parent?.EnableInClassList("overlay-clone-host--active-effects", tall);
            Root?.parent?.parent?.EnableInClassList("overlay-layer--active-effects", tall);
        }

        public void BindState(GameSession session, GameLoop? loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
            if (Root == null) return;

            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (_localPlayerId < 0) return;

            int bindKey = ComputeBindKey(session, _localPlayerId);
            if (bindKey == _bindKey) return;
            _bindKey = bindKey;

            CrucibleCodexRows.Populate(Root, session, _localPlayerId);
        }

        static int ComputeBindKey(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            int spreadHash = 0;
            foreach (var id in player.Spread)
                spreadHash = spreadHash * 31 + id.GetHashCode();

            int slotHash = 0;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                slotHash = slotHash * 31 + (int)slot.State;
                slotHash = slotHash * 31 + slot.CardInstanceId.GetHashCode();
            }

            return playerId * 10000
                   + (int)player.AssignedCodex * 100
                   + spreadHash
                   + slotHash;
        }
    }
}
