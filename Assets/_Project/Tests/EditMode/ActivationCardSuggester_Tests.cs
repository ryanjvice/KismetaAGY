using System.Collections.Generic;
using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class ActivationCardSuggester_Tests
    {
        private CardDatabase _db = null!;
        private CodexFormulaValidator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            Assert.IsTrue(File.Exists(path), $"cards.json not found at {path}.");
            _db = CardDatabase.LoadFromJson(File.ReadAllText(path));
            _validator = new CodexFormulaValidator(_db);
        }

        private static CodexFormulaDefinition MakePlanetFormula(Planet planet) =>
            new CodexFormulaDefinition(
                codex: CodexVariant.A, slotIndex: 0,
                cauldron: "Red", cauldronSuit: Suit.Wands,
                formulaType: CodexFormulaType.AnyThreePlanet,
                requiredPlanet: planet, requiredSuit: Suit.None,
                minRankSum: 0, displayName: $"Any Three {planet}");

        private static CodexFormulaDefinition MakeRankSumFormula(Suit suit, int minSum = 25) =>
            new CodexFormulaDefinition(
                codex: CodexVariant.B, slotIndex: 1,
                cauldron: "Blue", cauldronSuit: suit,
                formulaType: CodexFormulaType.RankSum,
                requiredPlanet: Planet.None, requiredSuit: suit,
                minRankSum: minSum, displayName: $"25 Total Ranks · {suit}");

        private static CardDefinition FakeDef(string id, Planet planet, Suit suit = Suit.None, Rank rank = Rank.None) =>
            new CardDefinition(
                id: id, deck: Deck.Kismeta,
                suit: suit, rank: rank,
                variant: CardVariant.One,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: planet,
                name: "",
                effectType: "", effectText: "", effectTextResonant: "",
                isCurse: false, nullifiesCard: "",
                wildcardArcanaNumber: -1,
                crucibleGroup: CrucibleGroup.None,
                alchemicalFormula: "",
                alchemicalCost: ReagentCost.Zero);

        private static List<(string id, CardDefinition def)> Spread(params (string id, CardDefinition def)[] cards) =>
            new List<(string id, CardDefinition def)>(cards);

        [Test]
        public void CanSpreadSatisfy_RankSum_CupsWithFortyPoints_ReturnsTrue()
        {
            var formula = MakeRankSumFormula(Suit.Cups);
            var spread = Spread(
                ("a", FakeDef("a", Planet.None, Suit.Cups, Rank.Princess)),
                ("b", FakeDef("b", Planet.None, Suit.Cups, Rank.Three)),
                ("c", FakeDef("c", Planet.None, Suit.Cups, Rank.Princess)),
                ("d", FakeDef("d", Planet.None, Suit.Cups, Rank.Ace)));

            Assert.IsTrue(ActivationCardSuggester.CanSpreadSatisfyFormula(spread, formula));
            Assert.GreaterOrEqual(
                ActivationCardSuggester.BestRankSumFromSpread(spread, Suit.Cups), 25);
        }

        [Test]
        public void SuggestActivationCards_RankSum_Cups_ReturnsValidMinimalSubset()
        {
            var formula = MakeRankSumFormula(Suit.Cups);
            var spread = Spread(
                ("a", FakeDef("a", Planet.None, Suit.Cups, Rank.Princess)),
                ("b", FakeDef("b", Planet.None, Suit.Cups, Rank.Three)),
                ("c", FakeDef("c", Planet.None, Suit.Cups, Rank.Princess)),
                ("d", FakeDef("d", Planet.None, Suit.Cups, Rank.Ace)));

            var suggested = ActivationCardSuggester.SuggestActivationCards(spread, formula, _db);
            Assert.IsNotNull(suggested);
            Assert.Greater(suggested!.Count, 0);

            var defs = new List<CardDefinition>();
            foreach (var id in suggested)
            {
                var def = spread.Find(x => x.id == id).def;
                defs.Add(def);
            }

            var (ok, reason) = _validator.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void CanSpreadSatisfy_AnyThreePlanet_ThreeMatching_ReturnsTrue()
        {
            var formula = MakePlanetFormula(Planet.Jupiter);
            var spread = Spread(
                ("a", FakeDef("a", Planet.Jupiter)),
                ("b", FakeDef("b", Planet.Jupiter)),
                ("c", FakeDef("c", Planet.Jupiter)),
                ("d", FakeDef("d", Planet.Mars)));

            Assert.IsTrue(ActivationCardSuggester.CanSpreadSatisfyFormula(spread, formula));
        }

        [Test]
        public void SuggestActivationCards_AnyThreePlanet_ReturnsExactlyThree()
        {
            var formula = MakePlanetFormula(Planet.Jupiter);
            var spread = Spread(
                ("a", FakeDef("a", Planet.Jupiter)),
                ("b", FakeDef("b", Planet.Jupiter)),
                ("c", FakeDef("c", Planet.Jupiter)),
                ("d", FakeDef("d", Planet.Mars)));

            var suggested = ActivationCardSuggester.SuggestActivationCards(spread, formula, _db);
            Assert.IsNotNull(suggested);
            Assert.AreEqual(3, suggested!.Count);
        }

        [Test]
        public void CanSpreadSatisfy_InsufficientSpread_ReturnsFalse()
        {
            var formula = MakeRankSumFormula(Suit.Wands);
            var spread = Spread(
                ("a", FakeDef("a", Planet.None, Suit.Wands, Rank.Two)),
                ("b", FakeDef("b", Planet.None, Suit.Wands, Rank.Three)));

            Assert.IsFalse(ActivationCardSuggester.CanSpreadSatisfyFormula(spread, formula));
            Assert.IsNull(ActivationCardSuggester.SuggestActivationCards(spread, formula, _db));
        }

        [Test]
        public void BestEffectiveRankSum_TwoAces_UsesBestAceCombo()
        {
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Cups, Rank.Ace),
                FakeDef("b", Planet.None, Suit.Cups, Rank.Ace),
                FakeDef("c", Planet.None, Suit.Cups, Rank.Two)
            };

            Assert.AreEqual(32, ActivationCardSuggester.BestEffectiveRankSum(defs));
        }
    }
}
