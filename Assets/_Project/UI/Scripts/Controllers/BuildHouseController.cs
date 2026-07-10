using System.Collections.Generic;
using System.Linq;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class BuildHouseController : OverlayController
    {
        readonly HashSet<string> _selected = new();
        string _lastPoolLayoutKey = "";

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action OnBack;
        public System.Action OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("build-btn")!.clicked += OnBuild;
        }

        protected override void Unwire()
        {
            _lastPoolLayoutKey = "";
        }

        protected override void Bind()
        {
            _selected.Clear();
            _lastPoolLayoutKey = "";
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            RefreshUi();
            NarrativeSlotBindings.BindById(Root, "summer.buildhouse");
        }

        int RequiredPaymentCount =>
            _session == null ? 2 : AstralHouseService.RequiredPaymentCount(_session.Mode);

        void RefreshUi()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var sign = player.CurrentSign;
            var planet = Correspondence.PlanetFor(sign);
            int required = RequiredPaymentCount;

            var eyebrow = Root.Q<Label>(className: "eyebrow");
            if (eyebrow != null)
                eyebrow.text = required == 1
                    ? $"cost — 1 {planet} card"
                    : $"cost — 2 {planet} cards";

            bool canBuild = CanBuild(player, sign, out string reason);
            var cards = CollectEligibleCards(player, planet);

            var layoutKey = BuildPoolLayoutKey(cards, _selected);
            if (layoutKey != _lastPoolLayoutKey)
            {
                _lastPoolLayoutKey = layoutKey;
                SummerCardPickBindings.RebuildPool(Root, _session, cards, _selected, null, OnCardToggle);
            }

            var btn = Btn("build-btn");
            if (btn != null)
            {
                bool hasRequiredCards = cards.Count >= required;
                bool readyToBuild = canBuild && hasRequiredCards && _selected.Count == required;
                btn.SetEnabled(readyToBuild);
                btn.EnableInClassList("btn--disabled", !readyToBuild);
                btn.text = !canBuild
                    ? reason
                    : !hasRequiredCards
                        ? $"Need {required} {planet} card{(required == 1 ? "" : "s")}"
                        : $"Raise the House on {sign}";
            }
        }

        bool CanBuild(PlayerState player, ZodiacSign sign, out string reason)
        {
            reason = "";
            if (player.UnplacedAstralHouses <= 0)
            {
                reason = "No Astral House tokens left";
                return false;
            }

            if (sign == ZodiacSign.None)
            {
                reason = "Roll your sign first";
                return false;
            }

            if (player.AstralHouses.Contains(sign))
            {
                reason = $"Already built on {sign}";
                return false;
            }

            foreach (var p in _session!.Players)
            {
                if (p.PlayerId != _playerId && p.AstralHouses.Contains(sign))
                {
                    reason = $"{sign} is claimed by a rival";
                    return false;
                }
            }

            return true;
        }

        List<string> CollectEligibleCards(PlayerState player, Planet planet)
        {
            var list = new List<string>();
            var db = _session!.Rules?.CardDatabase;
            if (db == null) return list;

            foreach (var id in player.Spread)
                TryAdd(id, list, db, planet);
            foreach (var id in player.Hand)
                TryAdd(id, list, db, planet);

            return list;
        }

        void TryAdd(string id, List<string> list, ICardDatabase db, Planet planet)
        {
            if (!TapSwapBindings.IsMinorArcana(_session!, id)) return;
            var inst = _session!.GetCard(id);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def != null && def.Planet == planet)
                list.Add(id);
        }

        static string BuildPoolLayoutKey(IReadOnlyList<string> cards, HashSet<string> selected)
        {
            var sortedCards = cards.OrderBy(id => id).ToList();
            var sortedSelected = selected.OrderBy(id => id).ToList();
            return string.Join(",", sortedCards) + "|" + string.Join(",", sortedSelected);
        }

        void OnCardToggle(string cardId)
        {
            if (_selected.Contains(cardId))
            {
                _selected.Remove(cardId);
            }
            else if (_selected.Count < RequiredPaymentCount)
            {
                _selected.Add(cardId);
            }
            else if (RequiredPaymentCount == 1)
            {
                _selected.Clear();
                _selected.Add(cardId);
            }

            RefreshUi();
        }

        void OnBuild()
        {
            int required = RequiredPaymentCount;
            if (_bridge == null || _session == null || _playerId < 0 || _selected.Count != required) return;
            var sign = _session.Players[_playerId].CurrentSign;
            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new BuildAstralHouseCommand(_playerId, sign, ids)))
                OnCompleted?.Invoke();
        }
    }
}
