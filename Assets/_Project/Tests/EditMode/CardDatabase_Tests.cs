using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    /// <summary>
    /// Verifies that the JSON data pipeline produces a well-formed CardDatabase.
    /// Loads cards.json directly from the Generated folder (bypassing Resources.Load)
    /// so these tests run in Edit Mode without entering Play Mode.
    /// </summary>
    public sealed class CardDatabase_Tests
    {
        private CardDatabase _db = null!;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            Assert.IsTrue(File.Exists(path), $"cards.json not found at {path}. Run 'npm run data:sync'.");
            var json = File.ReadAllText(path);
            _db = CardDatabase.LoadFromJson(json);
        }

        [Test]
        public void Loads_All_156_Cards()
        {
            Assert.AreEqual(CardDatabase.ExpectedCardCount, _db.Count,
                "Expected 156 cards total (22 Major Arcana + 22 Crucible + 112 Minor Arcana).");
        }

        [Test]
        public void MajorArcana_Count_Is_22()
        {
            int count = 0;
            foreach (var def in _db.All.Values)
                if (def.IsMajorArcana) count++;
            Assert.AreEqual(22, count, "Major Arcana should be exactly 22 (10 Fates + 12 Adepts).");
        }

        [Test]
        public void Crucible_Count_Is_22()
        {
            int count = 0;
            foreach (var def in _db.All.Values)
                if (def.IsCrucible) count++;
            Assert.AreEqual(22, count, "Crucible deck should be exactly 22 cards.");
        }

        [Test]
        public void MinorArcana_Count_Is_112()
        {
            int count = 0;
            foreach (var def in _db.All.Values)
                if (def.IsMinorArcana) count++;
            Assert.AreEqual(112, count, "Minor Arcana should be 4 suits × 14 ranks × 2 variants = 112.");
        }

        [Test]
        public void Each_Suit_Has_28_Minor_Arcana_Cards()
        {
            foreach (Suit suit in new[] { Suit.Wands, Suit.Cups, Suit.Pentacles, Suit.Swords })
            {
                int count = 0;
                foreach (var def in _db.GetBySuit(suit))
                    count++;
                Assert.AreEqual(28, count, $"{suit} should have 14 ranks × 2 variants = 28 cards.");
            }
        }

        [Test]
        public void Crucible_GroupA_Has_4_Cards()
        {
            int count = 0;
            foreach (var _ in _db.GetCrucibleByGroup(CrucibleGroup.A))
                count++;
            Assert.AreEqual(4, count, "Crucible Group A should have 4 cards.");
        }

        [Test]
        public void Known_Card_Id_Resolves_Correctly()
        {
            var def = _db.GetById("minor.cups.seven.1");
            Assert.IsNotNull(def, "Card 'minor.cups.seven.1' should exist.");
            Assert.AreEqual(Suit.Cups, def!.Suit);
            Assert.AreEqual(Rank.Seven, def.Rank);
        }

        [Test]
        public void All_Adept_Cards_Have_Sign()
        {
            foreach (var def in _db.All.Values)
            {
                if (def.IsMajorArcana && def.MajorArcanaType == MajorArcanaType.Adept)
                    Assert.AreNotEqual(ZodiacSign.None, def.Sign,
                        $"Adept card '{def.Id}' is missing its Zodiac Sign.");
            }
        }
    }
}
