using System.Collections.Generic;
using Kismeta.UI.Chronicle;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Painter2D line chart for the race-to-the-Altar stone positions.</summary>
    public sealed class ChronicleChartElement : VisualElement
    {
        IReadOnlyList<StoneRacePoint> _points = new List<StoneRacePoint>();
        int _playerCount;

        const float PadL = 4f;
        const float PadR = 8f;
        const float PadT = 8f;
        const float PadB = 12f;

        public ChronicleChartElement()
        {
            style.minHeight = 140;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetData(IReadOnlyList<StoneRacePoint> points, int playerCount)
        {
            _points = points;
            _playerCount = playerCount;
            MarkDirtyRepaint();
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var rect = contentRect;
            if (rect.width < 10 || rect.height < 10) return;

            var painter = ctx.painter2D;
            float plotW = rect.width - PadL - PadR;
            float plotH = rect.height - PadT - PadB;
            if (plotW <= 0 || plotH <= 0) return;

            int minRound = 1, maxRound = 1;
            foreach (var pt in _points)
            {
                if (pt.RoundNumber < minRound) minRound = pt.RoundNumber;
                if (pt.RoundNumber > maxRound) maxRound = pt.RoundNumber;
            }
            if (maxRound < minRound) maxRound = minRound;
            int roundSpan = Mathf.Max(1, maxRound - minRound);

            painter.lineWidth = 1f;
            for (int stage = 0; stage <= 8; stage += 2)
            {
                float y = PadT + plotH * (1f - stage / 8f);
                painter.strokeColor = new Color(0.35f, 0.28f, 0.22f, 0.45f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(PadL, y));
                painter.LineTo(new Vector2(PadL + plotW, y));
                painter.Stroke();
            }

            for (int pid = 0; pid < _playerCount; pid++)
            {
                var series = new List<Vector2>();
                foreach (var pt in _points)
                {
                    if (pt.PlayerId != pid) continue;
                    float x = PadL + plotW * ((pt.RoundNumber - minRound) / (float)roundSpan);
                    float y = PadT + plotH * (1f - pt.Position.Value / 8f);
                    series.Add(new Vector2(x, y));
                }
                if (series.Count == 0) continue;

                var color = PlayerUiNames.PlayerColor(pid);
                painter.strokeColor = color;
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(series[0]);
                for (int i = 1; i < series.Count; i++)
                    painter.LineTo(series[i]);
                painter.Stroke();

                foreach (var pt in series)
                    DrawMarker(painter, pt, pid, color);
            }
        }

        static void DrawMarker(Painter2D painter, Vector2 pt, int playerId, Color color)
        {
            painter.fillColor = color;
            painter.lineWidth = 1.5f;
            float r = 4f;

            switch (playerId % 4)
            {
                case 0:
                    painter.BeginPath();
                    painter.Arc(pt, r, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
                    painter.Fill();
                    break;
                case 1:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(pt.x - r, pt.y - r));
                    painter.LineTo(new Vector2(pt.x + r, pt.y - r));
                    painter.LineTo(new Vector2(pt.x + r, pt.y + r));
                    painter.LineTo(new Vector2(pt.x - r, pt.y + r));
                    painter.ClosePath();
                    painter.Fill();
                    break;
                case 2:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(pt.x, pt.y - r));
                    painter.LineTo(new Vector2(pt.x + r, pt.y + r));
                    painter.LineTo(new Vector2(pt.x - r, pt.y + r));
                    painter.ClosePath();
                    painter.Fill();
                    break;
                default:
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(pt.x, pt.y - r));
                    painter.LineTo(new Vector2(pt.x + r, pt.y));
                    painter.LineTo(new Vector2(pt.x, pt.y + r));
                    painter.LineTo(new Vector2(pt.x - r, pt.y));
                    painter.ClosePath();
                    painter.Fill();
                    break;
            }
        }
    }
}
