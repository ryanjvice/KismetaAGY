using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Chronicle;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class VictoryController : ScreenController
    {
        public override string ScreenId => ScreenIds.Victory;

        public Action? OnChronicle;
        public Action? OnNewGame;

        protected override void Wire()
        {
            Btn("chronicle-btn")!.clicked += () => OnChronicle?.Invoke();
            Btn("newgame-btn")!.clicked += () => OnNewGame?.Invoke();
        }

        public void BindState(GameSession session, GameChronicle? _)
        {
            if (Root == null) return;

            int? winnerId = session.WinnerPlayerId;
            if (winnerId.HasValue && Lbl("winner-name") != null)
                Lbl("winner-name")!.text = $"{PlayerUiNames.ForPlayer(winnerId.Value)} has completed the Great Work";

            RebuildStandings(session, winnerId);
        }

        void RebuildStandings(GameSession session, int? winnerId)
        {
            var host = El("standings");
            if (host == null) return;
            host.Clear();

            var players = new System.Collections.Generic.List<PlayerState>(session.Players);
            players.Sort((a, b) => b.StonePosition.Value.CompareTo(a.StonePosition.Value));

            for (int rank = 0; rank < players.Count; rank++)
            {
                var p = players[rank];
                bool isWinner = winnerId == p.PlayerId;

                var row = new VisualElement();
                row.AddToClassList("standing");
                if (isWinner)
                {
                    var gold = new StyleColor(new UnityEngine.Color(0.79f, 0.59f, 0.18f));
                    row.style.borderTopColor = row.style.borderRightColor =
                        row.style.borderBottomColor = row.style.borderLeftColor = gold;
                }

                var rankLbl = new Label((rank + 1).ToString());
                rankLbl.AddToClassList("standing__rank");
                if (isWinner)
                    rankLbl.style.color = new StyleColor(new UnityEngine.Color(0.96f, 0.83f, 0.37f));
                row.Add(rankLbl);

                var dot = new VisualElement();
                dot.style.width = 13;
                dot.style.height = 13;
                dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
                    dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 7;
                dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));
                dot.style.marginRight = 8;
                row.Add(dot);

                var name = new Label(PlayerUiNames.ShortName(p.PlayerId));
                name.style.flexGrow = 1;
                name.style.fontSize = 12;
                name.style.color = new StyleColor(new UnityEngine.Color(0.95f, 0.91f, 0.82f));
                row.Add(name);

                var stone = new Label(StoneLabel(p.StonePosition));
                stone.style.fontSize = 10;
                stone.style.color = new StyleColor(isWinner
                    ? new UnityEngine.Color(0.96f, 0.83f, 0.37f)
                    : new UnityEngine.Color(0.72f, 0.6f, 0.43f));
                row.Add(stone);

                host.Add(row);
            }
        }

        static string StoneLabel(StonePosition pos) => pos.Value switch
        {
            8 => "Altar",
            >= 6 => "Gold",
            >= 4 => "Silver",
            >= 2 => "Bronze",
            _ => "Lead"
        };
    }
}
