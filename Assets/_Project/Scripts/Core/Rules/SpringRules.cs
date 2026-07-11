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
    ///   Adept cards → held in PendingAdeptDecisions queue; player is prompted to Buy or Decline.
    /// </summary>
    public sealed class SpringRules : IHarvestService
    {
        private readonly Random _rng;
        private readonly ICardDatabase _db;
        private readonly CosmicEffectService? _cosmicEffect;
        private readonly FateCardResolver? _fateResolver;

        public SpringRules(ICardDatabase db, int? seed = null,
            CosmicEffectService? cosmicEffect = null,
            FateCardResolver? fateResolver = null)
        {
            _db           = db;
            _rng          = seed.HasValue ? new Random(seed.Value) : new Random();
            _cosmicEffect = cosmicEffect;
            _fateResolver = fateResolver;
        }

        // ─── Step 1: Cosmic Age roll ──────────────────────────────────────────────

        public void RollCosmicAge(GameSession session)
        {
            var sign = RollSign();
            session.Board.CosmicAgeSign = sign;
            session.EmitEvent(new CosmicAgeSetEvent(sign, Correspondence.PlanetFor(sign), Correspondence.ElementFor(sign)));
            _cosmicEffect?.Apply(session, sign);
        }

        // ─── Step 2: Personal Zodiac roll ─────────────────────────────────────────

        public void RollZodiac(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            var sign   = RollSign();
            player.CurrentSign = sign;
            player.PersonalCosmicEffects = CosmicEffectService.ComputePersonalEffects(
                sign, player.AstralHouses, session.Board.CosmicAgeSign);
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

            // Spread Element Match: each Spread card whose suit matches the Cosmic Age's element earns +1
            var cosmicElement = Correspondence.ElementFor(cosmicSign);
            if (cosmicElement != Element.None)
            {
                foreach (var cardId in player.Spread)
                {
                    var inst = session.GetCard(cardId);
                    var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                    if (def != null && Correspondence.ElementFor(def.Suit) == cosmicElement)
                        bonus++;
                }
            }

            // Adept card Aspects: each Adept in Arcanum contributes its Sign independently
            foreach (var cardId in player.Arcanum)
            {
                var inst = session.GetCard(cardId);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.Sign != ZodiacSign.None)
                    bonus += AlignmentBonus(def.Sign, cosmicSign);
            }

            // Astral Houses: each built house's sign is an independent Alignment source
            foreach (var houseSign in player.AstralHouses)
                bonus += AlignmentBonus(houseSign, cosmicSign);

            bonus += HarvestModifierService.SpreadPassiveBonus(session, playerId);
            bonus += HarvestModifierService.HouseDoublingBonus(session, playerId);

            // Board-wide Cosmic Age effect + player's personal effect (own sign + houses)
            int cosmicBonus = session.Board.CosmicEffect.HarvestBaseBonus
                            + player.PersonalCosmicEffects.HarvestBaseBonus;
            return 3 + cosmicBonus + bonus;
        }

        public void ExecuteHarvest(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            int target = CalculateHarvestCount(session, playerId);
            bool usePriestess = AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.PriestessArcana);
            int totalDraws = target + (usePriestess ? 2 : 0);
            int adeptsBefore = session.Board.PendingAdeptDecisions.Count;

            for (int i = 0; i < totalDraws; i++)
            {
                if (session.Board.CommonDeck.Count == 0)
                    ReshuffleDiscard(session);

                if (session.Board.CommonDeck.Count == 0)
                {
                    // Fates Intervene: all players discard their Hands into the Common Deck and reshuffle
                    FatesIntervene(session);
                    if (session.Board.CommonDeck.Count == 0)
                        break; // truly exhausted — stop harvest
                }

                var id = session.Board.CommonDeck.Pop();
                RouteDrawnCard(session, playerId, id);
            }

            if (usePriestess)
                session.Board.PendingPriestessReturns.Add(playerId);

            // Drawn count = cards dealt minus Adepts held in limbo (Fate + Minor count for the event)
            int adeptsQueued = session.Board.PendingAdeptDecisions.Count - adeptsBefore;
            session.EmitEvent(new CardsDrawnEvent(playerId, totalDraws - adeptsQueued));
        }

        /// <inheritdoc/>
        public bool RouteDrawnCard(GameSession session, int playerId, string cardId,
            ICollection<string>? minorPool = null)
        {
            var player = session.Players[playerId];
            var inst   = session.GetCard(cardId);
            if (inst == null) return false;

            var def = _db.GetById(inst.DefinitionId);

            if (def?.MajorArcanaType == MajorArcanaType.Fate)
            {
                if (IsMajorAlreadyCommitted(session, cardId))
                {
                    DiscardDuplicateMajor(session, cardId, inst);
                    return false;
                }

                // Fate cards go directly to Arcanum, face-up; never enter Hand or Spread
                inst.MoveTo(CardZone.Arcanum, playerId);
                player.Arcanum.Add(cardId);

                if (_fateResolver != null)
                {
                    bool resolved = _fateResolver.Resolve(session, playerId, cardId, def.ArcanaNumber);
                    if (!resolved)
                        TryQueueFateDecision(session, playerId, cardId, def.ArcanaNumber);
                }
                else
                {
                    TryQueueFateDecision(session, playerId, cardId, def.ArcanaNumber);
                }
                return false;
            }

            if (def?.MajorArcanaType == MajorArcanaType.Adept)
            {
                if (IsMajorAlreadyCommitted(session, cardId))
                {
                    DiscardDuplicateMajor(session, cardId, inst);
                    return false;
                }

                // Adept sits in limbo until the player buys or declines; never enters Hand or Spread
                inst.MoveTo(CardZone.Deck, -1);
                TryQueueAdeptDecision(session, playerId, cardId);
                return false;
            }

            // Minor Arcana → Hand
            inst.MoveTo(CardZone.Hand, playerId);
            player.Hand.Add(cardId);
            minorPool?.Add(cardId);
            return true;
        }

        // ─── Step 4 extension: Adept buy / decline ─────────────────────────────────

        public CommandResult HandleBuyAdept(GameSession session, int playerId, string adeptCardId,
            IReadOnlyList<string> paymentCardIds, string? swapOutAdeptId = null)
        {
            var player = session.Players[playerId];

            if (paymentCardIds.Count != 3)
                return CommandResult.Invalid("Purchasing an Adept costs exactly 3 cards.");

            // Payment must be minor arcana only — Fate/Adept cards are not valid currency
            foreach (var id in paymentCardIds)
            {
                var pinst = session.GetCard(id);
                var pdef  = pinst != null ? _db.GetById(pinst.DefinitionId) : null;
                if (pdef?.IsMajorArcana == true)
                    return CommandResult.Invalid(
                        $"Card {id} is a Major Arcana card and cannot be used as Adept payment.");
            }

            // Ownership check
            var playerCards = BuildCardSet(player);
            foreach (var id in paymentCardIds)
                if (!playerCards.Contains(id))
                    return CommandResult.Invalid($"Card {id} does not belong to player {playerId}.");

            // Arcanum limit: 2 (3 with The Hermit in Arcanum)
            int limit      = ArcanaLimitFor(session, player);
            int adeptCount = CountAdeptsInArcanum(session, player);

            if (adeptCount >= limit)
            {
                if (swapOutAdeptId == null)
                    return CommandResult.Invalid(
                        $"Arcanum is full ({limit} Adepts). Specify SwapOutAdeptId to swap.");

                // Remove the outgoing Adept and return it to Common Deck
                if (!player.Arcanum.Remove(swapOutAdeptId))
                    return CommandResult.Invalid($"SwapOut card {swapOutAdeptId} not in Arcanum.");

                WardRefundHelper.RefundAdeptWards(player, swapOutAdeptId);

                var outInst = session.GetCard(swapOutAdeptId);
                outInst?.MoveTo(CardZone.Deck, -1);
                // Shuffle back into deck rather than discard (swap rule)
                session.Board.CommonDeck.Push(swapOutAdeptId);
            }

            // Discard the 3 payment cards
            foreach (var id in paymentCardIds)
            {
                player.Spread.Remove(id);
                player.Hand.Remove(id);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }

            // Place the Adept into Arcanum
            session.GetCard(adeptCardId)?.MoveTo(CardZone.Arcanum, playerId);
            player.Arcanum.Add(adeptCardId);

            session.EmitEvent(new AdeptPurchasedEvent(playerId, adeptCardId));
            return CommandResult.Ok("Adept purchased.");
        }

        public CommandResult HandleDeclineAdept(GameSession session, int playerId, string adeptCardId)
        {
            // Discard the Adept back to the Common Discard pile (once per instance)
            session.GetCard(adeptCardId)?.MoveTo(CardZone.Discard, -1);
            if (!session.Board.CommonDiscard.Contains(adeptCardId))
                session.Board.CommonDiscard.Add(adeptCardId);
            session.EmitEvent(new AdeptDeclinedEvent(playerId, adeptCardId));
            return CommandResult.Ok("Adept declined.");
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Max Adept cards allowed in Arcanum. The Hermit grants +1.</summary>
        private int ArcanaLimitFor(GameSession session, PlayerState player)
        {
            foreach (var id in player.Arcanum)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.ArcanaNumber == 9) // The Hermit
                    return 3;
            }
            return 2;
        }

        /// <summary>Count only Adept-type cards in the player's Arcanum.</summary>
        private int CountAdeptsInArcanum(GameSession session, PlayerState player)
        {
            int count = 0;
            foreach (var id in player.Arcanum)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept)
                    count++;
            }
            return count;
        }

        private static HashSet<string> BuildCardSet(PlayerState player)
        {
            var set = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) set.Add(id);
            foreach (var id in player.Hand)   set.Add(id);
            return set;
        }

        // ─── Step 4: Commune ──────────────────────────────────────────────────────

        public CommandResult HandleCommune(GameSession session, int playerId,
            IReadOnlyList<string> spreadCardIds, IReadOnlyList<string> handCardIds)
        {
            if (session.CardLockActive)
                return CommandResult.Invalid("Cards are locked until Winter.");

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

            // Hand is capped during Commune; spread has no round limit until Winter discard.
            int handLimit = PlayerLimitService.GetHandLimit(session, player);
            if (handCardIds.Count > handLimit)
                return CommandResult.Invalid(
                    $"Hand cannot exceed {handLimit} cards (submitted {handCardIds.Count}).");

            // Major Arcana cards must never be assigned to Spread or Hand
            foreach (var id in spreadCardIds)
            {
                var cinst = session.GetCard(id);
                var cdef  = cinst != null ? _db.GetById(cinst.DefinitionId) : null;
                if (cdef?.IsMajorArcana == true)
                    return CommandResult.Invalid(
                        $"Card {id} is a Major Arcana card and cannot be placed in the Spread.");
            }
            foreach (var id in handCardIds)
            {
                var cinst = session.GetCard(id);
                var cdef  = cinst != null ? _db.GetById(cinst.DefinitionId) : null;
                if (cdef?.IsMajorArcana == true)
                    return CommandResult.Invalid(
                        $"Card {id} is a Major Arcana card and cannot be placed in the Hand.");
            }

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

        /// <summary>
        /// Triggered when the Common Deck is exhausted mid-Harvest after a reshuffle attempt.
        /// All players discard their entire Hands back into the Common Deck, which is then reshuffled.
        /// This is the "Fates intervene" rule — a rare catastrophe when the deck runs completely dry.
        /// </summary>
        private static void FatesIntervene(GameSession session)
        {
            int returned = 0;
            foreach (var player in session.Players)
            {
                for (int j = player.Hand.Count - 1; j >= 0; j--)
                {
                    var cardId = player.Hand[j];
                    player.Hand.RemoveAt(j);
                    session.Board.CommonDiscard.Add(cardId);
                    session.GetCard(cardId)?.MoveTo(CardZone.Discard, -1);
                    returned++;
                }
            }

            session.EmitEvent(new HarvestCatastropheEvent(returned));
            ReshuffleDiscard(session);
        }

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

        public static void ReshuffleDiscardStatic(GameSession session) => ReshuffleDiscard(session);

        private static void ReshuffleDiscard(GameSession session)
        {
            if (session.Board.CommonDiscard.Count == 0) return;
            var cards = DedupeDiscardPile(session.Board.CommonDiscard);
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

        static bool IsMajorAlreadyCommitted(GameSession session, string cardId)
        {
            foreach (var (_, id) in session.Board.PendingAdeptDecisions)
                if (id == cardId) return true;
            foreach (var (_, id, _) in session.Board.PendingFateDecisions)
                if (id == cardId) return true;
            foreach (var player in session.Players)
            {
                if (player.Arcanum.Contains(cardId)) return true;
            }
            return false;
        }

        static void TryQueueAdeptDecision(GameSession session, int playerId, string cardId)
        {
            foreach (var (_, id) in session.Board.PendingAdeptDecisions)
            {
                if (id == cardId) return;
            }
            session.Board.PendingAdeptDecisions.Add((playerId, cardId));
        }

        static void TryQueueFateDecision(GameSession session, int playerId, string cardId, int arcanaNumber)
        {
            foreach (var (_, id, _) in session.Board.PendingFateDecisions)
            {
                if (id == cardId) return;
            }
            session.Board.PendingFateDecisions.Add((playerId, cardId, arcanaNumber));
        }

        static void DiscardDuplicateMajor(GameSession session, string cardId, CardInstance inst)
        {
            if (!session.Board.CommonDiscard.Contains(cardId))
                session.Board.CommonDiscard.Add(cardId);
            inst.MoveTo(CardZone.Discard, -1);
        }

        static List<string> DedupeDiscardPile(IReadOnlyList<string> discard)
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
    }
}
