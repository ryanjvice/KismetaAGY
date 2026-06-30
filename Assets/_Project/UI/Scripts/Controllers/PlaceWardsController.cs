using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class PlaceWardsController : OverlayController
    {
        readonly ReagentStepper _stepper = new() { Cap = 1 };
        readonly Dictionary<string, string> _pendingKeys = new();

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action OnBack;
        public System.Action OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("seal-btn")!.clicked += OnSeal;
        }

        public void BindState(GameSession session, CommandBridge bridge) =>
            BindState(session, null, bridge);

        public void BindState(GameSession session, GameLoop? loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
            _playerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null || _playerId < 0) return;

            _stepper.Reset();
            _pendingKeys.Clear();
            RebuildRows();
            NarrativeSlotBindings.BindById(Root, "summer.wards");
        }

        void RebuildRows()
        {
            if (_session == null || _playerId < 0 || Root == null || _bridge == null) return;

            var player = _session.Players[_playerId];
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
                _stepper.SetHave(rt.ToString(), player.GetReagent(rt));

            UpdateSupplyLabel();

            bool canPlaceCrucibleOrAdept = CanPlaceCrucibleOrAdept();
            bool canPlaceForge = CanPlaceForge();

            ProtectiveWardRows.Populate(
                Root,
                _session,
                _playerId,
                canPlaceCrucibleOrAdept,
                canPlaceForge,
                Adjust);

            RefreshSealBtn();
        }

        bool CanPlaceCrucibleOrAdept() =>
            _bridge != null
            && _bridge.CanSubmit
            && _bridge.PendingHint == ActionHint.SummerAction
            && _session != null
            && _session.Phase.CurrentSeason == Season.Summer;

        bool CanPlaceForge() =>
            _bridge != null
            && _bridge.CanSubmit
            && _bridge.PendingHint == ActionHint.AutumnAction
            && _session != null
            && _session.Phase.CurrentSeason == Season.Autumn
            && _session.Players[_playerId].StoneState == StoneState.Forging;

        void Adjust(string key, int delta)
        {
            var valLbl = Lbl($"ward-{key}-val");
            if (valLbl == null) return;

            int current = int.TryParse(valLbl.text, out var v) ? v : 0;

            if (delta > 0)
            {
                if (_stepper.Total >= _stepper.Cap) return;
                if (!_stepper.TryAdd(key)) return;
                _pendingKeys[key] = key;
                valLbl.text = (current + 1).ToString();
            }
            else
            {
                if (current <= 0) return;
                _stepper.TryRemove(key);
                _pendingKeys.Remove(key);
                valLbl.text = (current - 1).ToString();
            }

            UpdateSupplyLabel();
            RefreshSealBtn();
        }

        void UpdateSupplyLabel()
        {
            if (_session == null || _playerId < 0 || Lbl("ward-supply") == null) return;
            var player = _session.Players[_playerId];
            int remaining = 0;
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
                remaining += player.GetReagent(rt);
            remaining -= _stepper.Total;
            Lbl("ward-supply")!.text = $"{remaining} left";
        }

        void RefreshSealBtn()
        {
            var btn = Btn("seal-btn");
            if (btn == null) return;
            bool ready = _stepper.Total == 1;
            btn.SetEnabled(ready);
            btn.EnableInClassList("btn--disabled", !ready);
        }

        void OnSeal()
        {
            if (_bridge == null || _session == null || _playerId < 0 || _stepper.Total != 1) return;

            string? pendingKey = null;
            foreach (var kv in _pendingKeys)
            {
                pendingKey = kv.Key;
                break;
            }
            if (pendingKey == null) return;

            var player = _session.Players[_playerId];
            ReagentType rt = PickDefaultReagent(player);

            bool submitted = pendingKey switch
            {
                var k when k.StartsWith("crucible-") =>
                    int.TryParse(k["crucible-".Length..], out var slotIndex)
                    && _bridge.TrySubmit(new PlaceCardWardCommand(_playerId, slotIndex, rt)),
                var k when k.StartsWith("adept-") =>
                    _bridge.TrySubmit(new PlaceAdeptWardCommand(_playerId, k["adept-".Length..], rt)),
                "stone" => _bridge.TrySubmit(new PlaceStoneWardCommand(_playerId, rt)),
                _ => false
            };

            if (submitted)
            {
                _stepper.Reset();
                _pendingKeys.Clear();
                RebuildRows();
                OnCompleted?.Invoke();
            }
        }

        static ReagentType PickDefaultReagent(PlayerState player)
        {
            foreach (ReagentType type in System.Enum.GetValues(typeof(ReagentType)))
            {
                if (player.GetReagent(type) > 0)
                    return type;
            }
            return ReagentType.Salt;
        }
    }
}
