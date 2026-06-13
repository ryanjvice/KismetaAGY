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
    /// <summary>
    /// Unit tests for CodexFormulaValidator — isolated from rule services and session state.
    /// Tests both AnyThreePlanet and RankSum formula types including Ace edge cases.
    /// </summary>
    public sealed class CodexFormulaValidator_Tests
    {
        private CardDatabase    _db     = null!;
        private CodexFormulaValidator _sut = null!;

        [SetUp]
        public void SetUp()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            Assert.IsTrue(File.Exists(path), $"cards.json not found at {path}.");
            _db  = CardDatabase.LoadFromJson(File.ReadAllText(path));
            _sut = new CodexFormulaValidator(_db);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static CodexFormulaDefinition MakePlanetFormula(Planet planet, int slotIndex = 0) =>
            new CodexFormulaDefinition(
                codex: CodexVariant.A, slotIndex: slotIndex,
                cauldron: "Red", cauldronSuit: Suit.Wands,
                formulaType: CodexFormulaType.AnyThreePlanet,
                requiredPlanet: planet, requiredSuit: Suit.None,
                minRankSum: 0, displayName: $"Any Three {planet}");

        private static CodexFormulaDefinition MakeRankSumFormula(Suit suit, int minSum = 25) =>
            new CodexFormulaDefinition(
                codex: CodexVariant.B, slotIndex: 0,
                cauldron: "Green", cauldronSuit: suit,
                formulaType: CodexFormulaType.RankSum,
                requiredPlanet: Planet.None, requiredSuit: suit,
                minRankSum: minSum, displayName: $"25 Total Ranks · {suit}");

        /// <summary>Builds a fake CardDefinition with the given planet (Crucible-style, deck-agnostic).</summary>
        private static CardDefinition FakeDef(string id, Planet planet, Suit suit = Suit.None, Rank rank = Rank.None) =>
            new CardDefinition(
                id: id, deck: Deck.Kismeta,
                suit: suit, rank: rank,
                variant: CardVariant.One,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: planet,
                effectType: "", effectText: "",
                wildcardArcanaNumber: -1,
                crucibleGroup: CrucibleGroup.None,
                alchemicalFormula: "",
                alchemicalCost: ReagentCost.Zero);

        // ─── AnyThreePlanet ───────────────────────────────────────────────────────

        [Test]
        public void Planet_ExactlyThreeMars_Succeeds()
        {
            var formula = MakePlanetFormula(Planet.Mars);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.Mars),
                FakeDef("b", Planet.Mars),
                FakeDef("c", Planet.Mars)
            };
            var (ok, reason) = _sut.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void Planet_TwoMars_Fails()
        {
            var formula = MakePlanetFormula(Planet.Mars);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.Mars),
                FakeDef("b", Planet.Mars)
            };
            var (ok, _) = _sut.ValidateDefs(defs, formula);
            Assert.IsFalse(ok, "Two cards should not satisfy a three-planet formula.");
        }

        [Test]
        public void Planet_FourMars_Fails()
        {
            var formula = MakePlanetFormula(Planet.Mars);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.Mars),
                FakeDef("b", Planet.Mars),
                FakeDef("c", Planet.Mars),
                FakeDef("d", Planet.Mars)
            };
            var (ok, _) = _sut.ValidateDefs(defs, formula);
            Assert.IsFalse(ok, "Four cards should not satisfy an exactly-three formula.");
        }

        [Test]
        public void Planet_WrongPlanet_Fails()
        {
            var formula = MakePlanetFormula(Planet.Mars);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.Mars),
                FakeDef("b", Planet.Mars),
                FakeDef("c", Planet.Venus) // wrong planet
            };
            var (ok, _) = _sut.ValidateDefs(defs, formula);
            Assert.IsFalse(ok, "Mismatched planet should fail.");
        }

        [Test]
        public void Planet_EmptyList_Fails()
        {
            var formula = MakePlanetFormula(Planet.Mars);
            var (ok, _) = _sut.ValidateDefs(new List<CardDefinition>(), formula);
            Assert.IsFalse(ok);
        }

        // ─── RankSum ──────────────────────────────────────────────────────────────

        [Test]
        public void RankSum_ThreeWands_SufficientSum_Succeeds()
        {
            var formula = MakeRankSumFormula(Suit.Wands, minSum: 25);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Wands, Rank.King),   // 14
                FakeDef("b", Planet.None, Suit.Wands, Rank.King),   // 14
                FakeDef("c", Planet.None, Suit.Wands, Rank.Two)     // 2  => 30 >= 25 ✓
            };
            var (ok, reason) = _sut.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void RankSum_ThreeWands_InsufficientSum_Fails()
        {
            var formula = MakeRankSumFormula(Suit.Wands, minSum: 25);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Wands, Rank.Two),    // 2
                FakeDef("b", Planet.None, Suit.Wands, Rank.Three),  // 3
                FakeDef("c", Planet.None, Suit.Wands, Rank.Four)    // 4 => 9 < 25 ✗
            };
            var (ok, _) = _sut.ValidateDefs(defs, formula);
            Assert.IsFalse(ok, "Sum of 9 should not meet the 25-point threshold.");
        }

        [Test]
        public void RankSum_WrongSuit_Fails()
        {
            var formula = MakeRankSumFormula(Suit.Wands, minSum: 25);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Cups, Rank.King),    // wrong suit
                FakeDef("b", Planet.None, Suit.Wands, Rank.King),
                FakeDef("c", Planet.None, Suit.Wands, Rank.King)
            };
            var (ok, _) = _sut.ValidateDefs(defs, formula);
            Assert.IsFalse(ok, "Mixed suits should fail the RankSum formula.");
        }

        [Test]
        public void RankSum_OneCardExactlyAtThreshold_Succeeds()
        {
            var formula = MakeRankSumFormula(Suit.Swords, minSum: 10);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Swords, Rank.Ten)    // 10 >= 10 ✓
            };
            var (ok, reason) = _sut.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void RankSum_AceAs15_MeetsThreshold()
        {
            var formula = MakeRankSumFormula(Suit.Wands, minSum: 25);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Wands, Rank.Ace),    // 15 as high
                FakeDef("b", Planet.None, Suit.Wands, Rank.King),   // 14 => 29 >= 25 ✓
            };
            var (ok, reason) = _sut.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void RankSum_AceAs1_DoesNotMeetThreshold_AceAs15_Does()
        {
            var formula = MakeRankSumFormula(Suit.Wands, minSum: 25);
            // Ace(1 or 15) + Ten(10) + Nine(9) = 20 or 34.
            // As 1: 1+10+9=20 < 25. As 15: 15+10+9=34 >= 25. Should succeed.
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Wands, Rank.Ace),
                FakeDef("b", Planet.None, Suit.Wands, Rank.Ten),
                FakeDef("c", Planet.None, Suit.Wands, Rank.Nine)
            };
            var (ok, reason) = _sut.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void RankSum_TwoAces_BothAs1_Fails_BothAs15_Succeeds()
        {
            var formula = MakeRankSumFormula(Suit.Cups, minSum: 25);
            // Ace + Ace + Two = (1+1+2)=4, (15+1+2)=18, (1+15+2)=18, (15+15+2)=32 >= 25. Should succeed.
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Cups, Rank.Ace),
                FakeDef("b", Planet.None, Suit.Cups, Rank.Ace),
                FakeDef("c", Planet.None, Suit.Cups, Rank.Two)
            };
            var (ok, reason) = _sut.ValidateDefs(defs, formula);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void RankSum_AllAcesAsLow_CannotReachThreshold_Fails()
        {
            // 3 Aces: best = 15+15+15=45 >= 25, so this should actually succeed.
            // Use a threshold of 50 to force failure.
            var formula = MakeRankSumFormula(Suit.Swords, minSum: 50);
            var defs = new List<CardDefinition>
            {
                FakeDef("a", Planet.None, Suit.Swords, Rank.Ace),   // max 15
                FakeDef("b", Planet.None, Suit.Swords, Rank.Ace),   // max 15
                FakeDef("c", Planet.None, Suit.Swords, Rank.Ace)    // max 15 => 45 < 50
            };
            var (ok, _) = _sut.ValidateDefs(defs, formula);
            Assert.IsFalse(ok, "Even with best Ace values, 45 < 50 should fail.");
        }
    }
}
