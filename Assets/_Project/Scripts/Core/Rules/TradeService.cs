using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves direct card trades between players during the Summer free-action pool.
    /// Only minor arcana cards in the Spread may be traded; no card-lock check is required
    /// because card lock is always active in Summer (Spread cards are visible/usable).
    /// </summary>
    public sealed class TradeService
    {
        private readonly ICardDatabase _db;

        public TradeService(ICardDatabase db) => _db = db;

        /// <summary>
        /// Immediately swaps <paramref name="offerCardIds"/> from initiator's Spread with
        /// <paramref name="requestCardIds"/> from the target's Spread.
        /// Either offer or request may be empty (a gift trade), but not both.
        /// </summary>
        public CommandResult TryTrade(GameSession session,
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

            // Validate initiator's offered cards
            foreach (var id in offerCardIds)
            {
                if (!initiator.Spread.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in your Spread.");
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid($"Card {id} is Major Arcana and cannot be traded.");
            }

            // Validate target's requested cards
            foreach (var id in requestCardIds)
            {
                if (!target.Spread.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in the target's Spread.");
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid($"Card {id} is Major Arcana and cannot be traded.");
            }

            // Execute the swap
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

            session.EmitEvent(new TradeCompletedEvent(
                initiatorId, targetId,
                offerCardIds.Count, requestCardIds.Count));
            return CommandResult.Ok(
                $"Trade complete: P{initiatorId} gave {offerCardIds.Count}, received {requestCardIds.Count}.");
        }
    }
}
