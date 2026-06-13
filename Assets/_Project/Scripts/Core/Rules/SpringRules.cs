using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Implements all Spring phase steps:
    ///   Step 1 – Cosmic Age roll (Agekeeper rolls the Age Die)
    ///   Step 2 – Personal Zodiac roll (each player)
    ///   Step 3 – Harvest (draw base 3 + alignment bonuses)
    ///   Step 4 – Commune (player assigns cards to Spread / Hand)
    ///   Step 5 – Card Lock (handled by GameSession.Apply(SetCardLockCommand))
    ///
    /// Alignment bonus: same Sign +3, same Planet +2, same Element +1 (highest applies).
    /// Agekeeper's Boon: if Agekeeper's sign matches Cosmic Age, ALL players get +2 extra.
    ///
    /// Major Arcana routing during Harvest (per rules):
    ///   Fate cards  → placed in Arcanum immediately (face-up), never enter Hand.
    ///   Adept cards → discarded back to Common Discard (purchase mechanic not yet in M2).
    /// </summary>
    public sealed class SpringRules : IHarvestService
    {
        private readonly Random _rng;
        private readonly ICardDatabase _db;

        public SpringRules(ICardDatabase db, int? seed = null)
        {
            _db  = db;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ─── Step 1: Cosmic Age roll ──────────────────────────────────────────────

        public void RollCosmicAge(GameSession session)
        {
            var sign = RollSign();
            session.Board.CosmicAgeSign = sign;
            session.EmitEvent(new CosmicAgeSetEvent(sign, Correspondence.PlanetFor(sign), Correspondence.ElementFor(sign)));
        }

        // ─── Step 2: Personal Zodiac roll ─────────────────────────────────────────

        public void RollZodiac(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            var sign   = RollSign();
            player.CurrentSign = sign;
            session.EmitEvent(new ZodiacRolledEvent(playerId, sign));
        }

        // ─── Step 3: Harvest ──────────────────────────────────────────────────────

        public int CalculateHarvestCount(GameSession session, int playerId)
        {
            var player     = session.Players[playerId];
            var cosmicSign = session.Board.CosmicAgeSign;

            int bonus = AlignmentBonus(player.CurrentSign, cosmicSign);

            // Agekeeper's Boon: if Agekeeper's sign matches the Cosmic Age, +2 to everyone
            if (AgekeeperMatchesCosmic(session))
                bonus += 2;

            return 3 + bonus;
        }

        public void ExecuteHarvest(GameSession session, int playerId)
        {
            int count  = CalculateHarvestCount(session, playerId);
            var player = session.Players[playerId];
            var drawn  = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                if (session.Board.CommonDeck.Count == 0)
                    ReshuffleDiscard(session);
                if (session.Board.CommonDeck.Count == 0)
                    break;

                var id   = session.Board.CommonDeck.Pop();
                var inst = session.GetCard(id);
                if (inst == null) continue;

                var def = _db.GetById(inst.DefinitionId);

                if (def?.MajorArcanaType == MajorArcanaType.Fate)
                {
                    // Fate cards go directly to Arcanum, face-up — never enter Hand or Spread
                    inst.MoveTo(CardZone.Arcanum, playerId);
                    player.Arcanum.Add(id);
                    drawn.Add(id);
                }
                else if (def?.MajorArcanaType == MajorArcanaType.Adept)
                {
                    // Adept cards must be purchased to keep; discard until purchase mechanic is implemented
                    inst.MoveTo(CardZone.Discard, -1);
                    session.Board.CommonDiscard.Add(id);
                }
                else
                {
                    // Minor Arcana → Hand as normal
                    inst.MoveTo(CardZone.Hand, playerId);
                    player.Hand.Add(id);
                    drawn.Add(id);
                }
            }

            session.EmitEvent(new CardsDrawnEvent(playerId, drawn.Count));
        }

        // ─── Step 4: Commune ──────────────────────────────────────────────────────

        public CommandResult HandleCommune(GameSession session, int playerId,
            IReadOnlyList<string> spreadCardIds, IReadOnlyList<string> handCardIds)
        {
            var player  = session.Players[playerId];

            // All provided IDs must currently belong to this player
            var allProvided = new HashSet<string>(spreadCardIds.Count + handCardIds.Count);
            foreach (var id in spreadCardIds) allProvided.Add(id);
            foreach (var id in handCardIds)   allProvided.Add(id);

            var playerCards = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) playerCards.Add(id);
            foreach (var id in player.Hand)   playerCards.Add(id);

            if (allProvided.Count != playerCards.Count)
                return CommandResult.Invalid("Commune must assign every card in Spread + Hand exactly once.");

            foreach (var id in allProvided)
                if (!playerCards.Contains(id))
                    return CommandResult.Invalid($"Card {id} does not belong to player {playerId}.");

            // Rebuild zones
            player.Spread.Clear();
            player.Hand.Clear();

            foreach (var id in spreadCardIds)
            {
                session.GetCard(id)?.MoveTo(CardZone.Spread, playerId);
                player.Spread.Add(id);
            }
            foreach (var id in handCardIds)
            {
                session.GetCard(id)?.MoveTo(CardZone.Hand, playerId);
                player.Hand.Add(id);
            }

            return CommandResult.Ok();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private ZodiacSign RollSign() => (ZodiacSign)(_rng.Next(1, 13)); // 1–12

        private static int AlignmentBonus(ZodiacSign playerSign, ZodiacSign cosmicSign)
        {
            if (playerSign == ZodiacSign.None || cosmicSign == ZodiacSign.None) return 0;
            if (playerSign == cosmicSign) return 3;
            if (Correspondence.PlanetFor(playerSign) == Correspondence.PlanetFor(cosmicSign)) return 2;
            if (Correspondence.ElementFor(playerSign) == Correspondence.ElementFor(cosmicSign)) return 1;
            return 0;
        }

        private static bool AgekeeperMatchesCosmic(GameSession session)
        {
            foreach (var p in session.Players)
                if (p.IsAgekeeper && p.CurrentSign == session.Board.CosmicAgeSign
                    && p.CurrentSign != ZodiacSign.None)
                    return true;
            return false;
        }

        private static void ReshuffleDiscard(GameSession session)
        {
            if (session.Board.CommonDiscard.Count == 0) return;
            var cards = new List<string>(session.Board.CommonDiscard);
            session.Board.CommonDiscard.Clear();
            var rng = new Random();
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
            foreach (var id in cards)
            {
                session.Board.CommonDeck.Push(id);
                session.GetCard(id)?.MoveTo(CardZone.Deck, -1);
            }
        }
    }
}
