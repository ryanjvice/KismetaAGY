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
            int attackRoll, int defendRoll, int winnerId, string targetCardId, string anteCardId)
        {
            var legs = new List<ExchangeLeg>();
            if (winnerId == attackerId)
            {
                legs.Add(new ExchangeLeg(defenderId, attackerId,
                    new[] { ExchangeItem.Spread(targetCardId) }));
            }
            else
            {
                legs.Add(new ExchangeLeg(attackerId, PlayerExchangeEvent.SinkDiscard,
                    new[] { ExchangeItem.Discard(anteCardId) }));
                legs.Add(new ExchangeLeg(defenderId, defenderId,
                    new[] { ExchangeItem.Spread(targetCardId) }));
            }

            string context = winnerId == attackerId
                ? $"Duel — challenger wins ({attackRoll} vs {defendRoll})"
                : $"Duel — defender wins ({defendRoll} vs {attackRoll})";

            session.EmitEvent(new PlayerExchangeEvent(ExchangeKind.Duel, legs, context));
        }

        public static void EmitGambit(GameSession session, int attackerId, int defenderId,
            int attackRoll, int defendRoll, int winnerId, string offeredCardId,
            bool offeredInCrucible, string? arrestedDefenderCardId)
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
                ? $"Gambit — challenger wins ({attackRoll} vs {defendRoll})"
                : $"Gambit — defender wins ({defendRoll} vs {attackRoll})";

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
    }

}
