using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Implements reagent crafting available in Summer, Autumn, and Winter.
    /// Cost resolution is delegated to <see cref="CraftModifierService"/>.
    /// </summary>
    public sealed class CraftingRules : ICraftingService
    {
        private readonly ICardDatabase _db;

        public CraftingRules(ICardDatabase db) => _db = db;

        public CommandResult TryCraft(GameSession session, int playerId,
            ReagentType reagentType, IReadOnlyList<string> cardInstanceIds)
        {
            var player = session.Players[playerId];

            var option = CraftModifierService.FindMatchingOption(
                session, playerId, reagentType, cardInstanceIds, _db);
            if (!option.HasValue)
            {
                int minCost = CraftModifierService.GetMinimumCost(session, playerId, reagentType);
                return CommandResult.Invalid(
                    $"Crafting {reagentType} requires a valid payment of {minCost} cards.");
            }

            if (reagentType != ReagentType.Salt)
            {
                var requiredSuit = Correspondence.SuitFor(reagentType);
                if (!player.IsCauldronLit(requiredSuit))
                {
                    return CommandResult.Invalid(
                        $"The {Correspondence.ElementFor(requiredSuit)} Cauldron must be lit to craft {reagentType}.");
                }
            }

            DiscardCards(session, player, cardInstanceIds);
            player.AddReagent(reagentType);
            session.EmitEvent(new ReagentCraftedEvent(playerId, reagentType));
            return CommandResult.Ok($"Crafted 1 {reagentType}.");
        }

        public CommandResult TryKingDiscardCraft(GameSession session, int playerId, string kingCardId)
        {
            var player = session.Players[playerId];
            var owned = BuildPlayerCardSet(player);
            if (!owned.Contains(kingCardId))
                return CommandResult.Invalid($"Card {kingCardId} does not belong to player {playerId}.");

            var inst = session.GetCard(kingCardId);
            var def = inst != null ? _db.GetById(inst.DefinitionId) : null;
            if (def == null || !SpreadCraftEffectCatalog.IsKingV1DiscardCraft(def))
                return CommandResult.Invalid("Card is not a King V1 craft action.");

            bool kingInSpread = player.Spread.Contains(kingCardId);
            if (!kingInSpread)
                return CommandResult.Invalid("King must be in Spread to discard-craft.");

            var reagentType = SpreadCraftEffectCatalog.ReagentForKing(def);
            var requiredSuit = Correspondence.SuitFor(reagentType);
            if (!player.IsCauldronLit(requiredSuit))
            {
                return CommandResult.Invalid(
                    $"The {Correspondence.ElementFor(requiredSuit)} Cauldron must be lit to craft {reagentType}.");
            }

            DiscardCards(session, player, new[] { kingCardId });
            player.AddReagent(reagentType);
            session.EmitEvent(new ReagentCraftedEvent(playerId, reagentType));
            return CommandResult.Ok($"Discarded {def.Name} to craft 1 {reagentType}.");
        }

        public CommandResult TryMarkEmpressReagent(GameSession session, int playerId, ReagentType reagentType)
        {
            if (reagentType == ReagentType.Salt)
                return CommandResult.Invalid("Empress marks elemental reagent types only.");

            var player = session.Players[playerId];
            var empressId = FindAdeptInstance(session, player, 3);
            if (empressId == null)
                return CommandResult.Invalid("The Empress must be in your Arcanum.");

            if (player.EmpressMarkedReagents.Contains(reagentType))
                return CommandResult.Invalid($"{reagentType} is already marked.");

            int limit = CraftModifierService.EmpressMarkLimit(session, player);
            if (player.EmpressMarkedReagents.Count >= limit)
                return CommandResult.Invalid($"Empress mark limit reached ({limit}).");

            bool firstMarkThisAge = !player.UsedAdeptInstanceIdsThisAge.Contains(empressId);
            if (firstMarkThisAge)
            {
                if (!player.SpendReagent(ReagentType.Salt))
                    return CommandResult.Invalid("Marking a reagent costs 1 Salt.");
                ActiveEffectsService.MarkAdeptUsed(session, playerId, empressId);
            }

            player.EmpressMarkedReagents.Add(reagentType);
            return CommandResult.Ok($"Marked {reagentType} for 2-for-1 crafting this age.");
        }

        public CommandResult TryMarkTemperanceWildReagent(GameSession session, int playerId, ReagentType reagentType)
        {
            if (reagentType == ReagentType.Salt)
                return CommandResult.Invalid("Temperance wild mark targets an elemental reagent.");

            var player = session.Players[playerId];
            var temperanceId = FindAdeptInstance(session, player, 14);
            if (temperanceId == null)
                return CommandResult.Invalid("Temperance must be in your Arcanum.");

            if (!CraftModifierService.IsTemperanceResonant(session, player))
                return CommandResult.Invalid("Temperance must be resonant to mark a wild reagent.");

            if (player.TemperanceSaltWildReagent.HasValue)
                return CommandResult.Invalid("Temperance wild reagent already marked this age.");

            if (player.UsedAdeptInstanceIdsThisAge.Contains(temperanceId))
                return CommandResult.Invalid("Temperance power already used this age.");

            if (!player.SpendReagent(ReagentType.Salt))
                return CommandResult.Invalid("Marking a wild reagent costs 1 Salt.");

            player.TemperanceSaltWildReagent = reagentType;
            ActiveEffectsService.MarkAdeptUsed(session, playerId, temperanceId);
            return CommandResult.Ok($"Salt may substitute for {reagentType} when crafting this age.");
        }

        private static string? FindAdeptInstance(GameSession session, PlayerState player, int arcanaNumber)
        {
            foreach (var cardId in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(cardId))
                    continue;
                var inst = session.GetCard(cardId);
                var def = inst != null ? session.Rules?.CardDatabase.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.ArcanaNumber == arcanaNumber)
                    return cardId;
            }

            return null;
        }

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
