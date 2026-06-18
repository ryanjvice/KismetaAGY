using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
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
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("build-btn")!.clicked += OnBuild;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionHelpers.ResolvePlayerId(session, bridge);
            if (_playerId < 0 || Root == null) return;

            var player = session.Players[_playerId];
            var sign = player.CurrentSign;
            var requiredPlanet = Correspondence.PlanetFor(sign);

            var costEyebrow = Root.Q<Label>(className: "eyebrow");
            if (costEyebrow != null)
                costEyebrow.text = $"cost — 1 {requiredPlanet} card";

            var pool = El("cost-cards");
            if (pool != null)
            {
                pool.Clear();
                var db = session.Rules?.CardDatabase;
                if (db == null) return;

                var cards = SummerActionHelpers.CollectMinorCards(session, player);
                foreach (var id in cards)
                {
                    var inst = session.GetCard(id);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null || def.Planet != requiredPlanet) continue;

                    bool sel = _selected.Contains(id);
                    var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: sel);
                    chip.userData = id;
                    chip.RegisterCallback<ClickEvent>(_ =>
                    {
                        if (_selected.Contains(id)) _selected.Remove(id);
                        else
                        {
                            _selected.Clear();
                            _selected.Add(id);
                        }
                        BindState(session, bridge);
                    });
                    pool.Add(chip);
                }
            }

            bool canBuild = CanBuild(player, sign);
            var btn = Btn("build-btn");
            if (btn != null)
            {
                btn.text = canBuild && _selected.Count == 1
                    ? $"Raise the House on {sign}"
                    : BuildDisabledLabel(player, sign);
                btn.SetEnabled(canBuild && _selected.Count == 1);
                btn.EnableInClassList("btn--disabled", !canBuild || _selected.Count != 1);
            }
        }

        static string BuildDisabledLabel(PlayerState player, ZodiacSign sign)
        {
            if (player.UnplacedAstralHouses <= 0) return "No house tokens left";
            if (player.AstralHouses.Contains(sign)) return $"Already built on {sign}";
            return $"Select 1 {Correspondence.PlanetFor(sign)} card";
        }

        bool CanBuild(PlayerState player, ZodiacSign sign)
        {
            if (_session == null) return false;
            if (player.UnplacedAstralHouses <= 0) return false;
            if (player.AstralHouses.Contains(sign)) return false;
            foreach (var p in _session.Players)
                if (p.PlayerId != _playerId && p.AstralHouses.Contains(sign))
                    return false;
            return true;
        }

        void OnBuild()
        {
            if (_bridge == null || _session == null || _playerId < 0) return;
            if (_selected.Count != 1) return;
            var sign = _session.Players[_playerId].CurrentSign;
            if (_bridge.TrySubmit(new BuildAstralHouseCommand(_playerId, sign, new List<string>(_selected))))
                OnCompleted?.Invoke();
        }
    }
}
