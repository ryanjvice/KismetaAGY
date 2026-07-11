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
    public sealed class HouseModifierService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession SessionWithAce(string aceId, string defId, ZodiacSign sign)
        {
            var players = new List<PlayerState>
            {
                new PlayerState(0, PlayerColor.Red),
                new PlayerState(1, PlayerColor.Blue)
            };
            var session = new GameSession("test", GameMode.Standard, players, null);
            session.Players[0].CurrentSign = sign;
            session.RegisterCard(new CardInstance(aceId, defId, CardZone.Spread, 0));
            session.Players[0].Spread.Add(aceId);
            return session;
        }

        [Test]
        public void ValidateEntryFeePayment_WaterSignWithAceOfCups()
        {
            var db = LoadDb();
            var session = SessionWithAce("ace", "minor.cups.ace.1", ZodiacSign.Cancer);
            Assert.IsTrue(HouseModifierService.ValidateEntryFeePayment(
                session, 0, ZodiacSign.Cancer, new List<string> { "ace" }, db, out var error), error);
        }

        [Test]
        public void ValidateEntryFeePayment_RejectsWrongElement()
        {
            var db = LoadDb();
            var session = SessionWithAce("ace", "minor.cups.ace.1", ZodiacSign.Aries);
            Assert.IsFalse(HouseModifierService.ValidateEntryFeePayment(
                session, 0, ZodiacSign.Aries, new List<string> { "ace" }, db, out _));
        }
    }
}
