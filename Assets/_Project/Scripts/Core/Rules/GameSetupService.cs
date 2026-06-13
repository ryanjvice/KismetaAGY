using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Performs the full pre-game setup sequence:
    ///   1. Creates one CardInstance per definition and registers it with the session.
    ///   2. Shuffles all Kismeta (common) instances into BoardState.CommonDeck.
    ///   3. Builds the Quickplay Crucible deck for the player count and deals 4 per player.
    ///   4. Deals 1 starter Spread card to each player (redeals Major Arcana).
    ///   5. Sets Player 0 as the first Agekeeper.
    ///
    /// M2: Agekeeper assigned by index; Crucible counts are Quickplay standard.
    /// </summary>
    public sealed class GameSetupService
    {
        // Quickplay crucible counts per group, indexed by player count (2-4).
        // Format: [playerCount] = count
        private static int CrucibleGroupCount(CrucibleGroup group, int playerCount) => group switch
        {
            CrucibleGroup.A => playerCount == 2 ? 3 : playerCount == 3 ? 3 : 3,
            CrucibleGroup.B => playerCount == 2 ? 4 : playerCount == 3 ? 5 : 5,
            CrucibleGroup.C => playerCount == 2 ? 1 : playerCount == 3 ? 3 : 5,
            CrucibleGroup.D => playerCount == 2 ? 0 : playerCount == 3 ? 0 : 1,
            _               => 0
        };

        private readonly ICardDatabase _db;
        private readonly Random _rng;

        public GameSetupService(ICardDatabase db, int? seed = null)
        {
            _db  = db;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public CommandResult Setup(GameSession session)
        {
            int playerCount = session.Players.Count;

            // Separate definitions by deck
            var kismetaDefs  = new List<CardDefinition>();
            var crucibleDefs = new List<CardDefinition>();

            foreach (var def in _db.GetAll())
            {
                if (def.Deck == Deck.Kismeta)  kismetaDefs.Add(def);
                if (def.Deck == Deck.Crucible)  crucibleDefs.Add(def);
            }

            // Create + register all instances
            var kismetaInsts  = CreateAndRegister(kismetaDefs,  session);
            var crucibleInsts = CreateAndRegister(crucibleDefs, session);

            // Shuffle common deck
            Shuffle(kismetaInsts);
            foreach (var inst in kismetaInsts)
            {
                session.Board.CommonDeck.Push(inst.InstanceId);
                inst.MoveTo(CardZone.Deck, -1);
            }

            // Build and deal Crucible deck
            var crucibleDeckInsts = BuildCrucibleDeck(crucibleInsts, playerCount);
            foreach (var inst in crucibleDeckInsts)
            {
                session.Board.CrucibleDeck.Push(inst.InstanceId);
                inst.MoveTo(CardZone.Deck, -1);
            }

            DealCrucibleCards(session, crucibleDeckInsts);

            // Assign a random Codex variant to each player.
            AssignCodexVariants(session);

            // Starter Spread card per player
            DealStarterSpreadCards(session);

            // Set Agekeeper
            session.Players[0].IsAgekeeper = true;

            session.EmitEvent(new GameSetupCompleteEvent(playerCount));
            return CommandResult.Ok("Game setup complete.");
        }

        private static List<CardInstance> CreateAndRegister(List<CardDefinition> defs, GameSession session)
        {
            var list = new List<CardInstance>(defs.Count);
            foreach (var def in defs)
            {
                var inst = new CardInstance($"inst-{def.Id}", def.Id, CardZone.Deck, -1);
                session.RegisterCard(inst);
                list.Add(inst);
            }
            return list;
        }

        private List<CardInstance> BuildCrucibleDeck(List<CardInstance> allCrucible, int playerCount)
        {
            var result = new List<CardInstance>();
            int pc = Math.Clamp(playerCount, 2, 4);

            foreach (var group in new[] { CrucibleGroup.A, CrucibleGroup.B, CrucibleGroup.C, CrucibleGroup.D })
            {
                int needed = CrucibleGroupCount(group, pc);
                if (needed == 0) continue;

                var groupInsts = new List<CardInstance>();
                foreach (var inst in allCrucible)
                {
                    var def = _db.GetById(inst.DefinitionId);
                    if (def?.CrucibleGroup == group) groupInsts.Add(inst);
                }

                Shuffle(groupInsts);
                for (int i = 0; i < Math.Min(needed, groupInsts.Count); i++)
                    result.Add(groupInsts[i]);
            }

            Shuffle(result);
            return result;
        }

        private static void DealCrucibleCards(GameSession session, List<CardInstance> crucibleDeck)
        {
            int playerCount = session.Players.Count;
            int perPlayer   = crucibleDeck.Count / playerCount;

            // Pop from the deck stack so it is empty after dealing (all cards go to players).
            foreach (var player in session.Players)
            {
                for (int i = 0; i < perPlayer && session.Board.CrucibleDeck.Count > 0; i++)
                {
                    var id   = session.Board.CrucibleDeck.Pop();
                    var inst = session.GetCard(id);
                    if (inst == null) continue;

                    inst.MoveTo(CardZone.Deck, player.PlayerId);
                    var slot = new PlayerCrucibleSlot(id);
                    slot.PlaceCoal();
                    player.CrucibleSlots.Add(slot);
                }
            }
        }

        private static readonly CodexVariant[] AllCodexVariants =
            { CodexVariant.A, CodexVariant.B, CodexVariant.C, CodexVariant.D };

        private void AssignCodexVariants(GameSession session)
        {
            // Build a shuffled pool; each player must receive a unique variant.
            var pool = new List<CodexVariant>(AllCodexVariants);
            Shuffle(pool);

            for (int i = 0; i < session.Players.Count; i++)
            {
                var variant = pool[i % pool.Count];
                session.Players[i].AssignedCodex = variant;
                session.EmitEvent(new CodexAssignedEvent(session.Players[i].PlayerId, variant));
            }
        }

        private void DealStarterSpreadCards(GameSession session)
        {
            foreach (var player in session.Players)
            {
                for (int attempt = 0; attempt < 15; attempt++)
                {
                    if (session.Board.CommonDeck.Count == 0) break;
                    var id   = session.Board.CommonDeck.Pop();
                    var inst = session.GetCard(id);
                    var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;

                    if (def != null && def.IsMajorArcana)
                    {
                        session.Board.CommonDiscard.Add(id);
                        continue;
                    }

                    inst?.MoveTo(CardZone.Spread, player.PlayerId);
                    player.Spread.Add(id);
                    break;
                }
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
