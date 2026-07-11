using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class ReversedCurseCatalog_Tests
    {
        static readonly string[] CurseCardIds =
        {
            "minor.cups.four.1",
            "minor.cups.five.1",
            "minor.cups.six.1",
            "minor.pentacles.four.1",
            "minor.pentacles.five.1",
            "minor.pentacles.six.1",
            "minor.swords.four.1",
            "minor.swords.five.1",
            "minor.swords.six.1",
            "minor.wands.four.1",
            "minor.wands.five.1",
            "minor.wands.six.1"
        };

        [Test]
        public void AllRankFourToSixV1Curses_AreClassified()
        {
            foreach (var id in CurseCardIds)
            {
                Assert.AreNotEqual(ReversedCurseKind.None, ReversedCurseCatalog.KindFor(id), id);
                Assert.IsTrue(ReversedCurseCatalog.IsReversedCurseCard(id), id);
            }
        }

        [Test]
        public void NonCurseCards_ReturnNone()
        {
            Assert.AreEqual(ReversedCurseKind.None, ReversedCurseCatalog.KindFor("minor.cups.ace.1"));
            Assert.IsFalse(ReversedCurseCatalog.IsReversedCurseCard("minor.cups.ace.1"));
        }
    }
}
