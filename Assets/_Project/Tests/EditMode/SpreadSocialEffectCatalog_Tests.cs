using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class SpreadSocialEffectCatalog_Tests
    {
        static CardDefinition Nine(Suit suit) =>
            new CardDefinition(
                id: $"test.nine.{suit}",
                deck: Deck.Kismeta,
                suit: suit,
                rank: Rank.Nine,
                variant: CardVariant.One,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: Planet.Venus,
                name: "Nine",
                effectType: "Social",
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
        public void TryGetDrawOnSuccess_MapsSuitToContest()
        {
            Assert.IsTrue(SpreadSocialEffectCatalog.TryGetDrawOnSuccess(
                Nine(Suit.Cups), ContestKind.Trade, out int cups));
            Assert.AreEqual(1, cups);

            Assert.IsTrue(SpreadSocialEffectCatalog.TryGetDrawOnSuccess(
                Nine(Suit.Swords), ContestKind.Duel, out int swords));
            Assert.AreEqual(2, swords);

            Assert.IsFalse(SpreadSocialEffectCatalog.TryGetDrawOnSuccess(
                Nine(Suit.Cups), ContestKind.Duel, out _));
        }
    }
}
