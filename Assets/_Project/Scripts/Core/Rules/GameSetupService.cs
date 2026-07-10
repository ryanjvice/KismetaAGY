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
    ///   3. Builds the Crucible deck (curated per mode or Let the Fates Decide).
    ///   4. Deals 4 Crucible cards per player.
    ///   5. Deals 1 starter Spread card to each player (redeals Major Arcana).
    ///   6. Sets the first Agekeeper from <see cref="GameSession.FirstAgekeeperPlayerId"/>.
    /// </summary>
    public sealed class GameSetupService
    {
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

            var kismetaDefs  = new List<CardDefinition>();
            var crucibleDefs = new List<CardDefinition>();

            foreach (var def in _db.GetAll())
            {
                if (def.Deck == Deck.Kismeta)  kismetaDefs.Add(def);
                if (def.Deck == Deck.Crucible) crucibleDefs.Add(def);
            }

            var kismetaInsts = CreateAndRegister(kismetaDefs, session);
            var cruciblePool = CreateInstances(crucibleDefs);

            Shuffle(kismetaInsts);
            foreach (var inst in kismetaInsts)
            {
                session.Board.CommonDeck.Push(inst.InstanceId);
                inst.MoveTo(CardZone.Deck, -1);
            }

            var crucibleDeckInsts = session.CrucibleBuild == CrucibleBuildMode.LetTheFatesDecide
                ? BuildFatesCrucibleDeck(cruciblePool, playerCount)
                : BuildCuratedCrucibleDeck(cruciblePool, session.Mode, playerCount);

            foreach (var inst in crucibleDeckInsts)
            {
                session.RegisterCard(inst);
                session.Board.CrucibleDeck.Push(inst.InstanceId);
                inst.MoveTo(CardZone.Deck, -1);
            }

            DealCrucibleCards(session, crucibleDeckInsts);
            AssignCodexVariants(session);
            DealStarterSpreadCards(session);
            SetFirstAgekeeper(session);

            session.EmitEvent(new GameSetupCompleteEvent(playerCount));
            return CommandResult.Ok("Game setup complete.");
        }

        private static void SetFirstAgekeeper(GameSession session)
        {
            int id = session.FirstAgekeeperPlayerId;
            if (id < 0 || id >= session.Players.Count)
                id = 0;

            foreach (var player in session.Players)
                player.IsAgekeeper = false;

            session.Players[id].IsAgekeeper = true;
            session.EmitEvent(new FirstAgekeeperDeterminedEvent(id));
        }

        private static List<CardInstance> CreateInstances(List<CardDefinition> defs)
        {
            var list = new List<CardInstance>(defs.Count);
            foreach (var def in defs)
                list.Add(new CardInstance($"inst-{def.Id}", def.Id, CardZone.Deck, -1));
            return list;
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

        private List<CardInstance> BuildCuratedCrucibleDeck(
            List<CardInstance> allCrucible, GameMode mode, int playerCount)
        {
            var result = new List<CardInstance>();
            int pc = Math.Clamp(playerCount, 2, 4);

            foreach (var group in new[] { CrucibleGroup.A, CrucibleGroup.B, CrucibleGroup.C, CrucibleGroup.D })
            {
                int needed = CuratedGroupCount(mode, group, pc);
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

        private List<CardInstance> BuildFatesCrucibleDeck(List<CardInstance> allCrucible, int playerCount)
        {
            var pool = new List<CardInstance>(allCrucible);
            Shuffle(pool);

            int needed = playerCount * 4;
            var result = new List<CardInstance>(needed);
            for (int i = 0; i < Math.Min(needed, pool.Count); i++)
                result.Add(pool[i]);

            Shuffle(result);
            return result;
        }

        /// <summary>Curated counts from Rules/setup.md §IV.I.</summary>
        private static int CuratedGroupCount(GameMode mode, CrucibleGroup group, int playerCount) =>
            (mode, group, playerCount) switch
            {
                (GameMode.Quickplay, CrucibleGroup.A, 2) => 3,
                (GameMode.Quickplay, CrucibleGroup.B, 2) => 4,
                (GameMode.Quickplay, CrucibleGroup.C, 2) => 1,
                (GameMode.Quickplay, CrucibleGroup.A, 3) => 3,
                (GameMode.Quickplay, CrucibleGroup.B, 3) => 6,
                (GameMode.Quickplay, CrucibleGroup.C, 3) => 3,
                (GameMode.Quickplay, CrucibleGroup.A, 4) => 4,
                (GameMode.Quickplay, CrucibleGroup.B, 4) => 7,
                (GameMode.Quickplay, CrucibleGroup.C, 4) => 5,

                (GameMode.Standard, CrucibleGroup.A, 2) => 1,
                (GameMode.Standard, CrucibleGroup.B, 2) => 4,
                (GameMode.Standard, CrucibleGroup.C, 2) => 2,
                (GameMode.Standard, CrucibleGroup.D, 2) => 1,
                (GameMode.Standard, CrucibleGroup.A, 3) => 2,
                (GameMode.Standard, CrucibleGroup.B, 3) => 5,
                (GameMode.Standard, CrucibleGroup.C, 3) => 4,
                (GameMode.Standard, CrucibleGroup.D, 3) => 1,
                (GameMode.Standard, CrucibleGroup.A, 4) => 2,
                (GameMode.Standard, CrucibleGroup.B, 4) => 7,
                (GameMode.Standard, CrucibleGroup.C, 4) => 5,
                (GameMode.Standard, CrucibleGroup.D, 4) => 2,

                (GameMode.MagnusAlchemist, CrucibleGroup.B, 2) => 2,
                (GameMode.MagnusAlchemist, CrucibleGroup.C, 2) => 4,
                (GameMode.MagnusAlchemist, CrucibleGroup.D, 2) => 2,
                (GameMode.MagnusAlchemist, CrucibleGroup.B, 3) => 3,
                (GameMode.MagnusAlchemist, CrucibleGroup.C, 3) => 6,
                (GameMode.MagnusAlchemist, CrucibleGroup.D, 3) => 3,
                (GameMode.MagnusAlchemist, CrucibleGroup.B, 4) => 5,
                (GameMode.MagnusAlchemist, CrucibleGroup.C, 4) => 7,
                (GameMode.MagnusAlchemist, CrucibleGroup.D, 4) => 4,

                _ => 0
            };

        private static void DealCrucibleCards(GameSession session, List<CardInstance> crucibleDeck)
        {
            int playerCount = session.Players.Count;
            int perPlayer   = crucibleDeck.Count / playerCount;

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
                        session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
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
