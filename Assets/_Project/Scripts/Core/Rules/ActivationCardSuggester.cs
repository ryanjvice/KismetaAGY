using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Shared helpers for evaluating spread readiness and suggesting card sets
    /// for Crucible activation (UI display, AI, and submit-time fallback).
    /// </summary>
    public static class ActivationCardSuggester
    {
        public static bool CanSpreadSatisfyFormula(
            IReadOnlyList<(string id, CardDefinition def)> spreadCards,
            CodexFormulaDefinition formula,
            ICardDatabase db)
        {
            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int count = 0;
                foreach (var (_, def) in spreadCards)
                {
                    if (WildcardLinkService.MatchesPlanet(def, formula.RequiredPlanet, db))
                        count++;
                }
                return count >= 3;
            }

            int best = BestRankSumFromSpread(spreadCards, formula.RequiredSuit);
            return best >= formula.MinRankSum;
        }

        public static List<string>? SuggestActivationCards(
            IReadOnlyList<(string id, CardDefinition def)> spreadCards,
            CodexFormulaDefinition formula,
            ICardDatabase db)
        {
            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                var matching = new List<string>();
                foreach (var (id, def) in spreadCards)
                {
                    if (WildcardLinkService.MatchesPlanet(def, formula.RequiredPlanet, db))
                        matching.Add(id);
                }
                return matching.Count >= 3 ? matching.GetRange(0, 3) : null;
            }

            var suitCards = new List<(string id, CardDefinition def)>();
            foreach (var (id, def) in spreadCards)
            {
                if (def.Suit == formula.RequiredSuit)
                    suitCards.Add((id, def));
            }

            if (suitCards.Count == 0)
                return null;

            suitCards.Sort((a, b) =>
            {
                int ra = a.def.Rank == Rank.Ace ? 15 : (int)a.def.Rank;
                int rb = b.def.Rank == Rank.Ace ? 15 : (int)b.def.Rank;
                return rb.CompareTo(ra);
            });

            var chosen = new List<string>();
            var chosenDefs = new List<CardDefinition>();
            var validator = new CodexFormulaValidator(db);

            foreach (var (id, def) in suitCards)
            {
                chosen.Add(id);
                chosenDefs.Add(def);
                if (validator.ValidateDefs(chosenDefs, formula).ok)
                    return chosen;
            }

            return null;
        }

        public static bool IsSelectedSetValid(
            IReadOnlyList<CardDefinition> selectedDefs,
            CodexFormulaDefinition formula,
            ICardDatabase db)
        {
            if (selectedDefs == null || selectedDefs.Count == 0)
                return false;

            return new CodexFormulaValidator(db).ValidateDefs(selectedDefs, formula).ok;
        }

        /// <summary>
        /// Best possible rank sum from spread cards of the given suit, trying all ace (1/15) combos.
        /// </summary>
        public static int BestRankSumFromSpread(
            IReadOnlyList<(string id, CardDefinition def)> spreadCards,
            Suit suit)
        {
            var defs = new List<CardDefinition>();
            foreach (var (_, def) in spreadCards)
            {
                if (def.Suit == suit)
                    defs.Add(def);
            }
            return BestEffectiveRankSum(defs);
        }

        /// <summary>
        /// Best possible rank sum for a fixed card set, trying all ace (1/15) combos.
        /// </summary>
        public static int BestEffectiveRankSum(IReadOnlyList<CardDefinition> defs)
        {
            if (defs == null || defs.Count == 0)
                return 0;

            int baseSum = 0;
            int aceCount = 0;
            foreach (var def in defs)
            {
                if (def.Rank == Rank.Ace)
                    aceCount++;
                else
                    baseSum += (int)def.Rank;
            }

            if (aceCount == 0)
                return baseSum;

            int best = baseSum;
            int combos = 1 << aceCount;
            for (int mask = 0; mask < combos; mask++)
            {
                int total = baseSum;
                for (int bit = 0; bit < aceCount; bit++)
                    total += ((mask >> bit) & 1) == 1 ? 15 : 1;
                if (total > best)
                    best = total;
            }
            return best;
        }
    }
}
