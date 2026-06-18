using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CommuneController : ScreenController
    {
        public override string ScreenId => ScreenIds.Commune;

        readonly List<string> _spreadIds = new();
        readonly List<string> _handIds = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        bool _initialized;

        protected override void Wire()
        {
            var lockBtn = Btn("lock-btn");
            if (lockBtn != null)
                lockBtn.clicked += OnLock;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            int pid = bridge.ActivePlayerId;
            if (pid != _playerId)
            {
                _playerId = pid;
                _initialized = false;
            }

            if (Root == null) return;

            if (!_initialized)
                SeedFromPlayer(session, pid);

            TapSwapBindings.RebuildZones(Root, session, _spreadIds, _handIds,
                session.Board.CosmicAgeSign, OnTapMove);
            UpdateLockButton();
        }

        void SeedFromPlayer(GameSession session, int playerId)
        {
            _spreadIds.Clear();
            _handIds.Clear();
            var player = session.Players[playerId];
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) _spreadIds.Add(id);
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) _spreadIds.Add(id);
            _initialized = true;
        }

        void OnTapMove(string cardId, bool fromSpread)
        {
            if (fromSpread)
            {
                _spreadIds.Remove(cardId);
                _handIds.Add(cardId);
            }
            else
            {
                _handIds.Remove(cardId);
                _spreadIds.Add(cardId);
            }

            if (_session != null && Root != null)
            {
                TapSwapBindings.RebuildZones(Root, _session, _spreadIds, _handIds,
                    _session.Board.CosmicAgeSign, OnTapMove);
                UpdateLockButton();
            }
        }

        void UpdateLockButton()
        {
            var lockBtn = Btn("lock-btn");
            if (lockBtn == null) return;
            bool valid = _handIds.Count <= WinterRules.HandLimit;
            lockBtn.SetEnabled(valid);
            lockBtn.EnableInClassList("btn--disabled", !valid);
        }

        void OnLock()
        {
            if (_bridge == null || _handIds.Count > WinterRules.HandLimit) return;
            _bridge.TrySubmit(new CommuneCommand(_playerId, _spreadIds, _handIds));
            _initialized = false;
        }
    }
}
