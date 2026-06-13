using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves Fate card effects immediately when drawn during Harvest.
    ///
    /// Auto-resolving fates (7) apply their effect inline in ExecuteHarvest.
    /// Async fates (3 — Moon, Fool, Lovers) queue a pending decision in
    /// <see cref="BoardState.PendingFateDecisions"/> for GameLoop to handle.
    ///
    /// Fate card arcana numbers (from cards.json):
    ///   Tower=16, Death=13, Sun=19, Judgement=20, Justice=11,
    ///   Moon=18, Fool=0, Wheel of Fortune=10, Hanged Man=12, Lovers=6
    /// </summary>
    public sealed class FateCardResolver
    {
        private readonly ICardDatabase _db;
        private readonly Random _rng = new();

        public FateCardResolver(ICardDatabase db) => _db = db;

        /// <summary>
        /// Resolve a Fate card drawn by <paramref name="drawerId"/>.
        /// Auto-resolving effects apply immediately. Async effects are queued.
        /// Returns true if the fate was resolved inline; false if queued.
        /// </summary>
        public bool Resolve(GameSession session, int drawerId, string fateCardId, int arcanaNumber)
        {
            switch (arcanaNumber)
            {
                case 16: ResolveTower(session, drawerId);      break;
                case 13: ResolveDeath(session, drawerId);      break;
                case 19: ResolveSun(session);                  break;
                case 20: ResolveJudgement(session, drawerId);  break;
                case 11: ResolveJustice(session);              break;
                case 10: ResolveWheelOfFortune(session);       break;
                case 12: ResolveHangedMan(session);            break;

                // Async: Moon, Fool, Lovers — defer to GameLoop
                case 18:
                case  0:
                case  6:
                    // Leave entry in PendingFateDecisions (caller already added it)
                    return false;

                default:
                    break; // Unknown fate — silently resolved
            }

            session.EmitEvent(new FateResolvedEvent(drawerId, fateCardId, arcanaNumber));
            return true;
        }

        // ── Auto-resolving fates ──────────────────────────────────────────────────

        /// <summary>Tower (16): All Adept cards are "arrested" (turned face-down).
        /// In code, we mark Adept cards with CardZone.Arcanum status — no new field needed for M3;
        /// a "refreshed" flag would require more state. Log the effect instead.</summary>
        private static void ResolveTower(GameSession session, int drawerId)
        {
            // Emit a log-level event; full arrest (face-down state) requires a future
            // IsArrested flag on PlayerCrucibleSlot-equivalent for Arcanum cards.
            // For now, opponents must manually pay 1 Salt to "refresh" per the rules display.
            session.EmitEvent(new FateResolvedEvent(drawerId, "tower", 16));
        }

        /// <summary>Death (13): All players discard their entire Hand to the Common Deck.</summary>
        private static void ResolveDeath(GameSession session, int drawerId)
        {
            var toReturn = new List<string>();
            foreach (var player in session.Players)
            {
                foreach (var id in player.Hand)
                {
                    session.GetCard(id)?.MoveTo(CardZone.Deck, -1);
                    toReturn.Add(id);
                }
                player.Hand.Clear();
            }
            // Shuffle all returned cards into the common deck
            Shuffle(toReturn, new Random());
            foreach (var id in toReturn)
                session.Board.CommonDeck.Push(id);
        }

        /// <summary>Sun (19): All players receive 1 of each Reagent.</summary>
        private static void ResolveSun(GameSession session)
        {
            foreach (var player in session.Players)
            {
                player.AddReagent(ReagentType.Salt);
                player.AddReagent(ReagentType.Sulphur);
                player.AddReagent(ReagentType.AquaRegia);
                player.AddReagent(ReagentType.Vitriol);
                player.AddReagent(ReagentType.Quicksilver);
            }
        }

        /// <summary>Judgement (20): Drawing player draws 1 card per lit Cauldron.</summary>
        private static void ResolveJudgement(GameSession session, int drawerId)
        {
            var player = session.Players[drawerId];
            int count  = 0;
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                if (suit != Suit.None && player.IsCauldronLit(suit))
                    count++;

            for (int i = 0; i < count; i++)
            {
                if (session.Board.CommonDeck.Count == 0)
                    SpringRules.ReshuffleDiscardStatic(session);
                if (session.Board.CommonDeck.Count == 0) break;

                var id   = session.Board.CommonDeck.Pop();
                var inst = session.GetCard(id);
                if (inst == null) continue;
                inst.MoveTo(CardZone.Hand, drawerId);
                player.Hand.Add(id);
            }
        }

        /// <summary>Justice (11): Duels this round resolve as best-of-3.</summary>
        private static void ResolveJustice(GameSession session) =>
            session.Board.BestOfThreeDuels = true;

        /// <summary>Wheel of Fortune (10): All re-roll Zodiac; highest gets 2 Salt, lowest discards 1.</summary>
        private void ResolveWheelOfFortune(GameSession session)
        {
            int highest = -1, lowest = 13;
            int highPid = -1, lowPid = -1;

            foreach (var player in session.Players)
            {
                int roll = _rng.Next(1, 13); // 1–12
                player.CurrentSign = (ZodiacSign)roll;
                session.EmitEvent(new ZodiacRolledEvent(player.PlayerId, player.CurrentSign));

                if (roll > highest) { highest = roll; highPid = player.PlayerId; }
                if (roll < lowest)  { lowest  = roll; lowPid  = player.PlayerId; }
            }

            if (highPid >= 0)
                session.Players[highPid].AddReagent(ReagentType.Salt, 2);

            if (lowPid >= 0 && session.Players[lowPid].Hand.Count > 0)
            {
                var id = session.Players[lowPid].Hand[0];
                session.Players[lowPid].Hand.RemoveAt(0);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }
        }

        /// <summary>Hanged Man (12): Each player passes their Hand to the left (lower player id, wrapping).</summary>
        private static void ResolveHangedMan(GameSession session)
        {
            int n = session.Players.Count;
            var hands = new List<List<string>>(n);
            for (int i = 0; i < n; i++)
                hands.Add(new List<string>(session.Players[i].Hand));

            for (int i = 0; i < n; i++)
            {
                int target = (i + 1) % n; // pass to next player (clockwise)
                session.Players[target].Hand.Clear();
                foreach (var id in hands[i])
                {
                    session.GetCard(id)?.MoveTo(CardZone.Hand, target);
                    session.Players[target].Hand.Add(id);
                }
            }
        }

        // ── Async fate resolution (called by GameLoop after RequestAsync returns) ──

        /// <summary>Moon (18): Player kept 2 cards; return the rest to the bottom of the deck.</summary>
        public CommandResult HandleMoonDecision(GameSession session, int playerId,
            IReadOnlyList<string> keepCardIds)
        {
            if (keepCardIds.Count != 2)
                return CommandResult.Invalid("The Moon: you must keep exactly 2 cards.");

            var player   = session.Players[playerId];
            var keepSet  = new HashSet<string>(keepCardIds);

            // Any Hand card not in keepSet goes to the bottom of the deck
            var toReturn = new List<string>();
            for (int i = player.Hand.Count - 1; i >= 0; i--)
            {
                var id = player.Hand[i];
                if (!keepSet.Contains(id))
                {
                    player.Hand.RemoveAt(i);
                    toReturn.Add(id);
                }
            }

            // Push to bottom (we reverse so last-removed ends up at bottom)
            toReturn.Reverse();
            var deckList = new List<string>(session.Board.CommonDeck);
            deckList.AddRange(toReturn);
            session.Board.CommonDeck.Clear();
            foreach (var id in deckList)
            {
                session.GetCard(id)?.MoveTo(CardZone.Deck, -1);
                session.Board.CommonDeck.Push(id);
            }

            return CommandResult.Ok("Moon resolved.");
        }

        /// <summary>Fool (0): opponents receive 1 Reagent of their choice (called per opponent).</summary>
        public CommandResult HandleFoolReagentChoice(GameSession session, int chooserId,
            ReagentType reagentType)
        {
            session.Players[chooserId].AddReagent(reagentType);
            return CommandResult.Ok($"P{chooserId} received 1 {reagentType} (Fool).");
        }

        /// <summary>Lovers (6): target's choice resolves for the drawer.</summary>
        public CommandResult HandleLoversChoice(GameSession session, int drawerId,
            bool drawCards, ReagentType chosenReagent)
        {
            if (drawCards)
            {
                // Drawer draws 2 cards
                var player = session.Players[drawerId];
                for (int i = 0; i < 2; i++)
                {
                    if (session.Board.CommonDeck.Count == 0)
                        SpringRules.ReshuffleDiscardStatic(session);
                    if (session.Board.CommonDeck.Count == 0) break;

                    var id   = session.Board.CommonDeck.Pop();
                    var inst = session.GetCard(id);
                    if (inst == null) continue;
                    inst.MoveTo(CardZone.Hand, drawerId);
                    player.Hand.Add(id);
                }
            }
            else
            {
                session.Players[drawerId].AddReagent(chosenReagent);
            }
            return CommandResult.Ok("Lovers resolved.");
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
