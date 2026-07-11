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
                case 16: ResolveTower(session, drawerId, fateCardId);      break;
                case 13: ResolveDeath(session, drawerId, fateCardId);      break;
                case 19: ResolveSun(session, fateCardId);                  break;
                case 20: ResolveJudgement(session, drawerId, fateCardId);  break;
                case 11: ResolveJustice(session, fateCardId);              break;
                case 10: ResolveWheelOfFortune(session, fateCardId);       break;
                case 12: ResolveHangedMan(session, fateCardId);            break;

                // Async: Moon, Fool, Lovers — defer to GameLoop
                case 18:
                case  0:
                case  6:
                    SetFateNote(session, fateCardId, PendingFateNote(arcanaNumber));
                    return false;

                default:
                    SetFateNote(session, fateCardId, "Resolved when drawn — no further effect.");
                    break;
            }

            session.EmitEvent(new FateResolvedEvent(drawerId, fateCardId, arcanaNumber));
            return true;
        }

        static void SetFateNote(GameSession session, string? fateCardId, string note)
        {
            if (string.IsNullOrEmpty(fateCardId)) return;
            session.GetCard(fateCardId)?.SetFateResolutionNote(note);
        }

        static string PendingFateNote(int arcanaNumber) => arcanaNumber switch
        {
            18 => "Awaiting your keep-2 decision from the Moon draw.",
            0 => "Opponents are choosing a reagent gift from the Fool.",
            6 => "Awaiting the Lovers choice — draw two cards or take a reagent.",
            _ => "Awaiting resolution."
        };

        // ── Auto-resolving fates ──────────────────────────────────────────────────

        /// <summary>
        /// Tower (16): All Adept cards in every player's Arcanum are arrested (turned face-down).
        /// Arrested Adepts no longer contribute to alignment scoring until refreshed (1 Salt each).
        /// </summary>
        private void ResolveTower(GameSession session, int drawerId, string fateCardId)
        {
            int totalArrested = 0;
            var arrests = new List<(int playerId, IReadOnlyList<string> arrestedIds)>();
            foreach (var player in session.Players)
            {
                var arrested = new List<string>();
                foreach (var id in player.Arcanum)
                {
                    var inst = session.GetCard(id);
                    var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                    if (def?.MajorArcanaType == MajorArcanaType.Adept)
                    {
                        player.ArrestedAdepts.Add(id);
                        arrested.Add(id);
                        totalArrested++;
                    }
                }
                if (arrested.Count > 0)
                    arrests.Add((player.PlayerId, arrested));
            }
            SetFateNote(session, fateCardId,
                $"Already resolved: arrested {totalArrested} Adept{(totalArrested == 1 ? "" : "s")} table-wide. Now face-up in Arcanum.");
            session.EmitEvent(new TowerFateResolvedEvent(drawerId, totalArrested));
            ExchangeEventEmitter.EmitTower(session, drawerId, arrests);
        }

        /// <summary>Death (13): All players discard their entire Hand to the Common Deck.</summary>
        private static void ResolveDeath(GameSession session, int drawerId, string fateCardId)
        {
            int discarded = 0;
            var hands = new List<(int playerId, IReadOnlyList<string> handCardIds)>();
            var toReturn = new List<string>();
            foreach (var player in session.Players)
            {
                var handCopy = new List<string>(player.Hand);
                if (handCopy.Count > 0)
                    hands.Add((player.PlayerId, handCopy));
                foreach (var id in player.Hand)
                {
                    session.GetCard(id)?.MoveTo(CardZone.Deck, -1);
                    toReturn.Add(id);
                    discarded++;
                }
                player.Hand.Clear();
            }
            Shuffle(toReturn, new Random());
            foreach (var id in toReturn)
                session.Board.CommonDeck.Push(id);
            SetFateNote(session, fateCardId,
                $"Already resolved: all hands returned to the deck ({discarded} cards). Now face-up in Arcanum.");
            ExchangeEventEmitter.EmitDeath(session, hands);
            session.EmitEvent(new HarvestHandsClearedEvent(drawerId));
        }

        /// <summary>Sun (19): All players receive 1 of each Reagent.</summary>
        private static void ResolveSun(GameSession session, string fateCardId)
        {
            foreach (var player in session.Players)
            {
                player.AddReagent(ReagentType.Salt);
                player.AddReagent(ReagentType.Sulphur);
                player.AddReagent(ReagentType.AquaRegia);
                player.AddReagent(ReagentType.Vitriol);
                player.AddReagent(ReagentType.Quicksilver);
            }
            SetFateNote(session, fateCardId,
                "Already resolved: every player received one of each reagent. Now face-up in Arcanum.");
            ExchangeEventEmitter.EmitSun(session);
        }

        /// <summary>Judgement (20): Drawing player draws 1 card per lit Cauldron.</summary>
        private static void ResolveJudgement(GameSession session, int drawerId, string fateCardId)
        {
            var player  = session.Players[drawerId];
            var harvest = session.Rules?.Harvest;
            int count   = 0;
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                if (suit != Suit.None && player.IsCauldronLit(suit))
                    count++;

            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (session.Board.CommonDeck.Count == 0)
                    SpringRules.ReshuffleDiscardStatic(session);
                if (session.Board.CommonDeck.Count == 0) break;

                var id = session.Board.CommonDeck.Pop();
                drawn++;
                if (harvest != null)
                    harvest.RouteDrawnCard(session, drawerId, id);
                else
                {
                    var inst = session.GetCard(id);
                    if (inst == null) continue;
                    inst.MoveTo(CardZone.Hand, drawerId);
                    player.Hand.Add(id);
                }
            }
            SetFateNote(session, fateCardId,
                $"Already resolved: drew {drawn} card{(drawn == 1 ? "" : "s")} from lit cauldrons. Now face-up in Arcanum.");
            ExchangeEventEmitter.EmitJudgement(session, drawerId, drawn);
        }

        /// <summary>Justice (11): Duels and Gambits this round resolve as best-of-3.</summary>
        private static void ResolveJustice(GameSession session, string fateCardId)
        {
            var effects = session.Board.ContestEffects;
            effects.DuelBestOfThree = true;
            effects.GambitBestOfThree = true;
            session.Board.ContestEffects = effects;
            SetFateNote(session, fateCardId,
                "Already resolved: duels and gambits are best-of-three this age. Now face-up in Arcanum.");
        }

        /// <summary>Wheel of Fortune (10): All re-roll Zodiac; highest gets 2 Salt, lowest discards 1.</summary>
        private void ResolveWheelOfFortune(GameSession session, string fateCardId)
        {
            int highest = -1, lowest = 13;
            int highPid = -1, lowPid = -1;
            var outcomes = new List<(int playerId, int roll, bool gainedSalt, bool discardedCard)>();

            foreach (var player in session.Players)
            {
                int roll = _rng.Next(1, 13);
                player.CurrentSign = (ZodiacSign)roll;
                session.EmitEvent(new ZodiacRolledEvent(player.PlayerId, player.CurrentSign));

                if (roll > highest) { highest = roll; highPid = player.PlayerId; }
                if (roll < lowest)  { lowest  = roll; lowPid  = player.PlayerId; }
            }

            if (highPid >= 0)
                session.Players[highPid].AddReagent(ReagentType.Salt, 2);

            int discarded = 0;
            if (lowPid >= 0 && session.Players[lowPid].Hand.Count > 0)
            {
                var id = session.Players[lowPid].Hand[0];
                session.Players[lowPid].Hand.RemoveAt(0);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
                discarded = 1;
            }

            foreach (var player in session.Players)
            {
                bool gainedSalt = player.PlayerId == highPid;
                bool lostCard = player.PlayerId == lowPid && discarded > 0;
                outcomes.Add((player.PlayerId, (int)player.CurrentSign, gainedSalt, lostCard));
            }

            SetFateNote(session, fateCardId,
                discarded > 0
                    ? "Already resolved: all players re-rolled zodiac; highest gained 2 Salt, lowest lost 1 card. Now face-up in Arcanum."
                    : "Already resolved: all players re-rolled zodiac; highest gained 2 Salt. Now face-up in Arcanum.");
            ExchangeEventEmitter.EmitWheel(session, outcomes);
        }

        /// <summary>Hanged Man (12): Each player passes their Hand to the left (lower player id, wrapping).</summary>
        private static void ResolveHangedMan(GameSession session, string fateCardId)
        {
            int n = session.Players.Count;
            var hands = new List<List<string>>(n);
            for (int i = 0; i < n; i++)
                hands.Add(new List<string>(session.Players[i].Hand));

            for (int i = 0; i < n; i++)
            {
                int target = (i + 1) % n;
                session.Players[target].Hand.Clear();
                foreach (var id in hands[i])
                {
                    session.GetCard(id)?.MoveTo(CardZone.Hand, target);
                    session.Players[target].Hand.Add(id);
                }
            }

            SetFateNote(session, fateCardId,
                "Already resolved: every hand passed clockwise. Now face-up in Arcanum.");
            ExchangeEventEmitter.EmitHangedMan(session, hands);
        }

        // ── Async fate resolution (called by GameLoop after RequestAsync returns) ──

        /// <summary>
        /// Moon (18): Player chose 2 cards to keep from the 4 Moon-drawn cards.
        /// The unchosen 2 Moon cards return to the bottom of the deck.
        /// The player's existing hand cards are untouched.
        /// </summary>
        public CommandResult HandleMoonDecision(GameSession session, int playerId,
            IReadOnlyList<string> keepCardIds)
        {
            if (keepCardIds.Count != 2)
                return CommandResult.Invalid("The Moon: you must keep exactly 2 cards.");

            var player        = session.Players[playerId];
            var keepSet       = new HashSet<string>(keepCardIds);
            var moonDrawn     = session.Board.FateMoonDrawnCardIds;

            // Validate that the chosen cards are from the Moon draw and are minor arcana
            foreach (var id in keepCardIds)
            {
                if (!moonDrawn.Contains(id))
                    return CommandResult.Invalid($"The Moon: card {id} was not drawn by the Moon.");
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return CommandResult.Invalid($"The Moon: card {id} is a Major Arcana card and cannot be kept in Hand.");
            }

            // Return the unchosen Moon cards to the bottom of the deck
            var toReturn = new List<string>();
            foreach (var id in moonDrawn)
            {
                if (keepSet.Contains(id)) continue; // player keeps this one

                player.Hand.Remove(id);
                session.GetCard(id)?.MoveTo(CardZone.Deck, -1);
                toReturn.Add(id);
            }

            // Append to bottom (bottom of a Stack<T> = enumerated first, pushed last)
            var deckList = new List<string>(session.Board.CommonDeck);
            deckList.AddRange(toReturn);
            session.Board.CommonDeck.Clear();
            foreach (var id in deckList)
                session.Board.CommonDeck.Push(id);

            SetFateNote(session, FindFateCardId(session, playerId, 18),
                "Already resolved: kept 2 Moon cards; unchosen cards returned to the deck. Now face-up in Arcanum.");
            return CommandResult.Ok("Moon resolved.");
        }

        /// <summary>Fool (0): opponents receive 1 Reagent of their choice (called per opponent).</summary>
        public CommandResult HandleFoolReagentChoice(GameSession session, int chooserId,
            ReagentType reagentType)
        {
            session.Players[chooserId].AddReagent(reagentType);
            int drawerId = FindFoolDrawerPlayerId(session);
            if (drawerId >= 0)
                ExchangeEventEmitter.EmitFoolGift(session, drawerId, chooserId, reagentType);
            SetFateNote(session, FindDrawerFateCardId(session, 0),
                $"Already resolved: opponents received reagents from the Fool. Now face-up in Arcanum.");
            return CommandResult.Ok($"P{chooserId} received 1 {reagentType} (Fool).");
        }

        /// <summary>Lovers (6): target's choice resolves for the drawer.</summary>
        public CommandResult HandleLoversChoice(GameSession session, int drawerId,
            bool drawCards, ReagentType chosenReagent, int chooserId = -1)
        {
            var drawnIds = new List<string>();
            if (drawCards)
            {
                // Drawer draws 2 cards — routed through harvest so Major Arcana go to Arcanum
                var player  = session.Players[drawerId];
                var harvest = session.Rules?.Harvest;
                for (int i = 0; i < 2; i++)
                {
                    if (session.Board.CommonDeck.Count == 0)
                        SpringRules.ReshuffleDiscardStatic(session);
                    if (session.Board.CommonDeck.Count == 0) break;

                    var id = session.Board.CommonDeck.Pop();
                    if (harvest != null)
                        harvest.RouteDrawnCard(session, drawerId, id);
                    else
                    {
                        var inst = session.GetCard(id);
                        if (inst == null) continue;
                        inst.MoveTo(CardZone.Hand, drawerId);
                        player.Hand.Add(id);
                    }
                    drawnIds.Add(id);
                }
            }
            else
            {
                session.Players[drawerId].AddReagent(chosenReagent);
            }

            if (chooserId >= 0)
                ExchangeEventEmitter.EmitLoversChoice(session, drawerId, chooserId,
                    drawCards, chosenReagent, drawnIds);

            SetFateNote(session, FindFateCardId(session, drawerId, 6),
                drawCards
                    ? "Already resolved: Lovers choice drew two cards. Now face-up in Arcanum."
                    : $"Already resolved: Lovers choice granted {chosenReagent}. Now face-up in Arcanum.");
            return CommandResult.Ok("Lovers resolved.");
        }

        static string? FindFateCardId(GameSession session, int playerId, int arcanaNumber)
        {
            foreach (var (pid, fateId, num) in session.Board.PendingFateDecisions)
            {
                if (pid == playerId && num == arcanaNumber)
                    return fateId;
            }

            foreach (var player in session.Players)
            {
                if (player.PlayerId != playerId) continue;
                foreach (var id in player.Arcanum)
                {
                    var inst = session.GetCard(id);
                    if (inst == null) continue;
                    var def = session.Rules?.CardDatabase.GetById(inst.DefinitionId);
                    if (def?.MajorArcanaType == MajorArcanaType.Fate && def.ArcanaNumber == arcanaNumber)
                        return id;
                }
            }

            return null;
        }

        static int FindFoolDrawerPlayerId(GameSession session)
        {
            foreach (var (pid, _, num) in session.Board.PendingFateDecisions)
            {
                if (num == 0)
                    return pid;
            }

            foreach (var player in session.Players)
            {
                foreach (var id in player.Arcanum)
                {
                    var inst = session.GetCard(id);
                    if (inst == null) continue;
                    var def = session.Rules?.CardDatabase.GetById(inst.DefinitionId);
                    if (def?.MajorArcanaType == MajorArcanaType.Fate && def.ArcanaNumber == 0)
                        return player.PlayerId;
                }
            }

            return -1;
        }

        static string? FindDrawerFateCardId(GameSession session, int arcanaNumber)
        {
            foreach (var (pid, fateId, num) in session.Board.PendingFateDecisions)
            {
                if (num == arcanaNumber)
                    return fateId;
            }

            foreach (var player in session.Players)
            {
                foreach (var id in player.Arcanum)
                {
                    var inst = session.GetCard(id);
                    if (inst == null) continue;
                    var def = session.Rules?.CardDatabase.GetById(inst.DefinitionId);
                    if (def?.MajorArcanaType == MajorArcanaType.Fate && def.ArcanaNumber == arcanaNumber)
                        return id;
                }
            }

            return null;
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
