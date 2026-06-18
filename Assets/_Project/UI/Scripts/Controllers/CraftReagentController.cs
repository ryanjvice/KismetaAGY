using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CraftReagentController : OverlayController
    {
        readonly HashSet<string> _selected = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        ReagentType _reagent = ReagentType.Sulphur;
        bool _forged;

        public System.Action OnBack;
        public System.Action OnDone;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("forge-btn")!.clicked += OnForge;
            Btn("result-done-btn")!.clicked += () => OnDone?.Invoke();

            foreach (var key in ReagentKeys)
                Btn($"pick-{key}")?.RegisterCallback<ClickEvent>(_ => PickReagent(key));
        }

        static readonly string[] ReagentKeys = { "salt", "sulphur", "vitriol", "aqua", "quicksilver" };

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            if (_forged)
            {
                El("forge-result")!.style.display = DisplayStyle.Flex;
                return;
            }

            El("forge-result")!.style.display = DisplayStyle.None;
            _selected.Clear();
            _reagent = ReagentType.Sulphur;

            var player = session.Players[_playerId];
            LockReagentPicks(player);
            PickReagent("sulphur");
        }

        void LockReagentPicks(PlayerState player)
        {
            foreach (var key in ReagentKeys)
            {
                var btn = Btn($"pick-{key}");
                if (btn == null) continue;

                var type = KeyToReagent(key);
                bool locked = type != ReagentType.Salt
                    && !player.IsCauldronLit(Correspondence.SuitFor(type));
                btn.EnableInClassList("reagent-pick--locked", locked);
                btn.SetEnabled(!locked);
            }
        }

        void PickReagent(string key)
        {
            var btn = Btn($"pick-{key}");
            if (btn != null && btn.ClassListContains("reagent-pick--locked")) return;

            _reagent = KeyToReagent(key);
            _selected.Clear();

            foreach (var k in ReagentKeys)
                Btn($"pick-{k}")?.EnableInClassList("reagent-pick--active", k == key);

            UpdateCauldronNote();
            RefreshPool();
            RefreshForgeBtn();
        }

        void UpdateCauldronNote()
        {
            var note = Lbl("cauldron-note");
            if (note == null || _session == null || _playerId < 0) return;

            if (_reagent == ReagentType.Salt)
            {
                note.text = "Salt accepts any 3 cards from your spread or hand.";
                return;
            }

            var suit = Correspondence.SuitFor(_reagent);
            var player = _session.Players[_playerId];
            var color = CauldronNameFor(suit);
            int need = EffectiveNeed();
            bool lit = player.IsCauldronLit(suit);
            note.text = lit
                ? $"{_reagent} needs the lit {color} cauldron — discard {need} {suit}."
                : $"The {color} cauldron must be lit before crafting {_reagent}.";
        }

        void RefreshPool()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var cards = SummerCardPickBindings.CollectMinorCards(_session, player);
            Suit? filter = _reagent == ReagentType.Salt ? null : Correspondence.SuitFor(_reagent);

            int need = EffectiveNeed();
            var pool = El("card-pool");
            var eyebrow = pool?.parent?.Q<Label>(className: "eyebrow");
            if (eyebrow != null)
            {
                if (filter.HasValue)
                    eyebrow.text = $"tap {need} {filter.Value} cards to discard";
                else
                    eyebrow.text = $"tap {need} cards to discard";
            }

            SummerCardPickBindings.RebuildPool(Root, _session, cards, _selected, filter, OnCardToggle);
            RefreshForgeBtn();
        }

        void OnCardToggle(string cardId)
        {
            if (_session == null) return;
            int need = EffectiveNeed();

            if (_selected.Contains(cardId))
                _selected.Remove(cardId);
            else if (_selected.Count < need)
                _selected.Add(cardId);

            RefreshPool();
        }

        int EffectiveNeed()
        {
            if (_session == null || _playerId < 0) return 3;
            return SummerActionBindings.CraftEffectiveCost(_session, _session.Players[_playerId], _reagent);
        }

        void RefreshForgeBtn()
        {
            var b = Btn("forge-btn");
            var countLbl = Lbl("pick-count");
            if (b == null) return;

            int need = EffectiveNeed();
            int picked = _selected.Count;
            if (countLbl != null) countLbl.text = $"{picked} / {need}";

            var contract = Lbl("contract-line");
            if (contract != null)
            {
                if (_reagent == ReagentType.Salt)
                    contract.text = $"{need} cards → 1 Salt";
                else
                {
                    var suit = Correspondence.SuitFor(_reagent);
                    var color = CauldronNameFor(suit);
                    contract.text = $"{need} {suit} → 1 {_reagent} into the {color} cauldron";
                }
            }

            bool ready = picked >= need;
            b.EnableInClassList("btn--disabled", !ready);
            b.EnableInClassList("btn--primary", ready);
            b.text = ready
                ? "Forge the reagent"
                : $"Forge — needs {need - picked} more card{(need - picked == 1 ? "" : "s")}";
        }

        void OnForge()
        {
            if (_bridge == null || _session == null || _playerId < 0) return;
            if (_selected.Count < EffectiveNeed()) return;

            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new CraftReagentCommand(_playerId, _reagent, ids)))
            {
                _forged = true;
                El("forge-result")!.style.display = DisplayStyle.Flex;
            }
        }

        public void ResetForgeState() => _forged = false;

        static ReagentType KeyToReagent(string key) => key switch
        {
            "sulphur" => ReagentType.Sulphur,
            "vitriol" => ReagentType.Vitriol,
            "aqua" => ReagentType.AquaRegia,
            "quicksilver" => ReagentType.Quicksilver,
            _ => ReagentType.Salt
        };

        static string CauldronNameFor(Suit suit) => suit switch
        {
            Suit.Wands => "Red",
            Suit.Cups => "Blue",
            Suit.Pentacles => "Green",
            Suit.Swords => "Yellow",
            _ => ""
        };

    }
}
