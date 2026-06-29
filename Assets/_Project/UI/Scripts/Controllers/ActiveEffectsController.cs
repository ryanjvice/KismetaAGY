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
        int _focusPlayerId = -1;
        int _localPlayerId = -1;

        public void SetFocus(int playerId) => _focusPlayerId = playerId;

        protected override void Wire()
        {
            Btn("active-effects-close")!.clicked += () => OnClose?.Invoke();
            ApplyHostLayout(tall: true);
        }

        protected override void Unwire()
        {
            ApplyHostLayout(tall: false);
            _bindKey = int.MinValue;
            _focusPlayerId = -1;
        }

        void ApplyHostLayout(bool tall)
        {
            Root?.parent?.EnableInClassList("overlay-clone-host--active-effects", tall);
            Root?.parent?.parent?.EnableInClassList("overlay-layer--active-effects", tall);
        }

        public void BindState(GameSession session, GameLoop? loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            if (Root == null) return;

            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            int playerId = _focusPlayerId >= 0 ? _focusPlayerId : _localPlayerId;
            if (playerId < 0) return;

            int bindKey = ComputeBindKey(session, playerId);
            if (bindKey == _bindKey) return;
            _bindKey = bindKey;

            var snapshot = ActiveEffectsService.Build(session, playerId);
            string subtitle = snapshot.Subtitle;
            if (playerId != _localPlayerId && _localPlayerId >= 0)
                subtitle = $"{PlayerUiNames.ShortName(playerId)} · {subtitle}";

            ActiveEffectsRows.Populate(Root, snapshot, subtitle);
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
