using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Enforces Phase 3 adept base passive commands.</summary>
    public sealed class AdeptRules : IAdeptService
    {
        private readonly ICardDatabase _db;

        public AdeptRules(ICardDatabase db) => _db = db;

        public CommandResult TryMagicianSwap(GameSession session, int playerId, string cardId, bool toSpread)
        {
            var player = session.Players[playerId];
            if (!AdeptEffectService.HasAdept(session, player, AdeptEffectService.MagicianArcana))
                return CommandResult.Invalid("The Magician must be in your Arcanum.");

            var inst = session.GetCard(cardId);
            var def = inst != null ? _db.GetById(inst.DefinitionId) : null;
            if (def?.IsMajorArcana == true)
                return CommandResult.Invalid("Major Arcana cards cannot be swapped.");

            if (toSpread)
            {
                if (!player.Hand.Remove(cardId))
                    return CommandResult.Invalid($"Card {cardId} is not in your Hand.");
                player.Spread.Add(cardId);
                inst?.MoveTo(CardZone.Spread, playerId);
            }
            else
            {
                if (!player.Spread.Remove(cardId))
                    return CommandResult.Invalid($"Card {cardId} is not in your Spread.");
                player.Hand.Add(cardId);
                inst?.MoveTo(CardZone.Hand, playerId);
            }

            session.EmitEvent(new CardMovedToZoneEvent(playerId, cardId, toSpread));
            return CommandResult.Ok();
        }

        public CommandResult TryProtectSpreadCards(GameSession session, int playerId,
            IReadOnlyList<string> cardIds)
        {
            if (cardIds.Count != 2)
                return CommandResult.Invalid("Emperor protects exactly 2 Spread cards.");

            var player = session.Players[playerId];
            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.EmperorArcana))
                return CommandResult.Invalid("The Emperor power is not available this age.");

            foreach (var id in cardIds)
            {
                if (!player.Spread.Contains(id))
                    return CommandResult.Invalid($"Card {id} must be in your Spread.");
                var inst = session.GetCard(id);
                var def = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid("Only minor arcana cards can be protected.");
            }

            player.EmperorProtectedSpreadIds.Clear();
            foreach (var id in cardIds)
                player.EmperorProtectedSpreadIds.Add(id);

            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.EmperorArcana);
            return CommandResult.Ok("Two Spread cards are now protected.");
        }

        public CommandResult TryShiftZodiac(GameSession session, int playerId, int delta)
        {
            if (delta is not (-1) and not 1)
                return CommandResult.Invalid("Hierophant shift must be -1 or +1.");

            var player = session.Players[playerId];
            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.HierophantArcana))
                return CommandResult.Invalid("The Hierophant power is not available this age.");
            if (player.CurrentSign == ZodiacSign.None)
                return CommandResult.Invalid("Roll your Zodiac die before shifting.");

            player.CurrentSign = AdeptEffectService.ShiftSign(player.CurrentSign, delta);
            player.PersonalCosmicEffects = CosmicEffectService.ComputePersonalEffects(
                player.CurrentSign, player.AstralHouses, session.Board.CosmicAgeSign);
            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.HierophantArcana);
            session.EmitEvent(new ZodiacRolledEvent(playerId, player.CurrentSign));
            return CommandResult.Ok($"Zodiac shifted to {player.CurrentSign}.");
        }

        public CommandResult TryShiftOppositionZodiac(GameSession session, int playerId, int delta)
        {
            if (delta is not (-1) and not 1)
                return CommandResult.Invalid("Hierophant shift must be -1 or +1.");

            var player = session.Players[playerId];
            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.HierophantArcana))
                return CommandResult.Invalid("The Hierophant power is not available this age.");
            if (player.CurrentSign == ZodiacSign.None)
                return CommandResult.Invalid("You need a Zodiac sign before shifting Opposition alignment.");

            player.HierophantOppositionShift = delta;
            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.HierophantArcana);
            return CommandResult.Ok($"Opposition alignment will shift by {delta} this age.");
        }

        public CommandResult TryDevilSteal(GameSession session, int playerId, string sacrificeCardId,
            int targetPlayerId, string stolenCardId)
        {
            if (targetPlayerId == playerId)
                return CommandResult.Invalid("Cannot steal from yourself.");

            var player = session.Players[playerId];
            var target = session.Players[targetPlayerId];

            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.DevilArcana))
                return CommandResult.Invalid("The Devil power is not available this age.");

            bool sacrificeOwned = player.Spread.Contains(sacrificeCardId) || player.Hand.Contains(sacrificeCardId);
            if (!sacrificeOwned)
                return CommandResult.Invalid("Sacrifice card must be in your Hand or Spread.");

            if (!target.Spread.Contains(stolenCardId))
                return CommandResult.Invalid("Stolen card must be in the opponent's Spread.");

            var sacrificeDef = GetDef(session, sacrificeCardId);
            var stolenDef = GetDef(session, stolenCardId);
            if (sacrificeDef?.IsMajorArcana == true || stolenDef?.IsMajorArcana == true)
                return CommandResult.Invalid("Only minor arcana cards may be sacrificed or stolen.");

            player.Spread.Remove(sacrificeCardId);
            player.Hand.Remove(sacrificeCardId);
            session.Board.CommonDiscard.Add(sacrificeCardId);
            session.GetCard(sacrificeCardId)?.MoveTo(CardZone.Discard, -1);

            target.Spread.Remove(stolenCardId);
            player.Spread.Add(stolenCardId);
            session.GetCard(stolenCardId)?.MoveTo(CardZone.Spread, playerId);

            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.DevilArcana);
            return CommandResult.Ok("Devil steal resolved.");
        }

        public bool IsPriestessHarvestPending(GameSession session, int playerId)
            => session.Board.PendingPriestessReturns.Contains(playerId);

        public CommandResult TryCompletePriestessHarvest(GameSession session, int playerId,
            IReadOnlyList<string> returnCardIds)
        {
            if (!session.Board.PendingPriestessReturns.Contains(playerId))
                return CommandResult.Invalid("No Priestess harvest awaiting return.");

            if (returnCardIds.Count != 2)
                return CommandResult.Invalid("Return exactly 2 cards to the deck.");

            var player = session.Players[playerId];
            foreach (var id in returnCardIds)
            {
                if (!player.Hand.Contains(id))
                    return CommandResult.Invalid($"Card {id} must be in your Hand.");
                var def = GetDef(session, id);
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid("Only minor arcana cards may be returned.");
            }

            foreach (var id in returnCardIds)
            {
                player.Hand.Remove(id);
                session.Board.CommonDeck.Push(id);
                session.GetCard(id)?.MoveTo(CardZone.Deck, -1);
            }

            session.Board.PendingPriestessReturns.Remove(playerId);
            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.PriestessArcana);
            return CommandResult.Ok("Priestess harvest completed.");
        }

        CardDefinition? GetDef(GameSession session, string cardId)
        {
            var inst = session.GetCard(cardId);
            return inst != null ? _db.GetById(inst.DefinitionId) : null;
        }
    }
}
