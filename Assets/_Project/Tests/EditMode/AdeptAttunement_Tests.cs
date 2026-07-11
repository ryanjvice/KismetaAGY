using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class AdeptAttunement_Tests
    {
        static CardDefinition AdeptDef(ZodiacSign sign) =>
            new CardDefinition(
                "test", Deck.Kismeta, Suit.None, Rank.None, CardVariant.One,
                MajorArcanaType.Adept, 8, sign, Planet.Jupiter,
                "Strength", "", "", "", false, "", -1, CrucibleGroup.None, "", ReagentCost.Zero);

        [Test]
        public void IsResonant_True_WhenSignMatches()
        {
            var player = new PlayerState(0, PlayerColor.Red) { CurrentSign = ZodiacSign.Sagittarius };
            Assert.IsTrue(AdeptAttunement.IsResonant(player, AdeptDef(ZodiacSign.Sagittarius)));
        }

        [Test]
        public void IsResonant_True_WhenHouseMatches()
        {
            var player = new PlayerState(0, PlayerColor.Red) { CurrentSign = ZodiacSign.Aries };
            player.AstralHouses.Add(ZodiacSign.Sagittarius);
            Assert.IsTrue(AdeptAttunement.IsResonant(player, AdeptDef(ZodiacSign.Sagittarius)));
        }

        [Test]
        public void IsResonant_False_WhenNeitherSignNorHouseMatches()
        {
            var player = new PlayerState(0, PlayerColor.Red) { CurrentSign = ZodiacSign.Aries };
            Assert.IsFalse(AdeptAttunement.IsResonant(player, AdeptDef(ZodiacSign.Sagittarius)));
        }

        [Test]
        public void IsAttuned_AliasMatchesIsResonant()
        {
            var player = new PlayerState(0, PlayerColor.Red) { CurrentSign = ZodiacSign.Sagittarius };
            var def = AdeptDef(ZodiacSign.Sagittarius);
            Assert.AreEqual(AdeptAttunement.IsResonant(player, def), AdeptAttunement.IsAttuned(player, def));
        }
    }
}
