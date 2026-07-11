using System.Collections.Generic;
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

        int StandardPaymentCount =>
            _session == null ? 2 : AstralHouseService.RequiredPaymentCount(_session.Mode);

        void RefreshUi()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var sign = player.CurrentSign;
            var planet = Correspondence.PlanetFor(sign);
            int standardRequired = StandardPaymentCount;
            bool entryFeeAvailable = _session.Rules?.CardDatabase is { } db
                && HouseModifierService.HasEligibleEntryFeeAce(
                    player, Correspondence.ElementFor(sign), _session, db);

            var eyebrow = Root.Q<Label>(className: "eyebrow");
            if (eyebrow != null)
            {
                eyebrow.text = entryFeeAvailable
                    ? $"cost — {standardRequired} {planet} card{(standardRequired == 1 ? "" : "s")} or 1 Entry Fee ace"
                    : standardRequired == 1
                        ? $"cost — 1 {planet} card"
                        : $"cost — 2 {planet} cards";
            }

            bool canBuild = CanBuild(player, sign, out string reason);
            var cards = CollectEligibleCards(player, sign, planet);

            var layoutKey = BuildPoolLayoutKey(cards, _selected);
            if (layoutKey != _lastPoolLayoutKey)
            {
                _lastPoolLayoutKey = layoutKey;
                SummerCardPickBindings.RebuildPool(Root, _session, cards, _selected, null, OnCardToggle);
            }

            var btn = Btn("build-btn");
            if (btn != null)
            {
                bool readyToBuild = canBuild && IsSelectionReady(player, sign, out int required);
                btn.SetEnabled(readyToBuild);
                btn.EnableInClassList("btn--disabled", !readyToBuild);
                btn.text = !canBuild
                    ? reason
                    : !readyToBuild
                        ? DescribePaymentNeed(player, sign, planet, standardRequired, entryFeeAvailable)
                        : $"Raise the House on {sign}";
            }
        }

        static string DescribePaymentNeed(
            PlayerState player, ZodiacSign sign, Planet planet,
            int standardRequired, bool entryFeeAvailable)
        {
            if (entryFeeAvailable)
                return $"Select 1 Entry Fee ace or {standardRequired} {planet} card{(standardRequired == 1 ? "" : "s")}";
            return $"Need {standardRequired} {planet} card{(standardRequired == 1 ? "" : "s")}";
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

        List<string> CollectEligibleCards(PlayerState player, ZodiacSign sign, Planet planet)
        {
            var list = new List<string>();
            var db = _session!.Rules?.CardDatabase;
            if (db == null) return list;

            var signElement = Correspondence.ElementFor(sign);
            foreach (var id in player.Spread)
                TryAddEligible(id, list, db, planet, signElement);
            foreach (var id in player.Hand)
                TryAddEligible(id, list, db, planet, signElement);

            return list;
        }

        void TryAddEligible(string id, List<string> list, ICardDatabase db, Planet planet, Element signElement)
        {
            if (!TapSwapBindings.IsMinorArcana(_session!, id)) return;
            var inst = _session!.GetCard(id);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return;

            if (def.Planet == planet)
                list.Add(id);
            else if (SpreadHouseEffectCatalog.IsEntryFeeAce(def)
                     && Correspondence.ElementFor(def.Suit) == signElement)
                list.Add(id);
        }

        bool IsSelectionReady(PlayerState player, ZodiacSign sign, out int requiredCount)
        {
            requiredCount = 0;
            var db = _session!.Rules?.CardDatabase;
            if (db == null) return false;

            if (_selected.Count == 1)
            {
                foreach (var id in _selected)
                {
                    var inst = _session.GetCard(id);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def != null
                        && HouseModifierService.ValidateEntryFeePayment(
                            _session, _playerId, sign, new List<string> { id }, db, out _))
                    {
                        requiredCount = 1;
                        return true;
                    }
                }
            }

            requiredCount = StandardPaymentCount;
            if (_selected.Count != requiredCount) return false;

            return HouseModifierService.ValidateStandardPayment(
                _session, _playerId, sign, new List<string>(_selected), db, out _);
        }

        static string BuildPoolLayoutKey(IReadOnlyList<string> cards, HashSet<string> selected)
        {
            var sortedCards = new List<string>(cards);
            sortedCards.Sort();
            var sortedSelected = new List<string>(selected);
            sortedSelected.Sort();
            return string.Join(",", sortedCards) + "|" + string.Join(",", sortedSelected);
        }

        void OnCardToggle(string cardId)
        {
            var db = _session?.Rules?.CardDatabase;
            var sign = _session != null ? _session.Players[_playerId].CurrentSign : ZodiacSign.None;
            var inst = _session?.GetCard(cardId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            bool isEntryFee = def != null && SpreadHouseEffectCatalog.IsEntryFeeAce(def);

            if (_selected.Contains(cardId))
            {
                _selected.Remove(cardId);
            }
            else if (isEntryFee)
            {
                _selected.Clear();
                _selected.Add(cardId);
            }
            else if (_selected.Count < StandardPaymentCount)
            {
                _selected.RemoveWhere(id =>
                {
                    var i = _session!.GetCard(id);
                    var d = i != null && db != null ? db.GetById(i.DefinitionId) : null;
                    return d != null && SpreadHouseEffectCatalog.IsEntryFeeAce(d);
                });
                _selected.Add(cardId);
            }
            else if (StandardPaymentCount == 1)
            {
                _selected.Clear();
                _selected.Add(cardId);
            }

            RefreshUi();
        }

        void OnBuild()
        {
            if (_bridge == null || _session == null || _playerId < 0) return;
            var sign = _session.Players[_playerId].CurrentSign;
            if (!IsSelectionReady(_session.Players[_playerId], sign, out _)) return;

            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new BuildAstralHouseCommand(_playerId, sign, ids)))
                OnCompleted?.Invoke();
        }
    }
}
