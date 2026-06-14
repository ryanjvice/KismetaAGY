using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Summer social combat: Duel and Gambit resolution, and releasing Arrested cards.
    ///
    /// Duel   — Attacker antes a Spread card; dice roll decides who steals the ante.
    /// Gambit — Attacker offers an Active Crucible or Adept card; defender pays Ward reagents to enter;
    ///          winner takes both cards (loser's card is arrested).
    /// FreeArrested — Spend 1 Salt to un-arrest a Crucible slot (during Summer only).
    /// </summary>
    public sealed class CombatRules
    {
        private readonly Random _rng;

        public CombatRules(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ── Duel ──────────────────────────────────────────────────────────────────

        public CommandResult TryDuel(GameSession session, int attackerId, int defenderId,
            string anteCardId)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot Duel yourself.");

            var attacker = session.Players[attackerId];
            var defender = session.Players[defenderId];

            if (!attacker.Spread.Contains(anteCardId))
                return CommandResult.Invalid("Ante card must be in your Spread.");

            // Each side rolls 1–12; attacker wins ties
            int attackRoll = _rng.Next(1, 13);
            int defendRoll = _rng.Next(1, 13);
            bool attackerWins = attackRoll >= defendRoll;

            int winnerId = attackerWins ? attackerId : defenderId;
            int loserId  = attackerWins ? defenderId : attackerId;

            // Attacker's ante card moves to the winner's Spread
            attacker.Spread.Remove(anteCardId);
            session.GetCard(anteCardId)?.MoveTo(CardZone.Spread, winnerId);

            if (attackerWins)
            {
                attacker.Spread.Add(anteCardId); // stays with attacker
            }
            else
            {
                defender.Spread.Add(anteCardId); // defender takes it
            }

            // Defender antes their top Spread card (or nothing if empty)
            if (defender.Spread.Count > 0 && !attackerWins)
            {
                // Defender loses their top Spread card to attacker
                var defCard = defender.Spread[0];
                defender.Spread.RemoveAt(0);
                attacker.Spread.Add(defCard);
                session.GetCard(defCard)?.MoveTo(CardZone.Spread, attackerId);
            }

            session.EmitEvent(new DuelResolvedEvent(
                attackerId, defenderId, attackRoll, defendRoll, winnerId, anteCardId));
            return CommandResult.Ok(
                $"Duel: P{attackerId}({attackRoll}) vs P{defenderId}({defendRoll}) → P{winnerId} wins.");
        }

        // ── Gambit ────────────────────────────────────────────────────────────────

        public CommandResult TryGambit(GameSession session, int attackerId, int defenderId,
            string offeredCardId)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot Gambit yourself.");

            var attacker = session.Players[attackerId];
            var defender = session.Players[defenderId];

            // Offered card must be in an Active Crucible slot or Attacker's Arcanum
            bool offeredInCrucible = attacker.CrucibleSlots.Exists(
                s => s.CardInstanceId == offeredCardId && s.State == CrucibleCardState.Active);
            bool offeredInArcanum  = attacker.Arcanum.Contains(offeredCardId);

            if (!offeredInCrucible && !offeredInArcanum)
                return CommandResult.Invalid(
                    "Offered card must be in an Active Crucible slot or your Arcanum.");

            // Defender pays Ward count in reagents to enter the Gambit
            int wardCost = defender.StoneWardCount;
            if (wardCost > 0)
            {
                // Count all reagents the defender has
                int total = 0;
                foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
                    total += defender.GetReagent(rt);

                if (total < wardCost)
                    return CommandResult.Invalid(
                        $"Defender must pay {wardCost} Reagent(s) to enter (Ward cost); insufficient.");

                // Spend reagents in priority order: Salt → Sulphur → AquaRegia → Vitriol → Quicksilver
                int remaining = wardCost;
                ReagentType[] order = new[] { ReagentType.Salt, ReagentType.Sulphur,
                    ReagentType.AquaRegia, ReagentType.Vitriol, ReagentType.Quicksilver };
                foreach (var rt in order)
                {
                    if (remaining <= 0) break;
                    int have = defender.GetReagent(rt);
                    int spend = Math.Min(have, remaining);
                    defender.SpendReagent(rt, spend);
                    remaining -= spend;
                }
            }

            // Dice roll: attacker wins ties
            int attackRoll = _rng.Next(1, 13);
            int defendRoll = _rng.Next(1, 13);
            bool attackerWins = attackRoll >= defendRoll;
            int winnerId = attackerWins ? attackerId : defenderId;

            if (attackerWins)
            {
                // Attacker wins: defender's top Active Crucible slot (or Arcanum card) is arrested
                var defSlot = defender.CrucibleSlots.Find(s => s.State == CrucibleCardState.Active);
                if (defSlot != null) defSlot.Arrest();
            }
            else
            {
                // Defender wins: attacker's offered card is arrested
                if (offeredInCrucible)
                {
                    var aSlot = attacker.CrucibleSlots.Find(s => s.CardInstanceId == offeredCardId);
                    aSlot?.Arrest();
                }
                else
                {
                    // Adept card from Arcanum — remove and send to Arrested zone (discard for now)
                    attacker.Arcanum.Remove(offeredCardId);
                    session.Board.CommonDiscard.Add(offeredCardId);
                    session.GetCard(offeredCardId)?.MoveTo(CardZone.Discard, -1);
                }
            }

            session.EmitEvent(new GambitResolvedEvent(
                attackerId, defenderId, attackRoll, defendRoll, winnerId, offeredCardId));
            return CommandResult.Ok(
                $"Gambit: P{attackerId}({attackRoll}) vs P{defenderId}({defendRoll}) → P{winnerId} wins.");
        }

        // ── Free Arrested ─────────────────────────────────────────────────────────

        public CommandResult TryFreeArrested(GameSession session, int playerId, int slotIndex)
        {
            var player = session.Players[playerId];

            if (slotIndex < 0 || slotIndex >= player.CrucibleSlots.Count)
                return CommandResult.Invalid($"No Crucible slot at index {slotIndex}.");

            var slot = player.CrucibleSlots[slotIndex];
            if (slot.State != CrucibleCardState.Arrested)
                return CommandResult.Invalid("Slot is not Arrested.");

            if (!player.SpendReagent(ReagentType.Salt, 1))
                return CommandResult.Invalid("Freeing an Arrested card costs 1 Salt.");

            slot.Activate(); // restore to Active
            return CommandResult.Ok($"Crucible slot {slotIndex} freed from Arrest.");
        }
    }
}
