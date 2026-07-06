using System;
using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Chronicle;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class ChronicleController : ScreenController
    {
        public override string ScreenId => ScreenIds.Chronicle;

        public Action? OnBack;
        public Action? OnNewGame;

        ChronicleChartElement? _chart;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("newgame-btn")!.clicked += () => OnNewGame?.Invoke();
        }

        public void BindState(GameSession session, GameChronicle chronicle)
        {
            if (Root == null) return;

            int ages = Mathf.Max(1, session.Board.RoundNumber);
            int contests = chronicle.TotalContestCount();
            int forged = chronicle.ReagentsForged;

            SetStatValue(0, ages.ToString());
            SetStatValue(1, contests.ToString());
            SetStatValue(2, forged.ToString());

            var sub = Root.Q<Label>(className: "statusbar__sub");
            if (sub != null && session.WinnerPlayerId.HasValue)
            {
                sub.text = $"{ages} ages · {PlayerUiNames.ShortName(session.WinnerPlayerId.Value)} claims the Altar";
            }

            EnsureChart(chronicle, session.Players.Count);
            RebuildLegend(session.Players.Count);
            RebuildContestRows(session, chronicle);
            CeremonyRevealMotion.RevealChronicle(Root);
        }

        void SetStatValue(int index, string value)
        {
            if (Root == null) return;
            var cells = Root.Query(className: "stat-cell").ToList();
            if (index >= cells.Count) return;
            var val = cells[index].Q<Label>(className: "stat-cell__value");
            if (val != null) val.text = value;
        }

        void EnsureChart(GameChronicle chronicle, int playerCount)
        {
            var host = El("chart-host");
            if (host == null) return;
            host.Clear();

            _chart = new ChronicleChartElement();
            _chart.style.flexGrow = 1;
            _chart.style.height = 140;
            _chart.SetData(chronicle.StoneRace, playerCount);
            host.Add(_chart);
        }

        void RebuildLegend(int playerCount)
        {
            var legendRow = El("chart-legend");
            if (legendRow == null) return;
            legendRow.Clear();

            string[] markers = { "●", "■", "▲", "◆" };
            for (int i = 0; i < playerCount; i++)
            {
                var lbl = new Label($"{markers[i % markers.Length]} {PlayerUiNames.ShortName(i)}");
                lbl.style.fontSize = 10;
                lbl.style.color = new StyleColor(PlayerUiNames.PlayerColor(i));
                lbl.style.marginRight = 12;
                legendRow.Add(lbl);
            }
        }

        void RebuildContestRows(GameSession session, GameChronicle chronicle)
        {
            var host = El("record-rows");
            if (host == null) return;
            host.Clear();

            foreach (var p in session.Players)
            {
                var rec = chronicle.GetContestRecord(p.PlayerId);
                var row = new VisualElement();
                row.AddToClassList("standing");

                var name = new Label(PlayerUiNames.ShortName(p.PlayerId));
                name.style.flexGrow = 1.4f;
                name.style.fontSize = 11;
                name.style.color = new StyleColor(new Color(0.95f, 0.91f, 0.82f));
                row.Add(name);

                AddRecordCell(row, $"{rec.DuelWins}-{rec.DuelLosses}");
                AddRecordCell(row, $"{rec.GambitWins}-{rec.GambitLosses}");
                AddRecordCell(row, $"{rec.OppWins}-{rec.OppLosses}");

                host.Add(row);
            }
        }

        static void AddRecordCell(VisualElement row, string text)
        {
            var lbl = new Label(text);
            lbl.style.flexGrow = 1;
            lbl.style.fontSize = 11;
            lbl.style.color = new StyleColor(new Color(0.81f, 0.77f, 0.66f));
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(lbl);
        }
    }
}
