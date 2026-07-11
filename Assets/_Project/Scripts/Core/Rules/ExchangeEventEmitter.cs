using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Builds and emits <see cref="PlayerExchangeEvent"/> from rule services.</summary>
    public static class ExchangeEventEmitter
    {
        public static void EmitTrade(GameSession session, int initiatorId, int targetId,
            IReadOnlyList<string> offerCardIds, IReadOnlyList<string> requestCardIds)
        {
            var legs = new List<ExchangeLeg>();
            if (offerCardIds.Count > 0)
            {
                var items = new List<ExchangeItem>(offerCardIds.Count);
                foreach (var id in offerCardIds)
                    items.Add(ExchangeItem.Spread(id));
                legs.Add(new ExchangeLeg(initiatorId, targetId, items));
            }
            if (requestCardIds.Count > 0)
            {
                var items = new List<ExchangeItem>(requestCardIds.Count);
                foreach (var id in requestCardIds)
                    items.Add(ExchangeItem.Spread(id));
                legs.Add(new ExchangeLeg(targetId, initiatorId, items));
            }
            if (legs.Count == 0) return;

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.Trade, legs));
        }

        public static void EmitDuel(GameSession session, int attackerId, int defenderId,
            int attackRoll, int defendRoll, int winnerId, string targetCardId, string? anteCardId,
            int attackerRoundWins = 1, int defenderRoundWins = 0)
        {
            var legs = new List<ExchangeLeg>();
            if (winnerId == attackerId)
            {
                legs.Add(new ExchangeLeg(defenderId, attackerId,
                    new[] { ExchangeItem.Spread(targetCardId) }));
            }
            else if (!string.IsNullOrEmpty(anteCardId))
            {
                legs.Add(new ExchangeLeg(attackerId, PlayerExchangeEvent.SinkDiscard,
                    new[] { ExchangeItem.Discard(anteCardId) }));
                legs.Add(new ExchangeLeg(defenderId, defenderId,
                    new[] { ExchangeItem.Spread(targetCardId) }));
            }

            string context = winnerId == attackerId
                ? $"Duel — challenger wins ({attackerRoundWins}-{defenderRoundWins})"
                : $"Duel — defender wins ({defenderRoundWins}-{attackerRoundWins})";

            string modifierNote = ContestModifierService.DescribeExchangeContext(
                session, ContestKind.Duel, attackerId, defenderId);
            if (!string.IsNullOrWhiteSpace(modifierNote))
                context += $" · {modifierNote}";

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.Duel, legs, context));
        }

        public static void EmitGambit(GameSession session, int attackerId, int defenderId,
            int attackRoll, int defendRoll, int winnerId, string offeredCardId,
            bool offeredInCrucible, string? arrestedDefenderCardId,
            int attackerRoundWins = 1, int defenderRoundWins = 0)
        {
            var legs = new List<ExchangeLeg>();
            if (winnerId == attackerId && arrestedDefenderCardId != null)
            {
                var arrested = new[] { ExchangeItem.ArrestedCrucible(arrestedDefenderCardId) };
                legs.Add(new ExchangeLeg(defenderId, defenderId, arrested));
                legs.Add(new ExchangeLeg(attackerId, attackerId, arrested));
            }
            else if (winnerId == defenderId)
            {
                if (offeredInCrucible)
                {
                    var arrested = new[] { ExchangeItem.ArrestedCrucible(offeredCardId) };
                    legs.Add(new ExchangeLeg(attackerId, attackerId, arrested));
                    legs.Add(new ExchangeLeg(defenderId, defenderId, arrested));
                }
                else
                    legs.Add(new ExchangeLeg(attackerId, PlayerExchangeEvent.SinkDiscard,
                        new[] { ExchangeItem.Discard(offeredCardId) }));
            }

            string context = winnerId == attackerId
                ? $"Gambit — challenger wins ({attackerRoundWins}-{defenderRoundWins})"
                : $"Gambit — defender wins ({defenderRoundWins}-{attackerRoundWins})";

            string modifierNote = ContestModifierService.DescribeExchangeContext(
                session, ContestKind.Gambit, attackerId, defenderId);
            if (!string.IsNullOrWhiteSpace(modifierNote))
                context += $" · {modifierNote}";

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.Gambit, legs, context));
        }

        public static void EmitFoolGift(GameSession session, int drawerId, int recipientId, ReagentType reagentType)
        {
            var legs = new List<ExchangeLeg>
            {
                new(drawerId, recipientId, new[] { ExchangeItem.Reagent(reagentType) })
            };
            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateFool, legs,
                "The Fool — opponent receives a reagent"));
        }

        public static void EmitLoversChoice(GameSession session, int drawerId, int chooserId,
            bool drawCards, ReagentType chosenReagent, IReadOnlyList<string>? drawnCardIds = null)
        {
            var legs = new List<ExchangeLeg>();
            if (drawCards && drawnCardIds != null && drawnCardIds.Count > 0)
            {
                var items = new List<ExchangeItem>(drawnCardIds.Count);
                foreach (var id in drawnCardIds)
                    items.Add(ExchangeItem.Spread(id));
                legs.Add(new ExchangeLeg(chooserId, drawerId, items));
            }
            else if (drawCards)
            {
                session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateLovers,
                    System.Array.Empty<ExchangeLeg>(),
                    "The Lovers — rival chose draw two, but no cards were available"));
                return;
            }
            else if (!drawCards)
            {
                legs.Add(new ExchangeLeg(chooserId, drawerId,
                    new[] { ExchangeItem.Reagent(chosenReagent) }));
            }

            if (legs.Count == 0) return;

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateLovers, legs,
                "The Lovers — rival chose your reward"));
        }

        public static void EmitHangedMan(GameSession session, IReadOnlyList<List<string>> handsBeforePass)
        {
            int n = session.Players.Count;
            var legs = new List<ExchangeLeg>();
            for (int i = 0; i < n; i++)
            {
                int target = (i + 1) % n;
                var hand = handsBeforePass[i];
                if (hand.Count == 0) continue;

                var items = new List<ExchangeItem>();
                int show = System.Math.Min(hand.Count, 4);
                for (int c = 0; c < show; c++)
                    items.Add(ExchangeItem.Spread(hand[c]));
                if (hand.Count > show)
                    items.Add(new ExchangeItem(ExchangeItemKind.SpreadCard, count: hand.Count - show));

                legs.Add(new ExchangeLeg(i, target, items));
            }

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateHangedMan, legs,
                "The Hanged Man — hands passed clockwise"));
        }

        public static void EmitTower(GameSession session, int drawerId,
            IReadOnlyList<(int playerId, IReadOnlyList<string> arrestedIds)> arrests)
        {
            var legs = new List<ExchangeLeg>();
            foreach (var (playerId, ids) in arrests)
            {
                if (ids.Count == 0) continue;
                var items = new List<ExchangeItem>(ids.Count);
                foreach (var id in ids)
                    items.Add(ExchangeItem.Adept(id));
                legs.Add(new ExchangeLeg(playerId, playerId, items));
            }

            if (legs.Count == 0) return;
            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateTower, legs,
                "The Tower — all Adepts arrested"));
        }

        public static void EmitDeath(GameSession session,
            IReadOnlyList<(int playerId, IReadOnlyList<string> handCardIds)> hands)
        {
            var legs = new List<ExchangeLeg>();
            foreach (var (playerId, cards) in hands)
            {
                if (cards.Count == 0) continue;
                var items = new List<ExchangeItem>();
                int show = System.Math.Min(cards.Count, 4);
                for (int c = 0; c < show; c++)
                    items.Add(ExchangeItem.Discard(cards[c]));
                if (cards.Count > show)
                    items.Add(new ExchangeItem(ExchangeItemKind.ToDiscard, count: cards.Count - show));
                legs.Add(new ExchangeLeg(playerId, PlayerExchangeEvent.SinkDeck, items));
            }

            if (legs.Count == 0) return;
            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateDeath, legs,
                "Death — all hands returned to the deck"));
        }

        public static void EmitSun(GameSession session)
        {
            var reagentTypes = new[]
            {
                ReagentType.Salt, ReagentType.Sulphur, ReagentType.AquaRegia,
                ReagentType.Vitriol, ReagentType.Quicksilver
            };
            var legs = new List<ExchangeLeg>();
            foreach (var player in session.Players)
            {
                var items = new List<ExchangeItem>(reagentTypes.Length);
                foreach (var rt in reagentTypes)
                    items.Add(ExchangeItem.Reagent(rt));
                legs.Add(new ExchangeLeg(PlayerExchangeEvent.SinkDeck, player.PlayerId, items));
            }

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateSun, legs,
                "The Sun — every player received one of each reagent"));
        }

        public static void EmitJudgement(GameSession session, int drawerId, int cardsDrawn)
        {
            if (cardsDrawn <= 0)
            {
                session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateJudgement,
                    System.Array.Empty<ExchangeLeg>(),
                    "The Judgement — no lit cauldrons to draw from"));
                return;
            }

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateJudgement,
                new[] { new ExchangeLeg(PlayerExchangeEvent.SinkDeck, drawerId,
                    new[] { new ExchangeItem(ExchangeItemKind.SpreadCard, count: cardsDrawn) }) },
                $"The Judgement — drew {cardsDrawn} card{(cardsDrawn == 1 ? "" : "s")} from lit cauldrons"));
        }

        public static void EmitWheel(GameSession session,
            IReadOnlyList<(int playerId, int roll, bool gainedSalt, bool discardedCard)> outcomes)
        {
            var legs = new List<ExchangeLeg>();
            foreach (var (playerId, roll, gainedSalt, discardedCard) in outcomes)
            {
                if (gainedSalt)
                    legs.Add(new ExchangeLeg(PlayerExchangeEvent.SinkDeck, playerId,
                        new[] { ExchangeItem.Reagent(ReagentType.Salt, 2) }));
                if (discardedCard)
                    legs.Add(new ExchangeLeg(playerId, PlayerExchangeEvent.SinkDiscard,
                        new[] { new ExchangeItem(ExchangeItemKind.ToDiscard, count: 1) }));
            }

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.FateWheel, legs,
                "Wheel of Fortune — all players re-rolled zodiac"));
        }
    }

}
