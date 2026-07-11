using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Enforces Phase 3 adept base passives and Phase 4 resonant upgrades.</summary>
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
                return CommandResult.Invalid("Emperor protects exactly 2 cards.");

            var player = session.Players[playerId];
            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.EmperorArcana))
                return CommandResult.Invalid("The Emperor power is not available this age.");

            bool resonant = AdeptAttunement.IsEmperorResonant(session, player);
            foreach (var id in cardIds)
            {
                bool inSpread = player.Spread.Contains(id);
                bool inHand = player.Hand.Contains(id);
                if (!inSpread && !inHand)
                    return CommandResult.Invalid($"Card {id} must be in your Hand or Spread.");

                if (!resonant && !inSpread)
                    return CommandResult.Invalid("Base Emperor protection requires Spread cards.");

                var inst = session.GetCard(id);
                var def = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid("Only minor arcana cards can be protected.");
            }

            player.EmperorProtectedCardIds.Clear();
            foreach (var id in cardIds)
                player.EmperorProtectedCardIds.Add(id);

            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.EmperorArcana);
            return CommandResult.Ok(resonant
                ? "Two cards are now protected from any attack."
                : "Two Spread cards are now protected.");
        }

        public CommandResult TryShiftZodiac(GameSession session, int playerId, int delta)
        {
            var player = session.Players[playerId];
            int maxDelta = AdeptAttunement.HierophantMaxShiftDelta(session, player);
            if (delta < -maxDelta || delta > maxDelta || delta == 0)
                return CommandResult.Invalid($"Hierophant shift must be between -{maxDelta} and +{maxDelta}.");

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
            var player = session.Players[playerId];
            int maxDelta = AdeptAttunement.HierophantMaxShiftDelta(session, player);
            if (delta < -maxDelta || delta > maxDelta || delta == 0)
                return CommandResult.Invalid($"Hierophant shift must be between -{maxDelta} and +{maxDelta}.");

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

            if (AdeptEffectService.IsEmperorProtected(session, target, stolenCardId))
                return CommandResult.Invalid("Target card is protected by the Emperor.");

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

        public CommandResult TryDevilBanishAdept(GameSession session, int playerId,
            int targetPlayerId, string targetAdeptCardId)
        {
            if (targetPlayerId == playerId)
                return CommandResult.Invalid("Cannot banish your own adept.");

            var player = session.Players[playerId];
            var target = session.Players[targetPlayerId];

            if (!AdeptAttunement.IsDevilResonant(session, player))
                return CommandResult.Invalid("Devil banish requires an attuned Devil.");

            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.DevilArcana))
                return CommandResult.Invalid("The Devil power is not available this age.");

            var devilId = AdeptEffectService.FindAdeptInstance(session, player, AdeptEffectService.DevilArcana);
            if (devilId == null)
                return CommandResult.Invalid("The Devil must be in your Arcanum.");

            if (!target.Arcanum.Contains(targetAdeptCardId))
                return CommandResult.Invalid("Target must be an adept in the opponent's Arcanum.");

            var targetDef = GetDef(session, targetAdeptCardId);
            if (targetDef?.MajorArcanaType != MajorArcanaType.Adept)
                return CommandResult.Invalid("Target must be an Adept card.");

            player.Arcanum.Remove(devilId);
            target.Arcanum.Remove(targetAdeptCardId);

            AdeptEffectService.ReturnCardToDeckTop(session, devilId);
            AdeptEffectService.ReturnCardToDeckTop(session, targetAdeptCardId);

            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.DevilArcana);
            return CommandResult.Ok("Devil banish: both adepts returned to the deck.");
        }

        public CommandResult TryStarNullify(GameSession session, int playerId,
            int targetPlayerId, string targetCardId)
        {
            if (targetPlayerId == playerId)
                return CommandResult.Invalid("Star nullify must target an opponent.");

            var player = session.Players[playerId];
            var target = session.Players[targetPlayerId];

            if (!AdeptAttunement.IsStarResonant(session, player))
                return CommandResult.Invalid("Star nullify requires an attuned Star.");

            if (!AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.StarArcana))
                return CommandResult.Invalid("The Star power is not available this age.");

            if (!target.Arcanum.Contains(targetCardId))
                return CommandResult.Invalid("Target must be in the opponent's Arcanum.");

            var targetDef = GetDef(session, targetCardId);
            if (targetDef?.MajorArcanaType is not (MajorArcanaType.Adept or MajorArcanaType.Fate))
                return CommandResult.Invalid("Target must be an Adept or Fate card.");

            CardEffectSuppressionService.Suppress(session, targetCardId);
            AdeptEffectService.TryMarkUsed(session, playerId, AdeptEffectService.StarArcana);
            return CommandResult.Ok($"Nullified {targetDef.Name}.");
        }

        public CommandResult TryRefreshStarNullify(GameSession session, int playerId, string targetCardId)
        {
            var player = session.Players[playerId];
            if (!AdeptAttunement.IsStarResonant(session, player))
                return CommandResult.Invalid("Star refresh requires an attuned Star.");

            if (!CardEffectSuppressionService.IsSuppressed(session, targetCardId))
                return CommandResult.Invalid("Target card is not suppressed.");

            if (!player.SpendReagent(ReagentType.Salt, 1))
                return CommandResult.Invalid("Refreshing a nullified card costs 1 Salt.");

            CardEffectSuppressionService.ClearSuppression(session, targetCardId);
            return CommandResult.Ok("Nullification cleared.");
        }

        public CommandResult TryActivateWorldWildcard(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            if (!AdeptAttunement.IsWorldResonant(session, player))
                return CommandResult.Invalid("World crucible wildcard requires an attuned World.");

            var worldId = AdeptEffectService.FindAdeptInstance(session, player, AdeptEffectService.WorldArcana);
            if (worldId == null)
                return CommandResult.Invalid("World adept required.");

            if (player.WorldWildcardFlipped)
                return CommandResult.Invalid("World wildcard is already flipped.");

            if (player.UsedAdeptInstanceIdsThisAge.Contains(worldId))
                return CommandResult.Invalid("World wildcard flip already used this age; refresh with 1 Salt.");

            player.WorldWildcardFlipped = true;
            ActiveEffectsService.MarkAdeptUsed(session, playerId, worldId);
            return CommandResult.Ok("World wildcard flipped for next Fire.");
        }

        public CommandResult TryRefreshWorldWildcard(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            if (!AdeptAttunement.IsWorldResonant(session, player))
                return CommandResult.Invalid("World refresh requires an attuned World.");

            var worldId = AdeptEffectService.FindAdeptInstance(session, player, AdeptEffectService.WorldArcana);
            if (worldId == null)
                return CommandResult.Invalid("World adept required.");

            if (!player.UsedAdeptInstanceIdsThisAge.Contains(worldId))
                return CommandResult.Invalid("World wildcard flip has not been used this age.");

            if (!player.SpendReagent(ReagentType.Salt, 1))
                return CommandResult.Invalid("Refreshing World wildcard costs 1 Salt.");

            player.UsedAdeptInstanceIdsThisAge.Remove(worldId);
            return CommandResult.Ok("World wildcard refresh — may flip again.");
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
