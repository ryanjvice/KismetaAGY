using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CraftReagentController : OverlayController
    {
        const int DefaultNeed = 3;

        readonly HashSet<string> _selected = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        string _reagentKey = "salt";
        int _effectiveNeed = DefaultNeed;

        public System.Action OnBack;
        public System.Action OnDone;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("forge-btn")!.clicked += OnForge;
            Btn("result-done-btn")!.clicked += () => OnDone?.Invoke();

            foreach (var key in new[] { "sulphur", "vitriol", "aqua", "salt" })
            {
                var k = key;
                Btn($"pick-{k}")?.RegisterCallback<ClickEvent>(_ => PickReagent(k));
            }
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionHelpers.ResolvePlayerId(session, bridge);
            if (_playerId < 0 || Root == null) return;

            var player = session.Players[_playerId];
            LockReagentPicks(player);
            UpdateCauldronNote();
            RefreshPool();
            RefreshForgeBtn();
        }

        void LockReagentPicks(PlayerState player)
        {
            foreach (var key in new[] { "sulphur", "vitriol", "aqua", "salt" })
            {
                var btn = Btn($"pick-{key}");
                if (btn == null) continue;

                bool locked = key != "salt" && !IsElementalAvailable(player, key);
                btn.EnableInClassList("reagent-pick--locked", locked);
                btn.EnableInClassList("reagent-pick--active", key == _reagentKey && !locked);
                btn.SetEnabled(!locked || key == _reagentKey);
            }

            if (_reagentKey != "salt" && !IsElementalAvailable(player, _reagentKey))
                PickReagent("salt");
        }

        static bool IsElementalAvailable(PlayerState player, string key)
        {
            var type = SummerActionHelpers.KeyToReagent(key);
            if (type == ReagentType.Salt) return true;
            var suit = Correspondence.SuitFor(type);
            return suit != Suit.None && player.IsCauldronLit(suit);
        }

        void PickReagent(string key)
        {
            _reagentKey = key;
            _selected.Clear();
            _effectiveNeed = ComputeEffectiveNeed();
            if (_session != null)
                LockReagentPicks(_session.Players[_playerId]);
            RefreshPool();
            RefreshForgeBtn();
            UpdateCauldronNote();
        }

        int ComputeEffectiveNeed()
        {
            if (_session == null) return DefaultNeed;
            var player = _session.Players[_playerId];
            var cos = _session.Board.CosmicEffect;
            var personal = player.PersonalCosmicEffects;
            var type = SummerActionHelpers.KeyToReagent(_reagentKey);

            if (type == ReagentType.Salt && (cos.SaltCostsTwo || personal.SaltCostsTwo))
                return 2;
            var suit = Correspondence.SuitFor(type);
            if (type != ReagentType.Salt
                && ((cos.CheapCraftReagent == type && cos.CheapCraftSuit == suit)
                    || (personal.CheapCraftReagent == type && personal.CheapCraftSuit == suit)))
                return 2;
            return DefaultNeed;
        }

        void UpdateCauldronNote()
        {
            var note = Lbl("cauldron-note");
            if (note == null) return;

            if (_reagentKey == "salt")
            {
                note.text = "Salt — discard any 3 matching minors from hand or spread.";
                return;
            }

            var type = SummerActionHelpers.KeyToReagent(_reagentKey);
            var suit = Correspondence.SuitFor(type);
            note.text = $"{type} needs the lit {suit} cauldron — discard 3 {suit} cards.";
        }

        void RefreshPool()
        {
            if (_session == null || Root == null) return;
            var player = _session.Players[_playerId];
            var cards = SummerActionHelpers.CollectMinorCards(_session, player);
            Suit? filter = _reagentKey == "salt" ? null : Correspondence.SuitFor(SummerActionHelpers.KeyToReagent(_reagentKey));

            SummerCardPickBindings.RebuildPool(El("card-pool"), _session, cards, _selected, filter, id =>
            {
                if (_selected.Contains(id)) _selected.Remove(id);
                else if (_selected.Count < _effectiveNeed) _selected.Add(id);
                RefreshPool();
                RefreshForgeBtn();
            });

            var pickEyebrow = El("card-pool")?.parent?.Q<Label>(className: "eyebrow");
            if (pickEyebrow != null)
            {
                if (_reagentKey == "salt")
                    pickEyebrow.text = $"tap {_effectiveNeed} cards to discard";
                else
                    pickEyebrow.text = $"tap {_effectiveNeed} {filter} cards to discard";
            }
        }

        void RefreshForgeBtn()
        {
            var b = Btn("forge-btn");
            var count = Lbl("pick-count");
            if (b == null) return;

            int n = _selected.Count;
            if (count != null) count.text = $"{n} / {_effectiveNeed}";

            var contract = Lbl("contract-line");
            if (contract != null)
            {
                var type = SummerActionHelpers.KeyToReagent(_reagentKey);
                contract.text = _reagentKey == "salt"
                    ? $"{_effectiveNeed} cards → 1 Salt"
                    : $"{_effectiveNeed} cards → 1 {type}";
            }

            if (n >= _effectiveNeed)
            {
                b.RemoveFromClassList("btn--disabled");
                b.AddToClassList("btn--primary");
                b.text = "Forge the reagent";
            }
            else
            {
                b.AddToClassList("btn--disabled");
                b.RemoveFromClassList("btn--primary");
                b.text = $"Forge — needs {_effectiveNeed - n} more card{( _effectiveNeed - n == 1 ? "" : "s")}";
            }
        }

        void OnForge()
        {
            if (_bridge == null || _session == null || _playerId < 0) return;
            if (_selected.Count < _effectiveNeed) return;

            var type = SummerActionHelpers.KeyToReagent(_reagentKey);
            var cmd = new CraftReagentCommand(_playerId, type, new List<string>(_selected));
            if (!_bridge.TrySubmit(cmd)) return;

            El("forge-result")!.style.display = DisplayStyle.Flex;
        }
    }
}
