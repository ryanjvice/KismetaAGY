using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class SpreadLimitEffectCatalog_Tests
    {
        static CardDefinition QueenV2(Suit suit) =>
            new CardDefinition(
                id: $"test.queen.{suit}",
                deck: Deck.Kismeta,
                suit: suit,
                rank: Rank.Queen,
                variant: CardVariant.Two,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: Planet.None,
                name: "Queen V2",
                effectType: "Passive",
                effectText: "",
                effectTextResonant: "",
                isCurse: false,
                nullifiesCard: "",
                wildcardArcanaNumber: -1,
                wildcardArcanaMajorName: "",
                crucibleGroup: CrucibleGroup.None,
                alchemicalFormula: "",
                alchemicalCost: ReagentCost.Zero);

        [Test]
        public void IsQueenV2Passive_TrueForQueenVariantTwo()
        {
            Assert.IsTrue(SpreadLimitEffectCatalog.IsQueenV2Passive(QueenV2(Suit.Cups)));
        }

        [Test]
        public void GrantsHandBonus_CupsAndPentacles()
        {
            Assert.IsTrue(SpreadLimitEffectCatalog.GrantsHandBonus(QueenV2(Suit.Cups)));
            Assert.IsTrue(SpreadLimitEffectCatalog.GrantsHandBonus(QueenV2(Suit.Pentacles)));
            Assert.IsFalse(SpreadLimitEffectCatalog.GrantsHandBonus(QueenV2(Suit.Swords)));
        }

        [Test]
        public void GrantsSpreadBonus_SwordsAndWands()
        {
            Assert.IsTrue(SpreadLimitEffectCatalog.GrantsSpreadBonus(QueenV2(Suit.Swords)));
            Assert.IsTrue(SpreadLimitEffectCatalog.GrantsSpreadBonus(QueenV2(Suit.Wands)));
            Assert.IsFalse(SpreadLimitEffectCatalog.GrantsSpreadBonus(QueenV2(Suit.Cups)));
        }
    }
}
