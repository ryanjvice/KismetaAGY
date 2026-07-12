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
        bool _zonesBuilt;
        VisualElement? _builtForRoot;

        protected override void Wire()
        {
            var lockBtn = Btn("lock-btn");
            if (lockBtn != null)
                lockBtn.clicked += OnLock;
        }

        protected override void Unwire()
        {
            var lockBtn = Btn("lock-btn");
            if (lockBtn != null)
                lockBtn.clicked -= OnLock;

            _zonesBuilt = false;
            _builtForRoot = null;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;

            int pid = ResolvePlayerId(session, bridge);
            if (pid < 0) return;

            if (pid != _playerId)
            {
                _playerId = pid;
                _initialized = false;
                _zonesBuilt = false;
            }

            if (Root == null) return;

            if (!_initialized)
                SeedFromPlayer(session, pid);

            RenderZonesIfNeeded();
            UpdateLockButton();
        }

        static int ResolvePlayerId(GameSession session, CommandBridge bridge)
        {
            int pid = bridge.ActivePlayerId;
            if (pid >= 0 && pid < session.Players.Count)
                return pid;

            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            return -1;
        }

        void SeedFromPlayer(GameSession session, int playerId)
        {
            _spreadIds.Clear();
            _handIds.Clear();
            var player = session.Players[playerId];
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) _spreadIds.Add(id);
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) _handIds.Add(id);
            _initialized = true;
            _zonesBuilt = false;
        }

        void RenderZonesIfNeeded()
        {
            if (Root == null || _session == null) return;
            if (_zonesBuilt && ReferenceEquals(Root, _builtForRoot)) return;

            TapSwapBindings.RebuildZones(Root, _session, _spreadIds, _handIds,
                _session.Board.CosmicAgeSign, OnTapMove);
            _zonesBuilt = true;
            _builtForRoot = Root;
        }

        void OnTapMove(string cardId, bool fromSpread)
        {
            if (fromSpread)
            {
                if (!_spreadIds.Remove(cardId)) return;
                _handIds.Add(cardId);
            }
            else
            {
                if (!_handIds.Remove(cardId)) return;
                _spreadIds.Add(cardId);
            }

            if (_session != null && Root != null)
            {
                TapSwapBindings.RebuildZones(Root, _session, _spreadIds, _handIds,
                    _session.Board.CosmicAgeSign, OnTapMove);
                _zonesBuilt = true;
                _builtForRoot = Root;
                UpdateLockButton();
            }
        }

        void UpdateLockButton()
        {
            var lockBtn = Btn("lock-btn");
            if (lockBtn == null) return;
            int handLimit = ResolveHandLimit();
            bool valid = _handIds.Count <= handLimit;
            lockBtn.SetEnabled(valid);
            lockBtn.EnableInClassList("btn--disabled", !valid);
        }

        int ResolveHandLimit()
        {
            if (_session == null || _playerId < 0)
                return WinterRules.HandLimit;
            return PlayerLimitService.GetHandLimit(_session, _session.Players[_playerId]);
        }

        void OnLock()
        {
            if (_bridge == null || _playerId < 0 || _handIds.Count > ResolveHandLimit()) return;
            _bridge.TrySubmit(new CommuneCommand(_playerId, _spreadIds, _handIds));
        }
    }
}
