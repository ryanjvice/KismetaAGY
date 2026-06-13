using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Implements the full Crucible Card lifecycle for M2:
    ///   Activate (Summer)  — discard 3 cards, make slot Active, light Cauldron.
    ///   Fire    (Autumn)   — pay Alchemical Cost in reagents; stone moves to Forging.
    ///   Temper  (Autumn)   — advance stone one step; check for Altar win.
    ///   LeaveStasis        — spend 2 Salt to exit Stasis.
    ///   Opposition (Autumn)— dice-only: higher roll wins; loser's stone → Stasis.
    ///
    /// M2 simplification: Activate requires any 3 cards (formula type not validated).
    /// </summary>
    public sealed class CrucibleRules : ICrucibleService
    {
        private const int ActivationCardCost = 3;
        private const int StasisSaltCost     = 2;

        private readonly ICardDatabase _db;
        private readonly Random _rng;

        public CrucibleRules(ICardDatabase db, int? seed = null)
        {
            _db  = db;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ─── Activate ─────────────────────────────────────────────────────────────

        public CommandResult TryActivate(GameSession session, int playerId, int slotIndex,
            IReadOnlyList<string> cardInstanceIds)
        {
            var player = session.Players[playerId];

            if (slotIndex < 0 || slotIndex >= player.CrucibleSlots.Count)
                return CommandResult.Invalid($"No Crucible slot at index {slotIndex}.");

            var slot = player.CrucibleSlots[slotIndex];
            if (slot.State != CrucibleCardState.Dormant)
                return CommandResult.Invalid("Slot must be Dormant to Activate.");

            if (cardInstanceIds.Count < ActivationCardCost)
                return CommandResult.Invalid($"Activation requires at least {ActivationCardCost} cards.");

            // Validate all supplied cards belong to this player
            var playerCards = BuildPlayerCardSet(player);
            foreach (var id in cardInstanceIds)
                if (!playerCards.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in player {playerId}'s zones.");

            // Discard them
            foreach (var id in cardInstanceIds)
            {
                player.Spread.Remove(id);
                player.Hand.Remove(id);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }

            // Activate slot + light the matching Cauldron
            slot.Activate();

            var crucibleInst = session.GetCard(slot.CardInstanceId);
            if (crucibleInst != null)
            {
                var def = _db.GetById(crucibleInst.DefinitionId);
                if (def != null && def.Suit != Suit.None)
                    player.LightCauldron(def.Suit);
            }

            session.EmitEvent(new CrucibleActivatedEvent(playerId, slotIndex, slot.CardInstanceId));
            return CommandResult.Ok();
        }

        // ─── Fire ─────────────────────────────────────────────────────────────────

        public CommandResult TryFire(GameSession session, int playerId, int slotIndex)
        {
            var player = session.Players[playerId];

            if (slotIndex < 0 || slotIndex >= player.CrucibleSlots.Count)
                return CommandResult.Invalid($"No Crucible slot at index {slotIndex}.");

            var slot = player.CrucibleSlots[slotIndex];
            if (slot.State != CrucibleCardState.Active)
                return CommandResult.Invalid("Slot must be Active to Fire.");

            if (player.StoneState == StoneState.Stasis)
                return CommandResult.Invalid("Stone is in Stasis; leave Stasis before Firing.");

            // Pay alchemical cost from the Crucible card definition
            var crucibleInst = session.GetCard(slot.CardInstanceId);
            if (crucibleInst != null)
            {
                var def = _db.GetById(crucibleInst.DefinitionId);
                if (def != null && !CanPayCost(player, def.AlchemicalCost))
                    return CommandResult.Invalid("Insufficient reagents to Fire.");
                if (def != null)
                    PayCost(player, def.AlchemicalCost);
            }

            // Stone enters Forging state
            player.StoneState = StoneState.Forging;
            slot.Fire(session.Board.RoundNumber);

            session.EmitEvent(new StoneFiredEvent(playerId, slotIndex, player.StonePosition));
            return CommandResult.Ok();
        }

        // ─── Temper ───────────────────────────────────────────────────────────────

        public CommandResult TryTemper(GameSession session, int playerId)
        {
            var player = session.Players[playerId];

            if (player.StoneState != StoneState.Forging)
                return CommandResult.Invalid("Stone must be Forging to Temper.");

            // The Fired slot that was fired in a prior round is eligible
            PlayerCrucibleSlot? eligibleSlot = null;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var s = player.CrucibleSlots[i];
                if (s.State == CrucibleCardState.Fired &&
                    s.FiredAtRound >= 0 &&
                    s.FiredAtRound < session.Board.RoundNumber)
                {
                    eligibleSlot = s;
                    break;
                }
            }

            if (eligibleSlot == null)
                return CommandResult.Invalid("No Fired slot eligible for Temper (must have been Fired a previous round).");

            // Advance stone one step
            var newPos = player.StonePosition.Advance();
            player.StonePosition = newPos;
            player.StoneState    = StoneState.Tempering;

            // Discard the Fired crucible card
            eligibleSlot.Discard();
            session.Board.CrucibleDiscard.Add(eligibleSlot.CardInstanceId);
            session.GetCard(eligibleSlot.CardInstanceId)?.MoveTo(CardZone.Discard, -1);

            session.EmitEvent(new StoneTemperedEvent(playerId, newPos));

            // Win: stone reaches the Altar
            if (newPos.IsAltar)
                session.SetWinner(playerId);

            return CommandResult.Ok();
        }

        // ─── Leave Stasis ─────────────────────────────────────────────────────────

        public CommandResult TryLeaveStasis(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            if (player.StoneState != StoneState.Stasis)
                return CommandResult.Invalid("Stone is not in Stasis.");

            if (!player.SpendReagent(ReagentType.Salt, StasisSaltCost))
                return CommandResult.Invalid($"Leaving Stasis requires {StasisSaltCost} Salt.");

            player.StoneState = StoneState.Tempering;
            session.Board.StasisOccupancy[playerId] = false;
            return CommandResult.Ok("Left Stasis.");
        }

        // ─── Opposition ───────────────────────────────────────────────────────────

        public CommandResult TryOppose(GameSession session, int attackerId, int defenderId)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot oppose yourself.");

            var defender = session.Players[defenderId];
            if (defender.StoneState != StoneState.Forging)
                return CommandResult.Invalid("Target stone must be Forging to initiate Opposition.");

            // M2 dice-only: each side rolls 1–12; attacker wins ties
            int attackRoll = _rng.Next(1, 13);
            int defendRoll = _rng.Next(1, 13);
            int loserId    = attackRoll >= defendRoll ? defenderId : attackerId;

            session.Players[loserId].StoneState = StoneState.Stasis;
            session.Board.StasisOccupancy[loserId] = true;

            session.EmitEvent(new OppositionResolvedEvent(attackerId, defenderId, attackRoll, defendRoll, loserId));
            return CommandResult.Ok($"Opposition resolved. Player {loserId} enters Stasis.");
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private static HashSet<string> BuildPlayerCardSet(PlayerState player)
        {
            var set = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) set.Add(id);
            foreach (var id in player.Hand)   set.Add(id);
            return set;
        }

        private static bool CanPayCost(PlayerState player, ReagentCost cost) =>
            player.GetReagent(ReagentType.Sulphur)     >= cost.Sulphur     &&
            player.GetReagent(ReagentType.AquaRegia)   >= cost.AquaRegia   &&
            player.GetReagent(ReagentType.Vitriol)     >= cost.Vitriol     &&
            player.GetReagent(ReagentType.Quicksilver) >= cost.Quicksilver &&
            player.GetReagent(ReagentType.Salt)        >= cost.Salt;

        private static void PayCost(PlayerState player, ReagentCost cost)
        {
            player.SpendReagent(ReagentType.Sulphur,     cost.Sulphur);
            player.SpendReagent(ReagentType.AquaRegia,   cost.AquaRegia);
            player.SpendReagent(ReagentType.Vitriol,     cost.Vitriol);
            player.SpendReagent(ReagentType.Quicksilver, cost.Quicksilver);
            player.SpendReagent(ReagentType.Salt,        cost.Salt);
        }
    }
}
