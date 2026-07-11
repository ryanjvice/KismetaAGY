using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Validates that a command is legal given the current phase, turn, and pending contests.
    /// </summary>
    public sealed class ActionValidator : IActionValidator
    {
        public CommandResult Validate(GameSession session, IGameCommand command)
        {
            if (session.IsOver)
                return CommandResult.Invalid("The game is already over.");

            return command switch
            {
                RollCosmicAgeCommand cmd    => ValidateAgekeeper(session, cmd.PlayerId),
                RollZodiacCommand    _      => CommandResult.Ok(),
                HarvestCommand       _      => CommandResult.Ok(),
                BeginHarvestCommand  _      => CommandResult.Ok(),
                HarvestDealStepCommand _  => CommandResult.Ok(),
                CommuneCommand       _      => CommandResult.Ok(),
                SetCardLockCommand   _      => CommandResult.Ok(),
                ActivateCrucibleCommand _   => ValidateSeason(session, Season.Autumn),
                FireStoneCommand     _      => ValidateSeason(session, Season.Autumn),
                TemperCommand        _      => ValidateSeason(session, Season.Autumn),
                InitiateOppositionCommand cmd => ValidateContestInitiation(session, cmd.AttackerId, Season.Autumn),
                InitiateDuelCommand cmd     => ValidateContestInitiation(session, cmd.AttackerId, Season.Summer),
                InitiateGambitCommand cmd   => ValidateContestInitiation(session, cmd.AttackerId, Season.Summer),
                DirectTradeCommand cmd      => ValidateContestInitiation(session, cmd.PlayerId, Season.Summer),
                RespondTradeCommand cmd     => ValidateContestResponse(session, cmd.PlayerId),
                RespondDuelCommand cmd      => ValidateContestResponse(session, cmd.PlayerId),
                RespondGambitCommand cmd    => ValidateContestResponse(session, cmd.PlayerId),
                RespondOppositionCommand cmd => ValidateContestResponse(session, cmd.PlayerId),
                BuildAstralHouseCommand _   => ValidateSeason(session, Season.Summer),
                CraftReagentCommand  _      => CommandResult.Ok(),
                CraftKingReagentCommand _  => CommandResult.Ok(),
                MarkEmpressReagentCommand _ => CommandResult.Ok(),
                MarkTemperanceWildReagentCommand _ => CommandResult.Ok(),
                MagicianSwapCommand _ => CommandResult.Ok(),
                ProtectSpreadCardsCommand cmd => ValidatePlayerTurnInSeason(session, cmd.PlayerId, Season.Summer),
                ShiftZodiacCommand _ => ValidateSeason(session, Season.Spring),
                ShiftOppositionZodiacCommand cmd => ValidatePlayerTurnInSeason(session, cmd.PlayerId, Season.Autumn),
                DevilStealCommand cmd => ValidatePlayerTurnInSeason(session, cmd.PlayerId, Season.Summer),
                CompletePriestessHarvestCommand _ => ValidateSeason(session, Season.Spring),
                PassActionCommand    _      => CommandResult.Ok(),
                PassCrucibleActionCommand _ => CommandResult.Ok(),
                EnforceCardLimitsCommand _  => CommandResult.Ok(),
                TransitAgeCommand    _      => CommandResult.Ok(),
                PlaceCardWardCommand cmd   => ValidatePlayerTurnInSeason(session, cmd.PlayerId, Season.Summer),
                PlaceAdeptWardCommand cmd  => ValidatePlayerTurnInSeason(session, cmd.PlayerId, Season.Summer),
                PlaceStoneWardCommand cmd  => ValidatePlayerTurnInSeason(session, cmd.PlayerId, Season.Autumn),
                _                           => CommandResult.Ok(),
            };
        }

        static CommandResult ValidateContestInitiation(GameSession session, int playerId, Season required)
        {
            var seasonCheck = ValidateSeason(session, required);
            if (!seasonCheck.IsOk) return seasonCheck;

            if (session.Board.PendingContest != null)
                return CommandResult.Invalid("Resolve the pending contest before initiating another.");

            if (session.CurrentTurnPlayerId.HasValue && session.CurrentTurnPlayerId.Value != playerId)
                return CommandResult.Invalid("It is not your turn.");

            return CommandResult.Ok();
        }

        static CommandResult ValidateContestResponse(GameSession session, int playerId)
        {
            var pending = session.Board.PendingContest;
            if (pending == null)
                return CommandResult.Invalid("No pending contest to respond to.");
            if (pending.DefenderId != playerId)
                return CommandResult.Invalid("Only the targeted player may respond.");
            return CommandResult.Ok();
        }

        private static CommandResult ValidateAgekeeper(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            if (!player.IsAgekeeper)
                return CommandResult.Invalid("Only the Agekeeper may roll the Cosmic Age Die.");
            return CommandResult.Ok();
        }

        private static CommandResult ValidateSeason(GameSession session, Season required)
        {
            if (session.Phase.CurrentSeason != required)
                return CommandResult.Invalid($"Action is only available during {required}.");
            return CommandResult.Ok();
        }

        private static CommandResult ValidatePlayerTurnInSeason(GameSession session, int playerId, Season required)
        {
            var seasonCheck = ValidateSeason(session, required);
            if (!seasonCheck.IsOk) return seasonCheck;

            if (session.CurrentTurnPlayerId.HasValue && session.CurrentTurnPlayerId.Value != playerId)
                return CommandResult.Invalid("It is not your turn.");

            return CommandResult.Ok();
        }
    }
}
