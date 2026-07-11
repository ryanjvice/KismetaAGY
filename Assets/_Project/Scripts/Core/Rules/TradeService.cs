using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves direct card trades between players during the Summer action phase.
    /// Only minor arcana cards in the Spread may be traded.
    /// </summary>
    public sealed class TradeService
    {
        private readonly ICardDatabase _db;

        public TradeService(ICardDatabase db) => _db = db;

        /// <summary>Validate offer and record a pending trade awaiting target response.</summary>
        public CommandResult TryInitiateTrade(GameSession session,
            int initiatorId, int targetId,
            IReadOnlyList<string> offerCardIds,
            IReadOnlyList<string> requestCardIds)
        {
            var validation = ValidateTrade(session, initiatorId, targetId, offerCardIds, requestCardIds);
            if (!validation.IsOk) return validation;

            session.Board.PendingContest = new PendingContest
            {
                Kind           = ContestKind.Trade,
                AttackerId     = initiatorId,
                DefenderId     = targetId,
                OfferCardIds   = new List<string>(offerCardIds),
                RequestCardIds = new List<string>(requestCardIds)
            };

            session.EmitEvent(new TradeOfferedEvent(initiatorId, targetId, offerCardIds, requestCardIds));
            return CommandResult.Ok($"Trade offered to P{targetId}.");
        }

        /// <summary>Target accepts or declines the pending trade.</summary>
        public CommandResult TryRespondTrade(GameSession session, int targetId, bool accept)
        {
            var pending = session.Board.PendingContest;
            if (pending == null || pending.Kind != ContestKind.Trade)
                return CommandResult.Invalid("No pending trade to respond to.");
            if (pending.DefenderId != targetId)
                return CommandResult.Invalid("Only the trade target may respond.");
            if (pending.OfferCardIds == null || pending.RequestCardIds == null)
                return CommandResult.Invalid("Pending trade is missing card data.");

            var initiatorId   = pending.AttackerId;
            var offerCardIds  = pending.OfferCardIds;
            var requestCardIds = pending.RequestCardIds;
            session.Board.PendingContest = null;

            if (!accept)
            {
                session.EmitEvent(new TradeDeclinedEvent(initiatorId, targetId));
                return CommandResult.Ok("Trade declined.");
            }

            var validation = ValidateTrade(session, initiatorId, targetId, offerCardIds, requestCardIds);
            if (!validation.IsOk) return validation;

            ExecuteSwap(session, initiatorId, targetId, offerCardIds, requestCardIds);
            session.EmitEvent(new TradeCompletedEvent(
                initiatorId, targetId, offerCardIds.Count, requestCardIds.Count));
            ExchangeEventEmitter.EmitTrade(session, initiatorId, targetId, offerCardIds, requestCardIds);
            return CommandResult.Ok(
                $"Trade complete: P{initiatorId} gave {offerCardIds.Count}, received {requestCardIds.Count}.");
        }

        /// <summary>Legacy entry point — initiates then auto-accepts (tests / debug).</summary>
        public CommandResult TryTrade(GameSession session,
            int initiatorId, int targetId,
            IReadOnlyList<string> offerCardIds,
            IReadOnlyList<string> requestCardIds)
        {
            var initiate = TryInitiateTrade(session, initiatorId, targetId, offerCardIds, requestCardIds);
            if (!initiate.IsOk) return initiate;
            return TryRespondTrade(session, targetId, accept: true);
        }

        static CommandResult ValidateTrade(GameSession session,
            int initiatorId, int targetId,
            IReadOnlyList<string> offerCardIds,
            IReadOnlyList<string> requestCardIds)
        {
            if (initiatorId == targetId)
                return CommandResult.Invalid("Cannot trade with yourself.");

            if (offerCardIds.Count == 0 && requestCardIds.Count == 0)
                return CommandResult.Invalid("A trade must include at least one card on either side.");

            if (initiatorId < 0 || initiatorId >= session.Players.Count)
                return CommandResult.Invalid($"Invalid initiator player ID {initiatorId}.");
            if (targetId < 0 || targetId >= session.Players.Count)
                return CommandResult.Invalid($"Invalid target player ID {targetId}.");

            var initiator = session.Players[initiatorId];
            var target    = session.Players[targetId];
            var db        = session.Rules?.CardDatabase;

            foreach (var id in offerCardIds)
            {
                if (!initiator.Spread.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in your Spread.");
                if (db != null)
                {
                    var inst = session.GetCard(id);
                    var def  = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def?.IsMajorArcana == true)
                        return CommandResult.Invalid($"Card {id} is Major Arcana and cannot be traded.");
                }
            }

            foreach (var id in requestCardIds)
            {
                if (!target.Spread.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in the target's Spread.");
                if (db != null)
                {
                    var inst = session.GetCard(id);
                    var def  = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def?.IsMajorArcana == true)
                        return CommandResult.Invalid($"Card {id} is Major Arcana and cannot be traded.");
                }
            }

            if (!PlayerAspectAlignment.IsMagnusTradeRatioValid(session, initiatorId, targetId,
                    offerCardIds.Count, requestCardIds.Count))
                return CommandResult.Invalid(
                    "Misaligned Magnus trade requires offering 2 cards for every 1 received.");

            if (!ReversedCurseService.IsTradeOfferRatioValid(session, initiatorId,
                    offerCardIds.Count, requestCardIds.Count))
                return CommandResult.Invalid(
                    "Four of Pentacles curse requires offering 2 cards for every 1 received.");

            return CommandResult.Ok();
        }

        static void ExecuteSwap(GameSession session,
            int initiatorId, int targetId,
            IReadOnlyList<string> offerCardIds,
            IReadOnlyList<string> requestCardIds)
        {
            var initiator = session.Players[initiatorId];
            var target    = session.Players[targetId];

            foreach (var id in offerCardIds)
            {
                initiator.Spread.Remove(id);
                session.GetCard(id)?.MoveTo(CardZone.Spread, targetId);
                target.Spread.Add(id);
            }

            foreach (var id in requestCardIds)
            {
                target.Spread.Remove(id);
                session.GetCard(id)?.MoveTo(CardZone.Spread, initiatorId);
                initiator.Spread.Add(id);
            }
        }
    }
}
