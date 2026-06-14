using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Winter phase rule implementations.
    /// Card Unlock, Enforce Limits (Spread 5 / Hand 5), Shuffle Deck, Transit Age (rotate Agekeeper).
    /// These are all system-driven steps with no real player decision; the GameLoop calls them directly.
    /// </summary>
    public sealed class WinterRules
    {
        private readonly ICardDatabase _db;

        public WinterRules(ICardDatabase db) => _db = db;

        public const int SpreadLimit = 5;
        public const int HandLimit   = 5;

        /// <summary>Winter Step 3: discard Fate cards from Arcanum, then trim Spread and Hand.</summary>
        public void EnforceLimits(GameSession session)
        {
            ClearFateCards(session);

            foreach (var player in session.Players)
            {
                int discarded = 0;
                discarded += TrimZone(player.Spread, SpreadLimit, session.Board.CommonDiscard);
                discarded += TrimZone(player.Hand,   HandLimit,   session.Board.CommonDiscard);
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
            session.Board.BestOfThreeDuels = false;

            // Clear per-round stone flags
            foreach (var player in session.Players)
                player.ReturnedFromStasisThisRound = false;

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

            var allCards = new List<string>(board.CommonDiscard);
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
