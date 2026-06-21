using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    public enum HarvestAspectTier
    {
        None,
        Element,
        Planet,
        Sign
    }

    public readonly struct HarvestSourceRow
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly int Points;
        public readonly HarvestAspectTier Tier;
        public readonly bool ShowDash;

        public HarvestSourceRow(string title, string subtitle, int points, HarvestAspectTier tier, bool showDash = false)
        {
            Title = title;
            Subtitle = subtitle;
            Points = points;
            Tier = tier;
            ShowDash = showDash;
        }
    }

    public readonly struct HarvestBreakdown
    {
        public readonly int Base;
        public readonly int BonusSubtotal;
        public readonly int Boon;
        public readonly int Total;
        public readonly IReadOnlyList<HarvestSourceRow> Sources;

        public HarvestBreakdown(int baseDraw, int bonusSubtotal, int boon, int total,
            IReadOnlyList<HarvestSourceRow> sources)
        {
            Base = baseDraw;
            BonusSubtotal = bonusSubtotal;
            Boon = boon;
            Total = total;
            Sources = sources;
        }
    }

    /// <summary>
    /// Itemizes harvest card count using the same rules as <see cref="SpringRules.CalculateHarvestCount"/>.
    /// </summary>
    public static class HarvestBreakdownService
    {
        public const int BaseDraw = 3;

        public static HarvestBreakdown Build(GameSession session, int playerId)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return new HarvestBreakdown(BaseDraw, 0, 0, BaseDraw, System.Array.Empty<HarvestSourceRow>());

            var player = session.Players[playerId];
            var cosmic = session.Board.CosmicAgeSign;
            var rows = new List<HarvestSourceRow>();

            int boardCosmic = session.Board.CosmicEffect.HarvestBaseBonus;
            if (boardCosmic != 0)
            {
                rows.Add(new HarvestSourceRow(
                    "cosmic age effect",
                    "age sign modifier",
                    boardCosmic,
                    HarvestAspectTier.None));
            }

            int personalCosmic = player.PersonalCosmicEffects.HarvestBaseBonus;
            if (personalCosmic != 0)
            {
                rows.Add(new HarvestSourceRow(
                    "personal cosmic effect",
                    "your sign & houses",
                    personalCosmic,
                    HarvestAspectTier.None));
            }

            int signPts = AlignmentBonus(player.CurrentSign, cosmic);
            rows.Add(BuildSignRow(
                $"Zodiac die: {player.CurrentSign}",
                player.CurrentSign,
                cosmic,
                signPts));

            foreach (var houseSign in player.AstralHouses)
            {
                int pts = AlignmentBonus(houseSign, cosmic);
                rows.Add(BuildSignRow(
                    $"Astral House: {houseSign}",
                    houseSign,
                    cosmic,
                    pts));
            }

            var cosmicElement = Correspondence.ElementFor(cosmic);
            if (cosmicElement != Element.None)
            {
                foreach (var cardId in player.Spread)
                {
                    var inst = session.GetCard(cardId);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null || Correspondence.ElementFor(def.Suit) != cosmicElement)
                        continue;

                    rows.Add(new HarvestSourceRow(
                        $"Spread: {def.Rank} of {def.Suit}",
                        $"element · {cosmicElement.ToString().ToLowerInvariant()}",
                        1,
                        HarvestAspectTier.Element));
                }
            }

            foreach (var cardId in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(cardId)) continue;
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType != MajorArcanaType.Adept || def.Sign == ZodiacSign.None)
                    continue;

                int pts = AlignmentBonus(def.Sign, cosmic);
                rows.Add(BuildSignRow(
                    $"Adept: {def.Id}",
                    def.Sign,
                    cosmic,
                    pts));
            }

            int boon = AgekeeperMatchesCosmic(session) ? 2 : 0;
            if (boon > 0)
            {
                rows.Add(new HarvestSourceRow(
                    "Agekeeper's Boon",
                    "their roll matched the age · +2 to everyone",
                    boon,
                    HarvestAspectTier.None));
            }

            int bonusSubtotal = boardCosmic + personalCosmic;
            foreach (var row in rows)
            {
                if (row.Title == "Agekeeper's Boon") continue;
                if (!row.ShowDash)
                    bonusSubtotal += row.Points;
            }

            int total = session.Rules!.Harvest.CalculateHarvestCount(session, playerId);
            return new HarvestBreakdown(BaseDraw, bonusSubtotal, boon, total, rows);
        }

        static HarvestSourceRow BuildSignRow(string title, ZodiacSign source, ZodiacSign cosmic, int points)
        {
            if (points <= 0)
            {
                return new HarvestSourceRow(title, "no match", 0, HarvestAspectTier.None, showDash: true);
            }

            var (label, tier) = DescribeAspect(source, cosmic, points);
            return new HarvestSourceRow(title, label, points, tier);
        }

        static (string Label, HarvestAspectTier Tier) DescribeAspect(ZodiacSign source, ZodiacSign cosmic, int points)
        {
            if (source == cosmic)
                return ($"sign · {source.ToString().ToLowerInvariant()}", HarvestAspectTier.Sign);
            if (points >= 2)
            {
                var planet = Correspondence.PlanetFor(source);
                return ($"planet · {planet.ToString().ToLowerInvariant()}", HarvestAspectTier.Planet);
            }

            var element = Correspondence.ElementFor(source);
            return ($"element · {element.ToString().ToLowerInvariant()}", HarvestAspectTier.Element);
        }

        static int AlignmentBonus(ZodiacSign playerSign, ZodiacSign cosmicSign)
        {
            if (playerSign == ZodiacSign.None || cosmicSign == ZodiacSign.None) return 0;
            if (playerSign == cosmicSign) return 3;
            if (Correspondence.PlanetFor(playerSign) == Correspondence.PlanetFor(cosmicSign)) return 2;
            if (Correspondence.ElementFor(playerSign) == Correspondence.ElementFor(cosmicSign)) return 1;
            return 0;
        }

        static bool AgekeeperMatchesCosmic(GameSession session)
        {
            foreach (var p in session.Players)
            {
                if (p.IsAgekeeper && p.CurrentSign == session.Board.CosmicAgeSign
                    && p.CurrentSign != ZodiacSign.None)
                    return true;
            }
            return false;
        }
    }
}
