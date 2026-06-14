using System;
using System.Collections.Generic;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Validates a player's presented Spread cards against a Crucible card's Alchemical Formula string.
    ///
    /// Formula strings from cards.json are matched against a known lookup table. Each formula maps
    /// to one or two <see cref="FormulaSegment"/> requirements that must be satisfied by non-overlapping
    /// subsets of the presented cards.
    ///
    /// Segment types:
    ///   SuitGroup     — N cards of the specified Suit (any rank)
    ///   PlanetGroup   — N cards of the specified Planet (any suit/rank)
    ///   StraightSuit  — N consecutive-rank cards of the specified Suit (Ace = 1 only in straights)
    ///   SuitPair      — 2 cards of the same Suit AND same Rank
    ///   PlanetPair    — 2 cards of the same Planet AND same Rank
    ///   TwoPairs      — 4 cards: one same-rank pair per each of two Suits
    /// </summary>
    public sealed class AlchemicalAlignmentValidator
    {
        // ── Segment model ─────────────────────────────────────────────────────────

        public enum SegmentKind { SuitGroup, PlanetGroup, StraightSuit, SuitPair, PlanetPair, TwoPairs }

        public sealed record FormulaSegment(
            SegmentKind Kind,
            int         Count,
            Suit        Suit    = Suit.None,
            Planet      Planet  = Planet.None,
            Suit        Suit2   = Suit.None); // used by TwoPairs

        // ── Formula lookup ────────────────────────────────────────────────────────

        private static readonly Dictionary<string, FormulaSegment[]> _lookup =
            BuildLookup();

        private static Dictionary<string, FormulaSegment[]> BuildLookup()
        {
            var d = new Dictionary<string, FormulaSegment[]>(StringComparer.OrdinalIgnoreCase);

            // ── Single-segment ──
            d["Pair of Swords with the same Rank"]           = new[] { new FormulaSegment(SegmentKind.SuitPair,   2, Suit.Swords) };
            d["Pair of Mercury planets (5s + Princess)"]     = new[] { new FormulaSegment(SegmentKind.PlanetPair, 2, Planet: Planet.Mercury) };
            d["Pair of Moon planets (2s + Queens)"]          = new[] { new FormulaSegment(SegmentKind.PlanetPair, 2, Planet: Planet.Moon) };
            d["Pair of Venus planets (4s + 9s)"]             = new[] { new FormulaSegment(SegmentKind.PlanetPair, 2, Planet: Planet.Venus) };
            d["3-card Wands Straight (Any 3 consecutive ranks)"]    = new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Wands) };
            d["3-card Swords Straight (Any 3 consecutive ranks)"]   = new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Swords) };
            d["3-card Pentacles Straight (Any 3 consecutive ranks)"]= new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Pentacles) };
            d["3-card Cups Straight (Any 3 consecutive ranks)"]     = new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Cups) };
            // Short form (no parenthetical)
            d["3-card Wands Straight"]    = new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Wands) };
            d["3-card Swords Straight"]   = new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Swords) };
            d["3-card Pentacles Straight"]= new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Pentacles) };
            d["3-card Cups Straight"]     = new[] { new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Cups) };
            d["3 Moons (2s & Queens)"]                       = new[] { new FormulaSegment(SegmentKind.PlanetGroup, 3, Planet: Planet.Moon) };
            d["Two Pairs — Pentacles and Swords Suits only"] = new[] { new FormulaSegment(SegmentKind.TwoPairs,   4, Suit.Pentacles, Suit2: Suit.Swords) };

            // ── Two-segment ──
            d["One Venus + Pair of Pentacles (Same Rank)"]   = new[] { new FormulaSegment(SegmentKind.PlanetGroup, 1, Planet: Planet.Venus),
                                                                         new FormulaSegment(SegmentKind.SuitPair,   2, Suit.Pentacles) };
            d["One Sun + Pair of Wands (Same Rank)"]         = new[] { new FormulaSegment(SegmentKind.PlanetGroup, 1, Planet: Planet.Sun),
                                                                         new FormulaSegment(SegmentKind.SuitPair,   2, Suit.Wands) };
            d["Three Cups (any ranks) + one Sun card (Ace)"] = new[] { new FormulaSegment(SegmentKind.SuitGroup,   3, Suit.Cups),
                                                                         new FormulaSegment(SegmentKind.PlanetGroup, 1, Planet: Planet.Sun) };
            d["Three Mars planets (7s + Knights) + Two Cups"]= new[] { new FormulaSegment(SegmentKind.PlanetGroup, 3, Planet: Planet.Mars),
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   2, Suit.Cups) };
            d["Three Jupiter planets (3s + 8s) + Two Wands"] = new[] { new FormulaSegment(SegmentKind.PlanetGroup, 3, Planet: Planet.Jupiter),
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   2, Suit.Wands) };
            d["Three Saturn planets (6s, 10s, or Kings) + Two Pentacles"] = new[] {
                                                                         new FormulaSegment(SegmentKind.PlanetGroup, 3, Planet: Planet.Saturn),
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   2, Suit.Pentacles) };
            d["Two Mars planet (7 or Knight) + 3-card Wands straight"]  = new[] {
                                                                         new FormulaSegment(SegmentKind.PlanetGroup,  2, Planet: Planet.Mars),
                                                                         new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Wands) };
            d["3-card Swords straight + Two Venus cards (3 or 8)"]      = new[] {
                                                                         new FormulaSegment(SegmentKind.StraightSuit, 3, Suit.Swords),
                                                                         new FormulaSegment(SegmentKind.PlanetGroup,  2, Planet: Planet.Venus) };
            d["Four Mercury planets (5s + Princesses) + Two Cups"]      = new[] {
                                                                         new FormulaSegment(SegmentKind.PlanetGroup, 4, Planet: Planet.Mercury),
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   2, Suit.Cups) };
            d["Four Wands + Two Sun cards (Aces)"]                      = new[] {
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   4, Suit.Wands),
                                                                         new FormulaSegment(SegmentKind.PlanetGroup, 2, Planet: Planet.Sun) };
            d["Four Swords + Two Venus planets (4s or 9s)"]             = new[] {
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   4, Suit.Swords),
                                                                         new FormulaSegment(SegmentKind.PlanetGroup, 2, Planet: Planet.Venus) };
            d["Four Saturn planets (6s/10s/Kings) + Two Pentacles"]     = new[] {
                                                                         new FormulaSegment(SegmentKind.PlanetGroup, 4, Planet: Planet.Saturn),
                                                                         new FormulaSegment(SegmentKind.SuitGroup,   2, Suit.Pentacles) };

            return d;
        }

        // ── Public entry point ────────────────────────────────────────────────────

        /// <summary>
        /// Returns (true, "") if <paramref name="cards"/> satisfy the Alchemical Formula
        /// identified by <paramref name="formula"/>. Returns (false, reason) otherwise.
        /// </summary>
        public (bool ok, string reason) Validate(string formula, IReadOnlyList<CardDefinition> cards)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return (true, ""); // no formula on this card — nothing to validate

            if (!_lookup.TryGetValue(formula.Trim(), out var segments))
                return (false, $"Unknown formula: \"{formula}\". Cannot validate.");

            // Total cards required
            int required = 0;
            foreach (var seg in segments) required += seg.Count;

            if (cards.Count < required)
                return (false, $"Need {required} card(s); {cards.Count} provided.");

            // Greedily assign cards to segments, consuming used indices
            var used   = new bool[cards.Count];
            var reason = "";

            foreach (var seg in segments)
            {
                var segResult = ValidateSegment(seg, cards, used);
                if (!segResult.ok)
                    return (false, segResult.reason);
            }

            return (true, "");
        }

        // ── Segment dispatch ──────────────────────────────────────────────────────

        private static (bool ok, string reason) ValidateSegment(
            FormulaSegment seg, IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            switch (seg.Kind)
            {
                case SegmentKind.SuitGroup:    return PickSuitGroup(seg.Count, seg.Suit, cards, used);
                case SegmentKind.PlanetGroup:  return PickPlanetGroup(seg.Count, seg.Planet, cards, used);
                case SegmentKind.StraightSuit: return PickStraight(seg.Count, seg.Suit, cards, used);
                case SegmentKind.SuitPair:     return PickSuitPair(seg.Suit, cards, used);
                case SegmentKind.PlanetPair:   return PickPlanetPair(seg.Planet, cards, used);
                case SegmentKind.TwoPairs:     return PickTwoPairs(seg.Suit, seg.Suit2, cards, used);
                default:                       return (false, $"Unknown segment kind {seg.Kind}");
            }
        }

        // ── Segment validators ────────────────────────────────────────────────────

        private static (bool, string) PickSuitGroup(int n, Suit suit,
            IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            int found = 0;
            for (int i = 0; i < cards.Count && found < n; i++)
                if (!used[i] && cards[i].Suit == suit) { used[i] = true; found++; }

            return found >= n
                ? (true, "")
                : (false, $"Need {n} {suit} card(s); only {found} available.");
        }

        private static (bool, string) PickPlanetGroup(int n, Planet planet,
            IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            int found = 0;
            for (int i = 0; i < cards.Count && found < n; i++)
                if (!used[i] && cards[i].Planet == planet) { used[i] = true; found++; }

            return found >= n
                ? (true, "")
                : (false, $"Need {n} {planet}-planet card(s); only {found} available.");
        }

        private static (bool, string) PickStraight(int n, Suit suit,
            IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            // Collect unused cards of the required suit, mapped to rank value (Ace = 1)
            var rankToIdx = new Dictionary<int, int>();
            for (int i = 0; i < cards.Count; i++)
            {
                if (used[i] || cards[i].Suit != suit) continue;
                int rv = RankValue(cards[i].Rank, aceHigh: false);
                if (rv > 0 && !rankToIdx.ContainsKey(rv))
                    rankToIdx[rv] = i;
            }

            var ranks = new List<int>(rankToIdx.Keys);
            ranks.Sort();

            // Find a run of n consecutive ranks
            for (int start = 0; start <= ranks.Count - n; start++)
            {
                bool consecutive = true;
                for (int j = 1; j < n; j++)
                    if (ranks[start + j] != ranks[start] + j) { consecutive = false; break; }

                if (!consecutive) continue;

                for (int j = 0; j < n; j++)
                    used[rankToIdx[ranks[start + j]]] = true;
                return (true, "");
            }

            return (false, $"Need a {n}-card {suit} Straight (consecutive ranks); not found.");
        }

        private static (bool, string) PickSuitPair(Suit suit,
            IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            // Find two unused cards with the same Suit AND same Rank
            for (int i = 0; i < cards.Count; i++)
            {
                if (used[i] || cards[i].Suit != suit) continue;
                for (int j = i + 1; j < cards.Count; j++)
                {
                    if (used[j] || cards[j].Suit != suit) continue;
                    if (cards[i].Rank == cards[j].Rank)
                    {
                        used[i] = used[j] = true;
                        return (true, "");
                    }
                }
            }
            return (false, $"Need a same-rank pair of {suit}; not found.");
        }

        private static (bool, string) PickPlanetPair(Planet planet,
            IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            // Find two unused cards with the same Planet AND same Rank
            for (int i = 0; i < cards.Count; i++)
            {
                if (used[i] || cards[i].Planet != planet) continue;
                for (int j = i + 1; j < cards.Count; j++)
                {
                    if (used[j] || cards[j].Planet != planet) continue;
                    if (cards[i].Rank == cards[j].Rank)
                    {
                        used[i] = used[j] = true;
                        return (true, "");
                    }
                }
            }
            return (false, $"Need a same-rank pair of {planet}-planet cards; not found.");
        }

        private static (bool ok, string reason) PickTwoPairs(Suit suit1, Suit suit2,
            IReadOnlyList<CardDefinition> cards, bool[] used)
        {
            var r1 = PickSuitPair(suit1, cards, used);
            if (!r1.ok) return (false, $"Two-Pairs: first pair ({suit1}) — {r1.reason}");

            var r2 = PickSuitPair(suit2, cards, used);
            if (!r2.ok) return (false, $"Two-Pairs: second pair ({suit2}) — {r2.reason}");

            return (true, "");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns 1–10 for numeric ranks, 11 for Princess, 12 for Knight, 13 for Queen, 14 for King.
        /// Ace returns 1 (aceHigh=false) or 15 (aceHigh=true). Court cards are not counted in straights.</summary>
        private static int RankValue(Rank rank, bool aceHigh)
        {
            return rank switch
            {
                Rank.Ace      => aceHigh ? 15 : 1,
                Rank.Two      => 2,
                Rank.Three    => 3,
                Rank.Four     => 4,
                Rank.Five     => 5,
                Rank.Six      => 6,
                Rank.Seven    => 7,
                Rank.Eight    => 8,
                Rank.Nine     => 9,
                Rank.Ten      => 10,
                // Court cards — excluded from straights (return 0)
                _             => 0,
            };
        }

        /// <summary>Returns the display description of a formula, or the raw string if unrecognised.</summary>
        public static string Describe(string formula) =>
            _lookup.ContainsKey(formula?.Trim() ?? "") ? formula : $"(unknown formula) {formula}";
    }
}
