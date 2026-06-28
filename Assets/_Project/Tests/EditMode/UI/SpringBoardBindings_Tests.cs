using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class SpringBoardBindings_Tests
    {
        const float ToleranceDeg = 0.001f;
        const float PlacementTolerancePct = 0.05f;

        [TestCase(ZodiacSign.Gemini, 0, -15f)]
        [TestCase(ZodiacSign.Virgo, 3, 75f)]
        [TestCase(ZodiacSign.Libra, 4, 105f)]
        [TestCase(ZodiacSign.Taurus, 11, 315f)]
        public void SegmentCenterAngleRad_MatchesOuterSegmentCenters(
            ZodiacSign sign, int expectedSlot, float expectedDeg)
        {
            Assert.AreEqual(expectedSlot, ZodiacWheelLayout.SlotForSign(sign));

            float actualDeg = ZodiacWheelLayout.SegmentCenterAngleRad(expectedSlot) * Mathf.Rad2Deg;
            Assert.AreEqual(expectedDeg, actualDeg, ToleranceDeg);

            float signDeg = ZodiacWheelLayout.SegmentCenterAngleRad(sign) * Mathf.Rad2Deg;
            Assert.AreEqual(expectedDeg, signDeg, ToleranceDeg);
        }

        [Test]
        public void SegmentCenterAngleRad_DoesNotUseLegacyMinus90Offset()
        {
            float virgoDeg = ZodiacWheelLayout.SegmentCenterAngleRad(ZodiacSign.Virgo) * Mathf.Rad2Deg;
            Assert.AreNotEqual(0f, virgoDeg, "Virgo must not sit at 12 o'clock (old -90° bug).");
            Assert.AreEqual(75f, virgoDeg, ToleranceDeg);
        }

        [Test]
        public void BindBoard_PlacesMeeplesAtOuterSegmentCenters()
        {
            var board = new VisualElement { name = "spring-board" };
            board.Add(new VisualElement { name = "spring-board-tokens" });

            var session = new GameSession("spring-board-test", GameMode.Quickplay, new List<PlayerState>
            {
                new(0, PlayerColor.Green) { CurrentSign = ZodiacSign.Virgo },
                new(1, PlayerColor.Red) { CurrentSign = ZodiacSign.Libra },
                new(2, PlayerColor.Blue) { CurrentSign = ZodiacSign.Taurus },
            });

            SpringBoardBindings.BindBoard(board, session);

            AssertTokenCenter(board, "meeple-0", ZodiacSign.Virgo, 43f);
            AssertTokenCenter(board, "meeple-1", ZodiacSign.Libra, 43f);
            AssertTokenCenter(board, "meeple-2", ZodiacSign.Taurus, 43f);
        }

        static void AssertTokenCenter(VisualElement board, string tokenName, ZodiacSign sign, float radiusPct)
        {
            var token = board.Q(tokenName);
            Assert.IsNotNull(token, $"Expected token '{tokenName}'.");

            float angleRad = ZodiacWheelLayout.SegmentCenterAngleRad(sign);
            float expectedCx = 50f + radiusPct * Mathf.Sin(angleRad);
            float expectedCy = 50f - radiusPct * Mathf.Cos(angleRad);

            Assert.AreEqual(expectedCx, token!.style.left.value.value, PlacementTolerancePct);
            Assert.AreEqual(expectedCy, token.style.top.value.value, PlacementTolerancePct);
        }
    }
}
