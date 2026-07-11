using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class AdeptResonantCatalog_Tests
    {
        [Test]
        public void AllAdepts_Classified()
        {
            Assert.AreEqual(AdeptResonantKind.ReversedNegation, AdeptResonantCatalog.KindFor(1));
            Assert.AreEqual(AdeptResonantKind.HandLimit7, AdeptResonantCatalog.KindFor(2));
            Assert.AreEqual(AdeptResonantKind.CraftTwoMarks, AdeptResonantCatalog.KindFor(3));
            Assert.AreEqual(AdeptResonantKind.BroadProtection, AdeptResonantCatalog.KindFor(4));
            Assert.AreEqual(AdeptResonantKind.ShiftPlusMinus2, AdeptResonantCatalog.KindFor(5));
            Assert.AreEqual(AdeptResonantKind.DuelReroll, AdeptResonantCatalog.KindFor(7));
            Assert.AreEqual(AdeptResonantKind.CombatDicePlus2, AdeptResonantCatalog.KindFor(8));
            Assert.AreEqual(AdeptResonantKind.DoubleElementAlign, AdeptResonantCatalog.KindFor(9));
            Assert.AreEqual(AdeptResonantKind.SaltWildCraft, AdeptResonantCatalog.KindFor(14));
            Assert.AreEqual(AdeptResonantKind.BanishAdept, AdeptResonantCatalog.KindFor(15));
            Assert.AreEqual(AdeptResonantKind.NullifyCard, AdeptResonantCatalog.KindFor(17));
            Assert.AreEqual(AdeptResonantKind.CrucibleWildcard, AdeptResonantCatalog.KindFor(21));
        }

        [Test]
        public void HasResonantLayer_TrueForAllCataloguedAdepts()
        {
            foreach (int arcana in new[] { 1, 2, 3, 4, 5, 7, 8, 9, 14, 15, 17, 21 })
                Assert.IsTrue(AdeptResonantCatalog.HasResonantLayer(arcana), $"arcana {arcana}");
        }
    }
}
