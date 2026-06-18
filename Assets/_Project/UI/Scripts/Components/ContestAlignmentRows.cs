using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Builds Opposition alignment tally rows (sign +3 / planet +2 / element +1).</summary>
    public static class ContestAlignmentRows
    {
        public readonly struct TallyRow
        {
            public readonly string Label;
            public readonly int Points;
            public readonly string PointsClass;

            public TallyRow(string label, int points, string pointsClass)
            {
                Label = label;
                Points = points;
                PointsClass = pointsClass;
            }
        }

        public static List<TallyRow> BuildRows(GameSession session, int playerId, ZodiacSign referenceSign)
        {
            var rows = new List<TallyRow>();
            if (referenceSign == ZodiacSign.None || playerId < 0 || playerId >= session.Players.Count)
                return rows;

            var player = session.Players[playerId];
            var db = session.Rules?.CardDatabase;
            if (db == null) return rows;

            if (player.CurrentSign != ZodiacSign.None)
            {
                int pts = AspectScore(player.CurrentSign, referenceSign);
                if (pts > 0)
                    rows.Add(new TallyRow($"personal sign ({player.CurrentSign})", pts, ClassForPoints(pts)));
            }

            foreach (var house in player.AstralHouses)
            {
                int pts = AspectScore(house, referenceSign);
                if (pts > 0)
                    rows.Add(new TallyRow($"house on {house}", pts, ClassForPoints(pts)));
            }

            foreach (var id in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(id)) continue;
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.Sign != ZodiacSign.None)
                {
                    int pts = AspectScore(def.Sign, referenceSign);
                    if (pts > 0)
                        rows.Add(new TallyRow($"adept ({def.Id})", pts, ClassForPoints(pts)));
                }
            }

            foreach (var id in player.Spread)
                AddCardRow(session, db, id, referenceSign, rows, "spread");
            foreach (var id in player.Hand)
                AddCardRow(session, db, id, referenceSign, rows, "hand");

            return rows;
        }

        static void AddCardRow(GameSession session, ICardDatabase db, string cardId,
            ZodiacSign referenceSign, List<TallyRow> rows, string zone)
        {
            var inst = session.GetCard(cardId);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return;

            int pts = CardScore(def.Suit, def.Planet, referenceSign);
            if (pts > 0)
                rows.Add(new TallyRow($"{def.Rank} {def.Suit} ({zone})", pts, ClassForPoints(pts)));
        }

        public static void Populate(VisualElement root, GameSession session, int youId, int foeId)
        {
            var cosmic = session.Board.CosmicAgeSign;
            var youRows = root.Q<VisualElement>("tally-you-rows");
            var foeRows = root.Q<VisualElement>("tally-foe-rows");
            if (youRows != null) FillContainer(youRows, BuildRows(session, youId, cosmic));
            if (foeRows != null) FillContainer(foeRows, BuildRows(session, foeId, cosmic));

            int youTotal = session.Rules?.Alignment?.CalculateAlignmentPoints(session, youId, cosmic) ?? 0;
            int foeTotal = session.Rules?.Alignment?.CalculateAlignmentPoints(session, foeId, cosmic) ?? 0;
            foeTotal += session.Players[foeId].BesiegedBonusCount;

            var youLbl = root.Q<Label>("you-total");
            var foeLbl = root.Q<Label>("foe-total");
            if (youLbl != null) youLbl.text = youTotal.ToString();
            if (foeLbl != null) foeLbl.text = foeTotal.ToString();
        }

        static void FillContainer(VisualElement host, List<TallyRow> rows)
        {
            host.Clear();
            foreach (var row in rows)
            {
                var tally = new VisualElement();
                tally.AddToClassList("tally");

                var label = new Label(row.Label) { style = { flexGrow = 1, fontSize = 11 } };
                var pts = new Label($"+{row.Points}") { name = "pts" };
                pts.AddToClassList("tally__pts");
                pts.AddToClassList(row.PointsClass);

                tally.Add(label);
                tally.Add(pts);
                host.Add(tally);
            }

            if (rows.Count == 0)
            {
                host.Add(new Label("no alignment bonuses")
                {
                    style = { fontSize = 10, color = new StyleColor(new UnityEngine.Color(0.42f, 0.48f, 0.54f)) }
                });
            }
        }

        static int AspectScore(ZodiacSign source, ZodiacSign reference)
        {
            if (source == ZodiacSign.None || reference == ZodiacSign.None) return 0;
            if (source == reference) return 3;
            if (Correspondence.PlanetFor(source) == Correspondence.PlanetFor(reference)) return 2;
            if (Correspondence.ElementFor(source) == Correspondence.ElementFor(reference)) return 1;
            return 0;
        }

        static int CardScore(Suit suit, Planet planet, ZodiacSign reference)
        {
            var refPlanet = Correspondence.PlanetFor(reference);
            if (planet != Planet.None && planet == refPlanet) return 2;
            if (suit != Suit.None)
            {
                var cardElement = Correspondence.ElementFor(suit);
                var refElement = Correspondence.ElementFor(reference);
                if (cardElement != Element.None && cardElement == refElement) return 1;
            }
            return 0;
        }

        static string ClassForPoints(int pts) => pts switch
        {
            3 => "tally__pts--sign",
            2 => "tally__pts--planet",
            _ => "tally__pts--element"
        };
    }
}
