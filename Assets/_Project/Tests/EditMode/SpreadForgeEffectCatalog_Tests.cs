using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class SpreadForgeEffectCatalog_Tests
    {
        static CardDefinition ForgeCard(Rank rank, CardVariant variant, Suit suit, string effectType) =>
            new CardDefinition(
                id: $"test.{rank}.{suit}",
                deck: Deck.Kismeta,
                suit: suit,
                rank: rank,
                variant: variant,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: Planet.None,
                name: "Test",
                effectType: effectType,
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
        public void IsRank7ForgeReagent_TrueForSevenV1Forge()
        {
            Assert.IsTrue(SpreadForgeEffectCatalog.IsRank7ForgeReagent(
                ForgeCard(Rank.Seven, CardVariant.One, Suit.Cups, "Forge")));
        }

        [Test]
        public void IsQueenV1WildReagent_TrueForQueenV1Forge()
        {
            Assert.IsTrue(SpreadForgeEffectCatalog.IsQueenV1WildReagent(
                ForgeCard(Rank.Queen, CardVariant.One, Suit.Swords, "Forge")));
        }

        [Test]
        public void ReagentMappings_MatchSuitCorrespondence()
        {
            var seven = ForgeCard(Rank.Seven, CardVariant.One, Suit.Cups, "Forge");
            var queen = ForgeCard(Rank.Queen, CardVariant.One, Suit.Swords, "Forge");
            Assert.AreEqual(ReagentType.AquaRegia, SpreadForgeEffectCatalog.ReagentForRank7(seven));
            Assert.AreEqual(ReagentType.Quicksilver, SpreadForgeEffectCatalog.WildReagentForQueen(queen));
        }
    }
}
