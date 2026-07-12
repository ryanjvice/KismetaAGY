using System;
using System.Collections.Generic;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;

namespace Kismeta.UI.Controllers
{
    public sealed class ActiveEffectsController : OverlayController
    {
        public Action? OnClose;
        public Action? OnEmperorActivate;

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _bindKey = int.MinValue;
        int _focusPlayerId = -1;
        int _localPlayerId = -1;
        IReadOnlyList<string>? _spreadOverride;

        public void SetFocus(int playerId) => _focusPlayerId = playerId;

        public void SetSpreadOverride(IReadOnlyList<string>? spreadIds) => _spreadOverride = spreadIds;

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
            _spreadOverride = null;
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
            int playerId = _focusPlayerId >= 0 ? _focusPlayerId : _localPlayerId;
            if (playerId < 0) return;

            int bindKey = ComputeBindKey(session, playerId, _spreadOverride);
            if (bindKey == _bindKey) return;
            _bindKey = bindKey;

            var snapshot = ActiveEffectsService.Build(session, playerId, _spreadOverride);
            string subtitle = snapshot.Subtitle;
            if (playerId != _localPlayerId && _localPlayerId >= 0)
                subtitle = $"{PlayerUiNames.ShortName(playerId)} · {subtitle}";

            Action<ActiveEffectItem>? onItemAction = null;
            if (playerId == _localPlayerId
                && EmperorActivationService.CanActivate(
                    session, playerId, bridge.PendingHint, bridge.CanSubmit))
            {
                onItemAction = item =>
                {
                    if (item.Action == ActiveEffectActionKind.ActivateEmperor)
                        OnEmperorActivate?.Invoke();
                };
            }

            ActiveEffectsRows.Populate(Root, snapshot, subtitle, onItemAction);
        }

        public void ForceRefresh()
        {
            _bindKey = int.MinValue;
            if (_session != null && _bridge != null)
                BindState(_session, _loop, _bridge);
        }

        static int ComputeBindKey(GameSession session, int playerId, IReadOnlyList<string>? spreadOverride)
        {
            var player = session.Players[playerId];
            var spreadSource = spreadOverride ?? player.Spread;
            int spreadHash = 0;
            foreach (var id in spreadSource)
                spreadHash = spreadHash * 31 + id.GetHashCode();

            int arcanumHash = 0;
            foreach (var id in player.Arcanum)
                arcanumHash = arcanumHash * 31 + id.GetHashCode();

            int emperorHash = 0;
            foreach (var id in player.EmperorProtectedCardIds)
                emperorHash = emperorHash * 31 + id.GetHashCode();

            return playerId * 10000
                   + (int)session.Board.CosmicAgeSign * 100
                   + player.UsedAdeptInstanceIdsThisAge.Count * 10
                   + spreadHash
                   + arcanumHash
                   + emperorHash;
        }
    }
}
