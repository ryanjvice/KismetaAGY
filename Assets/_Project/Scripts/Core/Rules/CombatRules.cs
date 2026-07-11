using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Summer social combat: Duel and Gambit resolution, and releasing Arrested cards.
    /// </summary>
    public sealed class CombatRules
    {
        private readonly Random _rng;

        public CombatRules(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ── Duel ──────────────────────────────────────────────────────────────────

        public CommandResult TryInitiateDuel(GameSession session, int attackerId, int defenderId,
            string targetCardId, string? anteCardId)
        {
            var validation = ValidateDuelForInitiation(session, attackerId, defenderId, targetCardId, anteCardId);
            if (!validation.IsOk) return validation;

            if (IsNoAnte(anteCardId))
                AdeptEffectService.TryMarkUsed(session, attackerId, AdeptEffectService.ChariotArcana);

            session.Players[attackerId].DuelChallengedRivalId = defenderId;

            session.Board.PendingContest = new PendingContest
            {
                Kind         = ContestKind.Duel,
                AttackerId   = attackerId,
                DefenderId   = defenderId,
                TargetCardId = targetCardId,
                AnteCardId   = anteCardId
            };

            session.EmitEvent(new DuelOfferedEvent(attackerId, defenderId, targetCardId, anteCardId));
            return CommandResult.Ok($"Duel offered to P{defenderId}.");
        }

        public CommandResult TryRespondDuel(GameSession session, int defenderId, bool accept)
        {
            var pending = session.Board.PendingContest;
            if (pending == null || pending.Kind != ContestKind.Duel)
                return CommandResult.Invalid("No pending duel to respond to.");
            if (pending.DefenderId != defenderId)
                return CommandResult.Invalid("Only the duel defender may respond.");
            if (pending.TargetCardId == null)
                return CommandResult.Invalid("Pending duel is missing card data.");

            var attackerId   = pending.AttackerId;
            var targetCardId = pending.TargetCardId;
            var anteCardId   = pending.AnteCardId;

            if (!accept)
            {
                session.Board.PendingContest = null;
                session.EmitEvent(new DuelDeclinedEvent(attackerId, defenderId));
                return CommandResult.Ok("Duel declined.");
            }

            var validation = ValidateDuelCards(session, attackerId, defenderId, targetCardId, anteCardId,
                requireUnusedChariot: false);
            if (!validation.IsOk) return validation;

            session.Board.PendingContest = null;
            return ResolveDuel(session, attackerId, defenderId, targetCardId, anteCardId);
        }

        public CommandResult TryDuel(GameSession session, int attackerId, int defenderId,
            string targetCardId, string? anteCardId)
        {
            var initiate = TryInitiateDuel(session, attackerId, defenderId, targetCardId, anteCardId);
            if (!initiate.IsOk) return initiate;
            return TryRespondDuel(session, defenderId, accept: true);
        }

        static CommandResult ValidateDuelForInitiation(GameSession session, int attackerId, int defenderId,
            string targetCardId, string? anteCardId)
        {
            var cards = ValidateDuelCards(session, attackerId, defenderId, targetCardId, anteCardId);
            if (!cards.IsOk) return cards;

            if (session.Players[attackerId].DuelChallengedRivalId >= 0)
                return CommandResult.Invalid("You have already initiated a Duel this round.");

            return CommandResult.Ok();
        }

        static CommandResult ValidateDuelCards(GameSession session, int attackerId, int defenderId,
            string targetCardId, string? anteCardId, bool requireUnusedChariot = true)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot Duel yourself.");

            if (attackerId < 0 || attackerId >= session.Players.Count)
                return CommandResult.Invalid($"Invalid attacker player ID {attackerId}.");
            if (defenderId < 0 || defenderId >= session.Players.Count)
                return CommandResult.Invalid($"Invalid defender player ID {defenderId}.");

            var attacker = session.Players[attackerId];
            var defender = session.Players[defenderId];
            var db = session.Rules?.CardDatabase;

            if (!defender.Spread.Contains(targetCardId))
                return CommandResult.Invalid("Target card must be in the defender's Spread.");

            if (AdeptEffectService.IsEmperorProtected(session, defender, targetCardId))
                return CommandResult.Invalid("Target card is protected by the Emperor.");

            if (!IsNoAnte(anteCardId))
            {
                if (!attacker.Spread.Contains(anteCardId!))
                    return CommandResult.Invalid("Ante card must be in your Spread.");
            }
            else
            {
                var chariot = ValidateChariotNoAnte(session, attacker, requireUnusedChariot);
                if (!chariot.IsOk) return chariot;
            }

            if (db != null)
            {
                var targetInst = session.GetCard(targetCardId);
                var targetDef = targetInst != null ? db.GetById(targetInst.DefinitionId) : null;
                if (targetDef?.IsMajorArcana == true)
                    return CommandResult.Invalid("Target card is Major Arcana and cannot be dueled for.");

                if (!IsNoAnte(anteCardId))
                {
                    var anteInst = session.GetCard(anteCardId!);
                    var anteDef = anteInst != null ? db.GetById(anteInst.DefinitionId) : null;
                    if (anteDef?.IsMajorArcana == true)
                        return CommandResult.Invalid("Ante card is Major Arcana and cannot be dueled with.");
                }
            }

            return CommandResult.Ok();
        }

        static CommandResult ValidateChariotNoAnte(GameSession session, PlayerState attacker, bool requireUnused)
        {
            if (requireUnused)
            {
                if (!AdeptEffectService.CanUseOncePerAge(session, attacker, AdeptEffectService.ChariotArcana))
                    return CommandResult.Invalid("Chariot no-ante duel requires an unused Chariot in your Arcanum.");
            }
            else if (!AdeptEffectService.HasAdept(session, attacker, AdeptEffectService.ChariotArcana))
            {
                return CommandResult.Invalid("Chariot no-ante duel requires the Chariot in your Arcanum.");
            }

            return CommandResult.Ok();
        }

        static bool IsNoAnte(string? anteCardId) => string.IsNullOrEmpty(anteCardId);

        CommandResult ResolveDuel(GameSession session, int attackerId, int defenderId,
            string targetCardId, string? anteCardId)
        {
            var attacker = session.Players[attackerId];
            var defender = session.Players[defenderId];

            var modifiers = ContestModifierService.Build(session, ContestKind.Duel, attackerId, defenderId);
            bool bestOfThree = ContestModifierService.ResolveBestOfThree(session, ContestKind.Duel, modifiers);
            var options = new ContestResolveOptions(
                ContestKind.Duel, bestOfThree, modifiers.Attacker, modifiers.Defender);
            var series = ContestDiceSeriesResolver.Resolve(_rng, attackerId, defenderId, options);
            bool attackerWins = series.WinnerId == attackerId;

            if (attackerWins)
            {
                defender.Spread.Remove(targetCardId);
                attacker.Spread.Add(targetCardId);
                session.GetCard(targetCardId)?.MoveTo(CardZone.Spread, attackerId);
            }
            else if (!IsNoAnte(anteCardId) && !AdeptEffectService.IsEmperorProtected(session, attacker, anteCardId!))
            {
                attacker.Spread.Remove(anteCardId!);
                session.Board.CommonDiscard.Add(anteCardId!);
                session.GetCard(anteCardId!)?.MoveTo(CardZone.Discard, -1);
            }

            int loserId = attackerWins ? defenderId : attackerId;
            AdeptEffectService.TryApplyStarPostLossDraw(session, loserId);

            string resolvedAnte = anteCardId ?? string.Empty;
            session.EmitEvent(new DuelResolvedEvent(
                attackerId, defenderId,
                series.FinalAttackRoll, series.FinalDefendRoll, series.WinnerId,
                targetCardId, resolvedAnte,
                series.Rounds, series.AttackerRoundWins, series.DefenderRoundWins));
            ExchangeEventEmitter.EmitDuel(session, attackerId, defenderId,
                series.FinalAttackRoll, series.FinalDefendRoll, series.WinnerId,
                targetCardId, anteCardId,
                series.AttackerRoundWins, series.DefenderRoundWins);
            return CommandResult.Ok(
                $"Duel: P{attackerId}({series.AttackerRoundWins}) vs P{defenderId}({series.DefenderRoundWins}) → P{series.WinnerId} wins.");
        }

        // ── Gambit ────────────────────────────────────────────────────────────────

        public CommandResult TryInitiateGambit(GameSession session, int attackerId, int defenderId,
            string offeredCardId)
        {
            var validation = ValidateGambitSetup(session, attackerId, defenderId, offeredCardId);
            if (!validation.IsOk) return validation;

            int wardCost = session.Players[defenderId].StoneWardCount;
            session.Board.PendingContest = new PendingContest
            {
                Kind          = ContestKind.Gambit,
                AttackerId    = attackerId,
                DefenderId    = defenderId,
                OfferedCardId = offeredCardId
            };

            session.EmitEvent(new GambitOfferedEvent(attackerId, defenderId, offeredCardId, wardCost));
            return CommandResult.Ok($"Gambit offered to P{defenderId}.");
        }

        public CommandResult TryRespondGambit(GameSession session, int defenderId, bool accept,
            System.Collections.Generic.IReadOnlyList<(ReagentType Type, int Count)>? reagentPayments = null)
        {
            var pending = session.Board.PendingContest;
            if (pending == null || pending.Kind != ContestKind.Gambit)
                return CommandResult.Invalid("No pending gambit to respond to.");
            if (pending.DefenderId != defenderId)
                return CommandResult.Invalid("Only the gambit defender may respond.");
            if (pending.OfferedCardId == null)
                return CommandResult.Invalid("Pending gambit is missing card data.");

            var attackerId    = pending.AttackerId;
            var offeredCardId = pending.OfferedCardId;
            session.Board.PendingContest = null;

            if (!accept)
            {
                session.EmitEvent(new GambitDeclinedEvent(attackerId, defenderId));
                return CommandResult.Ok("Gambit declined.");
            }

            var validation = ValidateGambitSetup(session, attackerId, defenderId, offeredCardId);
            if (!validation.IsOk) return validation;

            var defender = session.Players[defenderId];
            int wardCost = defender.StoneWardCount;
            if (!ReagentSpendHelper.TrySpend(defender, wardCost, reagentPayments, out var spendError))
                return CommandResult.Invalid(spendError ?? "Could not pay ward cost.");

            return ResolveGambit(session, attackerId, defenderId, offeredCardId);
        }

        public CommandResult TryGambit(GameSession session, int attackerId, int defenderId,
            string offeredCardId)
        {
            var initiate = TryInitiateGambit(session, attackerId, defenderId, offeredCardId);
            if (!initiate.IsOk) return initiate;
            return TryRespondGambit(session, defenderId, accept: true);
        }

        static CommandResult ValidateGambitSetup(GameSession session, int attackerId, int defenderId,
            string offeredCardId)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot Gambit yourself.");

            var attacker = session.Players[attackerId];
            var defender = session.Players[defenderId];

            bool offeredInCrucible = attacker.CrucibleSlots.Exists(
                s => s.CardInstanceId == offeredCardId && s.State == CrucibleCardState.Active);
            bool offeredInArcanum  = attacker.Arcanum.Contains(offeredCardId);

            if (!offeredInCrucible && !offeredInArcanum)
                return CommandResult.Invalid(
                    "Offered card must be in an Active Crucible slot or your Arcanum.");

            return CommandResult.Ok();
        }

        CommandResult ResolveGambit(GameSession session, int attackerId, int defenderId, string offeredCardId)
        {
            var attacker = session.Players[attackerId];
            var defender = session.Players[defenderId];

            bool offeredInCrucible = attacker.CrucibleSlots.Exists(
                s => s.CardInstanceId == offeredCardId && s.State == CrucibleCardState.Active);

            var modifiers = ContestModifierService.Build(session, ContestKind.Gambit, attackerId, defenderId);
            bool bestOfThree = ContestModifierService.ResolveBestOfThree(session, ContestKind.Gambit, modifiers);
            var options = new ContestResolveOptions(
                ContestKind.Gambit, bestOfThree, modifiers.Attacker, modifiers.Defender);
            var series = ContestDiceSeriesResolver.Resolve(_rng, attackerId, defenderId, options);
            bool attackerWins = series.WinnerId == attackerId;

            string? arrestedDefenderCardId = null;
            if (attackerWins)
            {
                var defSlot = defender.CrucibleSlots.Find(s => s.State == CrucibleCardState.Active);
                if (defSlot != null)
                {
                    defSlot.Arrest();
                    arrestedDefenderCardId = defSlot.CardInstanceId;
                }
            }
            else
            {
                if (offeredInCrucible)
                {
                    var aSlot = attacker.CrucibleSlots.Find(s => s.CardInstanceId == offeredCardId);
                    if (!AdeptEffectService.IsEmperorProtected(session, attacker, offeredCardId))
                        aSlot?.Arrest();
                }
                else if (!AdeptEffectService.IsEmperorProtected(session, attacker, offeredCardId))
                {
                    WardRefundHelper.RefundAdeptWards(attacker, offeredCardId);
                    attacker.Arcanum.Remove(offeredCardId);
                    session.Board.CommonDiscard.Add(offeredCardId);
                    session.GetCard(offeredCardId)?.MoveTo(CardZone.Discard, -1);
                }
            }

            int loserId = attackerWins ? defenderId : attackerId;
            AdeptEffectService.TryApplyStarPostLossDraw(session, loserId);

            session.EmitEvent(new GambitResolvedEvent(
                attackerId, defenderId,
                series.FinalAttackRoll, series.FinalDefendRoll, series.WinnerId, offeredCardId,
                series.Rounds, series.AttackerRoundWins, series.DefenderRoundWins));
            ExchangeEventEmitter.EmitGambit(session, attackerId, defenderId,
                series.FinalAttackRoll, series.FinalDefendRoll, series.WinnerId, offeredCardId,
                offeredInCrucible, arrestedDefenderCardId,
                series.AttackerRoundWins, series.DefenderRoundWins);
            return CommandResult.Ok(
                $"Gambit: P{attackerId}({series.AttackerRoundWins}) vs P{defenderId}({series.DefenderRoundWins}) → P{series.WinnerId} wins.");
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

            slot.UnArrest();
            session.GetCard(slot.CardInstanceId)?.SetArrested(false);
            return CommandResult.Ok($"Crucible slot {slotIndex} freed from Arrest.");
        }
    }
}
