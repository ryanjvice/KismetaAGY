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
    public sealed class AlchemicalAlignmentValidator_Tests
    {
        private CardDatabase _db = null!;
        private AlchemicalAlignmentValidator _sut = null!;

        [SetUp]
        public void SetUp()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            Assert.IsTrue(File.Exists(path), $"cards.json not found at {path}.");
            _db = CardDatabase.LoadFromJson(File.ReadAllText(path));
            _sut = new AlchemicalAlignmentValidator();
        }

        static List<CardDefinition> Defs(params CardDefinition[] cards) => new(cards);

        [Test]
        public void Alignment_SunWildcardInFireFormula()
        {
            var cups2 = _db.GetById("minor.cups.two.1");
            var cups3 = _db.GetById("minor.cups.three.1");
            var cups4 = _db.GetById("minor.cups.four.1");
            var sunWild = _db.GetById("minor.cups.five.2");
            Assert.NotNull(cups2);
            Assert.NotNull(cups3);
            Assert.NotNull(cups4);
            Assert.NotNull(sunWild);

            var cards = Defs(cups2!, cups3!, cups4!, sunWild!);
            var (ok, reason) = _sut.Validate(
                "Three Cups (any ranks) + one Sun card (Ace)", cards, _db);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void Alignment_SunWildcardFailsWithoutDb()
        {
            var cups2 = _db.GetById("minor.cups.two.1");
            var cups3 = _db.GetById("minor.cups.three.1");
            var cups4 = _db.GetById("minor.cups.four.1");
            var sunWild = _db.GetById("minor.cups.five.2");
            var cards = Defs(cups2!, cups3!, cups4!, sunWild!);

            var (ok, _) = _sut.Validate(
                "Three Cups (any ranks) + one Sun card (Ace)", cards);
            Assert.IsFalse(ok, "Without card database, linked Sun planet should not match.");
        }

        [Test]
        public void Alignment_MercuryWildcardInPlanetPair()
        {
            var fiveMercury = _db.GetById("minor.cups.five.1");
            var magicianWild = _db.GetById("minor.wands.three.2");
            Assert.NotNull(fiveMercury);
            Assert.NotNull(magicianWild);

            // Planet pair requires same rank; use a fake rank-five Magician wildcard for the second card.
            var wildFive = new CardDefinition(
                id: "test.magician.wild.five",
                deck: Deck.Kismeta,
                suit: Suit.Wands,
                rank: Rank.Five,
                variant: CardVariant.Two,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: Planet.Jupiter,
                name: "Test Magician Wild Five",
                effectType: "WildcardLink",
                effectText: "",
                effectTextResonant: "",
                isCurse: false,
                nullifiesCard: "",
                wildcardArcanaNumber: 1,
                wildcardArcanaMajorName: "The Magician",
                crucibleGroup: CrucibleGroup.None,
                alchemicalFormula: "",
                alchemicalCost: ReagentCost.Zero);

            var cards = Defs(fiveMercury!, wildFive);
            var (ok, reason) = _sut.Validate(
                "Pair of Mercury planets (5s + Princess)", cards, _db);
            Assert.IsTrue(ok, reason);
        }

        [Test]
        public void Alignment_NativePlanetCardsStillWork()
        {
            var a = _db.GetById("minor.cups.seven.1");
            var b = _db.GetById("minor.wands.seven.1");
            var c = _db.GetById("minor.pentacles.seven.1");
            var d = _db.GetById("minor.swords.two.1");
            var e = _db.GetById("minor.wands.two.1");
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotNull(c);
            Assert.NotNull(d);
            Assert.NotNull(e);

            var cards = Defs(a!, b!, c!, d!, e!);
            var (ok, reason) = _sut.Validate(
                "Three Mars planets (7s + Knights) + Two Cups", cards, _db);
            Assert.IsFalse(ok, "Missing cups segment should fail.");
            StringAssert.Contains("Cups", reason);
        }
    }
}
