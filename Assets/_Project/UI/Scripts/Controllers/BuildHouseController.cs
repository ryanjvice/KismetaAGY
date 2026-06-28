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

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            _selected.Clear();
            RefreshUi();
            NarrativeSlotBindings.BindById(Root, "spring.buildhouse");
        }

        void RefreshUi()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var sign = player.CurrentSign;
            var planet = Correspondence.PlanetFor(sign);

            var eyebrow = Root.Q<Label>(className: "eyebrow");
            if (eyebrow != null)
                eyebrow.text = $"cost — 1 {planet} card";

            bool canBuild = CanBuild(player, sign, out string reason);
            var cards = CollectEligibleCards(player, planet);

            SummerCardPickBindings.RebuildPool(Root, _session, cards, _selected, null, OnCardToggle);

            var btn = Btn("build-btn");
            if (btn != null)
            {
                btn.SetEnabled(canBuild && _selected.Count == 1);
                btn.EnableInClassList("btn--disabled", !canBuild || _selected.Count != 1);
                btn.text = canBuild
                    ? $"Raise the House on {sign}"
                    : reason;
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

        void OnCardToggle(string cardId)
        {
            if (_selected.Contains(cardId))
                _selected.Remove(cardId);
            else
            {
                _selected.Clear();
                _selected.Add(cardId);
            }
            RefreshUi();
        }

        void OnBuild()
        {
            if (_bridge == null || _session == null || _playerId < 0 || _selected.Count != 1) return;
            var sign = _session.Players[_playerId].CurrentSign;
            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new BuildAstralHouseCommand(_playerId, sign, ids)))
                OnCompleted?.Invoke();
        }
    }
}
