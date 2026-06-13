using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Implements reagent crafting available in Summer, Autumn, and Winter:
    ///
    ///   Salt (any round)      — discard any 3 cards → gain 1 Salt.
    ///   Elemental reagent     — discard 3 cards of the matching Suit
    ///                           AND have the matching Cauldron lit.
    ///
    /// M2: crafting is available every season (no free-action restrictions tracked here;
    /// the GameLoop enforces turn order and action counts).
    /// </summary>
    public sealed class CraftingRules : ICraftingService
    {
        private const int CardCost = 3;

        private readonly ICardDatabase _db;

        public CraftingRules(ICardDatabase db) => _db = db;

        public CommandResult TryCraft(GameSession session, int playerId,
            ReagentType reagentType, IReadOnlyList<string> cardInstanceIds)
        {
            var player   = session.Players[playerId];
            var cosEffect = session.Board.CosmicEffect;

            // Determine the effective card cost for this crafting action
            int effectiveCost = CardCost; // default: 3

            if (reagentType == ReagentType.Salt && cosEffect.SaltCostsTwo)
                effectiveCost = 2;
            else if (reagentType != ReagentType.Salt
                     && cosEffect.CheapCraftReagent == reagentType
                     && cosEffect.CheapCraftSuit    != Suit.None)
                effectiveCost = 2;

            if (cardInstanceIds.Count < effectiveCost)
                return CommandResult.Invalid($"Crafting {reagentType} requires {effectiveCost} cards.");

            // Validate ownership
            var playerCards = BuildPlayerCardSet(player);
            foreach (var id in cardInstanceIds)
                if (!playerCards.Contains(id))
                    return CommandResult.Invalid($"Card {id} does not belong to player {playerId}.");

            if (reagentType == ReagentType.Salt)
            {
                // Salt: any cards (count already validated above)
                DiscardCards(session, player, cardInstanceIds);
                player.AddReagent(ReagentType.Salt);
                session.EmitEvent(new ReagentCraftedEvent(playerId, ReagentType.Salt));
                return CommandResult.Ok("Crafted 1 Salt.");
            }

            // Elemental reagent: all cards must share the required Suit (or be Wild Court Cards)
            var requiredSuit = Correspondence.SuitFor(reagentType);
            if (requiredSuit == Suit.None)
                return CommandResult.Invalid($"Unknown reagent type: {reagentType}.");

            foreach (var id in cardInstanceIds)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def == null)
                    return CommandResult.Invalid($"Unknown card {id}.");

                bool suitMatch = def.Suit == requiredSuit;
                bool wildMatch = cosEffect.WildCourtSuit != Suit.None
                    && def.Suit == cosEffect.WildCourtSuit
                    && IsCourtCard(def.Rank);

                if (!suitMatch && !wildMatch)
                    return CommandResult.Invalid(
                        $"All cards must be {requiredSuit} to craft {reagentType}.");
            }

            // Cauldron must be lit for this element
            if (!player.IsCauldronLit(requiredSuit))
                return CommandResult.Invalid(
                    $"The {Correspondence.ElementFor(requiredSuit)} Cauldron must be lit to craft {reagentType}.");

            DiscardCards(session, player, cardInstanceIds);
            player.AddReagent(reagentType);
            session.EmitEvent(new ReagentCraftedEvent(playerId, reagentType));
            return CommandResult.Ok($"Crafted 1 {reagentType}.");
        }

        private static bool IsCourtCard(Rank rank) =>
            rank == Rank.Princess || rank == Rank.Knight || rank == Rank.Queen || rank == Rank.King;

        private static HashSet<string> BuildPlayerCardSet(PlayerState player)
        {
            var set = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) set.Add(id);
            foreach (var id in player.Hand)   set.Add(id);
            return set;
        }

        private static void DiscardCards(GameSession session, PlayerState player,
            IReadOnlyList<string> cardIds)
        {
            foreach (var id in cardIds)
            {
                player.Spread.Remove(id);
                player.Hand.Remove(id);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }
        }
    }
}
