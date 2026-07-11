using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Winter phase rule implementations.
    /// Step 1 – Card Unlock (automated via SetCardLockCommand).
    /// Step 2 – Winter Activities free-action pool: card movement, crafting, Fateful Wager.
    /// Step 3 – Enforce card limits; player chooses which cards to discard when over limit.
    /// Step 4 – Transit Age (shuffle deck, rotate Agekeeper).
    /// </summary>
    public sealed class WinterRules
    {
        private readonly ICardDatabase _db;

        public WinterRules(ICardDatabase db) => _db = db;

        public const int SpreadLimit = 5;
        public const int HandLimit   = 5;

        // ─── Step 2a: Move card between Hand and Spread ───────────────────────────

        /// <summary>
        /// Moves one card from Hand → Spread (toSpread=true) or Spread → Hand (toSpread=false).
        /// Only valid during the Winter Activities window (card lock already lifted).
        /// </summary>
        public CommandResult TryMoveCard(GameSession session, int playerId, string cardId, bool toSpread)
        {
            var player = session.Players[playerId];
            if (toSpread)
            {
                if (!player.Hand.Remove(cardId))
                    return CommandResult.Invalid($"Card {cardId} is not in Player {playerId}'s Hand.");
                player.Spread.Add(cardId);
                session.GetCard(cardId)?.MoveTo(CardZone.Spread, playerId);
            }
            else
            {
                if (!player.Spread.Remove(cardId))
                    return CommandResult.Invalid($"Card {cardId} is not in Player {playerId}'s Spread.");
                player.Hand.Add(cardId);
                session.GetCard(cardId)?.MoveTo(CardZone.Hand, playerId);
            }
            session.EmitEvent(new CardMovedToZoneEvent(playerId, cardId, toSpread));
            return CommandResult.Ok();
        }

        // ─── Step 2b: Fateful Wager ───────────────────────────────────────────────

        /// <summary>
        /// Places a Fateful Wager: wagers cards from Spread/Hand on a predicted Cosmic Age sign.
        /// Wagered cards are removed from play until the next Spring Cosmic Age roll.
        /// Only one wager per player per round.
        /// </summary>
        public CommandResult TryPlaceWager(GameSession session, int playerId,
            ZodiacSign predictedSign, IReadOnlyList<string> cardIds)
        {
            if (predictedSign == ZodiacSign.None)
                return CommandResult.Invalid("Must predict a valid Zodiac Sign.");

            var player = session.Players[playerId];

            if (player.FatefulWagerSign != ZodiacSign.None)
                return CommandResult.Invalid("You have already placed a Fateful Wager this round.");

            if (cardIds.Count == 0)
                return CommandResult.Invalid("Must wager at least 1 card.");

            foreach (var id in cardIds)
            {
                // Must be in Spread or Hand, and must be minor arcana
                bool inSpread = player.Spread.Contains(id);
                bool inHand   = player.Hand.Contains(id);
                if (!inSpread && !inHand)
                    return CommandResult.Invalid($"Card {id} is not in Player {playerId}'s Spread or Hand.");

                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid($"Card {id} is a Major Arcana card and cannot be wagered.");
            }

            // Remove cards from zones and hold them in wager limbo
            foreach (var id in cardIds)
            {
                player.Spread.Remove(id);
                player.Hand.Remove(id);
                var inst = session.GetCard(id);
                if (inst != null)
                {
                    inst.MoveTo(CardZone.Deck, -1);
                    inst.SetOwnerId(-1);
                }
                player.FatefulWagerCards.Add(id);
            }

            player.FatefulWagerSign = predictedSign;
            session.EmitEvent(new FatefulWagerPlacedEvent(playerId, predictedSign, cardIds.Count));
            return CommandResult.Ok($"Fateful Wager placed on {predictedSign} with {cardIds.Count} card(s).");
        }

        /// <summary>
        /// Resolves all pending Fateful Wagers against the revealed Cosmic Age sign.
        /// Called from SpringRules.RollCosmicAge after setting CosmicAgeSign.
        /// </summary>
        public void ResolveWagers(GameSession session, ZodiacSign cosmicSign)
        {
            foreach (var player in session.Players)
            {
                if (player.FatefulWagerSign == ZodiacSign.None || player.FatefulWagerCards.Count == 0)
                    continue;

                bool won  = player.FatefulWagerSign == cosmicSign;
                int count = player.FatefulWagerCards.Count;

                if (won)
                {
                    // Return wagered cards to Hand plus draw an equal number of bonus cards
                    foreach (var id in player.FatefulWagerCards)
                    {
                        session.GetCard(id)?.MoveTo(CardZone.Hand, player.PlayerId);
                        player.Hand.Add(id);
                    }
                    // Draw equal count from Common Deck as bonus
                    int bonus = count;
                    for (int i = 0; i < bonus && session.Board.CommonDeck.Count > 0; i++)
                    {
                        var deckId = session.Board.CommonDeck.Pop();
                        session.GetCard(deckId)?.MoveTo(CardZone.Hand, player.PlayerId);
                        player.Hand.Add(deckId);
                    }
                }
                else
                {
                    // Wagered cards are lost to the discard pile
                    foreach (var id in player.FatefulWagerCards)
                    {
                        session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
                        session.Board.CommonDiscard.Add(id);
                    }
                }

                var predicted = player.FatefulWagerSign;
                session.EmitEvent(new FatefulWagerResolvedEvent(player.PlayerId, predicted, cosmicSign, won, count));
                player.FatefulWagerCards.Clear();
                player.FatefulWagerSign = ZodiacSign.None;
            }
        }

        // ─── Step 3: Player-chosen discard to limit ───────────────────────────────

        /// <summary>
        /// Player discards specific cards to bring Spread ≤ 5 and Hand ≤ 5.
        /// Called per-player when they are over the limit.
        /// </summary>
        public CommandResult TryDiscardToLimit(GameSession session, int playerId,
            IReadOnlyList<string> discardSpreadIds, IReadOnlyList<string> discardHandIds)
        {
            var player = session.Players[playerId];

            // Validate ownership
            foreach (var id in discardSpreadIds)
                if (!player.Spread.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in Player {playerId}'s Spread.");
            foreach (var id in discardHandIds)
                if (!player.Hand.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in Player {playerId}'s Hand.");

            // Validate result is within limits
            int newSpreadCount = player.Spread.Count - discardSpreadIds.Count;
            int newHandCount   = player.Hand.Count   - discardHandIds.Count;
            int spreadLimit = PlayerLimitService.GetSpreadLimit(session, player);
            if (newSpreadCount > spreadLimit)
                return CommandResult.Invalid(
                    $"After discarding, Spread would still be {newSpreadCount} (limit {spreadLimit}).");
            int handLimit = PlayerLimitService.GetHandLimit(session, player);
            if (newHandCount > handLimit)
                return CommandResult.Invalid(
                    $"After discarding, Hand would still be {newHandCount} (limit {handLimit}).");

            int total = discardSpreadIds.Count + discardHandIds.Count;
            foreach (var id in discardSpreadIds)
            {
                player.Spread.Remove(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
                session.Board.CommonDiscard.Add(id);
            }
            foreach (var id in discardHandIds)
            {
                player.Hand.Remove(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
                session.Board.CommonDiscard.Add(id);
            }

            session.EmitEvent(new CardsDiscardedToLimitEvent(playerId, total));
            return CommandResult.Ok($"Discarded {total} card(s) to meet card limits.");
        }

        /// <summary>
        /// Clears Fate cards from every player's Arcanum (they don't carry between rounds).
        /// Call this even when the player is within card limits.
        /// </summary>
        public void ClearFateCardsFromArcanum(GameSession session)
        {
            ClearFateCards(session);
        }

        /// <summary>Winter Step 3: discard Fate cards from Arcanum, then trim Spread and Hand.</summary>
        [Obsolete("Use TryDiscardToLimit via DiscardToLimitCommand instead. This path does not update CardInstance zones.")]
        public void EnforceLimits(GameSession session)
        {
            ClearFateCards(session);

            foreach (var player in session.Players)
            {
                int discarded = 0;
                int spreadLimit = PlayerLimitService.GetSpreadLimit(session, player);
                discarded += TrimZone(player.Spread, spreadLimit, session.Board.CommonDiscard);
                int handLimit = PlayerLimitService.GetHandLimit(session, player);
                discarded += TrimZone(player.Hand, handLimit, session.Board.CommonDiscard);
                session.EmitEvent(new CardLimitsEnforcedEvent(player.PlayerId, discarded));
            }
        }

        /// <summary>
        /// Discard all Fate cards from every player's Arcanum back to Common Discard.
        /// Fate cards have Effect Duration = single round; they never carry over to the next round.
        /// </summary>
        private void ClearFateCards(GameSession session)
        {
            foreach (var player in session.Players)
            {
                for (int i = player.Arcanum.Count - 1; i >= 0; i--)
                {
                    var id   = player.Arcanum[i];
                    var inst = session.GetCard(id);
                    var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                    if (def?.MajorArcanaType == MajorArcanaType.Fate)
                    {
                        player.Arcanum.RemoveAt(i);
                        inst!.MoveTo(CardZone.Discard, -1);
                        if (!session.Board.CommonDiscard.Contains(id))
                            session.Board.CommonDiscard.Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// Winter Step 4: shuffle all common discard back into the deck, increment round,
        /// rotate the Agekeeper key clockwise, and clear per-round Cosmic Effect flags.
        /// </summary>
        public void Transit(GameSession session)
        {
            ReshuffleCommonDeck(session);

            session.Board.RoundNumber++;

            // Clear all per-round effect flags
            session.Board.CosmicEffect    = CosmicEffectFlags.Default;
            session.Board.ContestEffects = ContestEffectFlags.Default;
            session.Board.PendingContest   = null;
            session.CurrentTurnPlayerId    = null;

            // Clear per-round stone flags and Tower arrest.
            // Wager state is intentionally preserved here; it is resolved and cleared
            // by ResolveWagers() at the start of the next Spring Cosmic Age roll.
            foreach (var player in session.Players)
            {
                player.ReturnedFromStasisThisRound = false;
                player.BesiegedBonusCount          = 0;
                player.DuelChallengedRivalId       = -1;
                player.ArrestedAdepts.Clear();
                player.UsedAdeptInstanceIdsThisAge.Clear();
                player.EmpressMarkedReagents.Clear();
                player.TemperanceSaltWildReagent = null;
                player.EmperorProtectedCardIds.Clear();
                player.HierophantOppositionShift = 0;
                player.WorldWildcardFlipped = false;
            }

            session.Board.PendingPriestessReturns.Clear();

            int newAgekeeper = RotateAgekeeper(session);
            session.EmitEvent(new AgeTransitedEvent(session.Board.RoundNumber, newAgekeeper));
        }

        private static int TrimZone(List<string> zone, int limit, List<string> discard)
        {
            int excess = zone.Count - limit;
            if (excess <= 0) return 0;

            // Remove from the end (most recently added cards go first)
            var removed = zone.GetRange(zone.Count - excess, excess);
            zone.RemoveRange(zone.Count - excess, excess);
            discard.AddRange(removed);

            // Update card instance zones
            return excess;
        }

        private static void ReshuffleCommonDeck(GameSession session)
        {
            var board = session.Board;
            if (board.CommonDiscard.Count == 0) return;

            var allCards = DedupeDiscardIds(board.CommonDiscard);
            board.CommonDiscard.Clear();

            // Push in shuffled order
            Shuffle(allCards, new System.Random());
            foreach (var id in allCards)
            {
                board.CommonDeck.Push(id);
                if (session.GetCard(id) is { } card)
                    card.MoveTo(Domain.CardZone.Deck, -1);
            }
        }

        static List<string> DedupeDiscardIds(IReadOnlyList<string> discard)
        {
            var cards = new List<string>(discard.Count);
            var seen = new HashSet<string>();
            foreach (var id in discard)
            {
                if (seen.Add(id))
                    cards.Add(id);
            }
            return cards;
        }

        private static int RotateAgekeeper(GameSession session)
        {
            int currentIdx = -1;
            for (int i = 0; i < session.Players.Count; i++)
            {
                if (session.Players[i].IsAgekeeper)
                {
                    currentIdx = i;
                    break;
                }
            }
            if (currentIdx < 0) currentIdx = 0;

            session.Players[currentIdx].IsAgekeeper = false;
            int nextIdx = (currentIdx + 1) % session.Players.Count;
            session.Players[nextIdx].IsAgekeeper = true;
            return session.Players[nextIdx].PlayerId;
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
