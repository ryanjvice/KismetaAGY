using System;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;

namespace Kismeta.UI.Controllers
{
    public sealed class ActiveEffectsController : OverlayController
    {
        public Action? OnClose;

        GameSession? _session;
        CommandBridge? _bridge;
        int _bindKey = int.MinValue;

        protected override void Wire()
        {
            Btn("active-effects-close")!.clicked += () => OnClose?.Invoke();
            ApplyHostLayout(tall: true);
        }

        protected override void Unwire()
        {
            ApplyHostLayout(tall: false);
            _bindKey = int.MinValue;
        }

        void ApplyHostLayout(bool tall) =>
            Root?.parent?.EnableInClassList("overlay-clone-host--active-effects", tall);

        public void BindState(GameSession session, GameLoop? loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            if (Root == null) return;

            int playerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (playerId < 0) return;

            int bindKey = ComputeBindKey(session, playerId);
            if (bindKey == _bindKey) return;
            _bindKey = bindKey;

            var snapshot = ActiveEffectsService.Build(session, playerId);
            ActiveEffectsRows.Populate(Root, snapshot);
        }

        static int ComputeBindKey(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            int spreadHash = 0;
            foreach (var id in player.Spread)
                spreadHash = spreadHash * 31 + id.GetHashCode();

            int arcanumHash = 0;
            foreach (var id in player.Arcanum)
                arcanumHash = arcanumHash * 31 + id.GetHashCode();

            return playerId * 10000
                   + (int)session.Board.CosmicAgeSign * 100
                   + player.UsedAdeptInstanceIdsThisAge.Count * 10
                   + spreadHash
                   + arcanumHash;
        }
    }
}
