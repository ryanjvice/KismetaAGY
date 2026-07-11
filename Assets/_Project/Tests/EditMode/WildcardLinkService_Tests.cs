using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class WildcardLinkService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        [Test]
        public void WildcardLink_MatchesLinkedPlanet_Sun()
        {
            var db = LoadDb();
            var def = db.GetById("minor.cups.five.2");
            Assert.IsNotNull(def);
            Assert.IsTrue(WildcardLinkService.IsWildcardLink(def!));
            Assert.IsTrue(WildcardLinkService.MatchesPlanet(def!, Planet.Sun, db));
            Assert.IsFalse(WildcardLinkService.MatchesPlanet(def!, Planet.Mercury, db));
        }

        [Test]
        public void WildcardLink_MatchesLinkedPlanet_Empress()
        {
            var db = LoadDb();
            var def = db.GetById("minor.pentacles.three.2");
            Assert.IsNotNull(def);
            Assert.IsTrue(WildcardLinkService.MatchesPlanet(def!, Planet.Venus, db));
            Assert.IsFalse(WildcardLinkService.MatchesPlanet(def!, Planet.Jupiter, db));
        }

        [Test]
        public void WildcardLink_NativePlanetStillCounts()
        {
            var db = LoadDb();
            var def = db.GetById("minor.wands.five.1");
            Assert.IsNotNull(def);
            Assert.IsFalse(WildcardLinkService.IsWildcardLink(def!));
            Assert.IsTrue(WildcardLinkService.MatchesPlanet(def!, Planet.Mercury, db));
            Assert.IsFalse(WildcardLinkService.MatchesPlanet(def!, Planet.Sun, db));
        }

        [Test]
        public void MatchesLinkedArcana_TrueWhenLinkMatches()
        {
            var db = LoadDb();
            var def = db.GetById("minor.cups.five.2");
            Assert.IsTrue(WildcardLinkService.MatchesLinkedArcana(def!, 19));
            Assert.IsFalse(WildcardLinkService.MatchesLinkedArcana(def!, 1));
        }

        [Test]
        public void CardDatabase_LoadsWildcardArcanaMajorName()
        {
            var db = LoadDb();
            var def = db.GetById("minor.cups.five.2");
            Assert.IsNotNull(def);
            Assert.AreEqual("The Sun", def!.WildcardArcanaMajorName);
        }

        [Test]
        public void GetByArcanaNumber_ResolvesMajor()
        {
            var db = LoadDb();
            var sun = db.GetByArcanaNumber(19);
            Assert.IsNotNull(sun);
            Assert.IsTrue(sun!.Name.Contains("Sun"));
        }
    }
}
