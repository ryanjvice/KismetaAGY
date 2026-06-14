using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Validates that a command is legal given the current phase and player turn.
    /// M2: lightweight checks only — season gating and active-player enforcement.
    /// The individual rule services run their own state-level validation as well.
    /// </summary>
    public sealed class ActionValidator : IActionValidator
    {
        public CommandResult Validate(GameSession session, IGameCommand command)
        {
            // Sanity: game must be running
            if (session.IsOver)
                return CommandResult.Invalid("The game is already over.");

            return command switch
            {
                RollCosmicAgeCommand cmd    => ValidateAgekeeper(session, cmd.PlayerId),
                RollZodiacCommand    cmd    => CommandResult.Ok(),
                HarvestCommand       cmd    => CommandResult.Ok(),
                CommuneCommand       cmd    => CommandResult.Ok(),
                SetCardLockCommand   _      => CommandResult.Ok(),
                ActivateCrucibleCommand cmd => ValidateSeasonSummerOrAutumn(session),
                FireStoneCommand     _      => ValidateSeason(session, Season.Autumn),
                TemperCommand        _      => ValidateSeason(session, Season.Autumn),
                InitiateOppositionCommand _ => ValidateSeason(session, Season.Autumn),
                CraftReagentCommand  _      => CommandResult.Ok(), // allowed any season
                PassActionCommand    _      => CommandResult.Ok(),
                PassCrucibleActionCommand _ => CommandResult.Ok(),
                EnforceCardLimitsCommand _  => CommandResult.Ok(),
                TransitAgeCommand    _      => CommandResult.Ok(),
                _                           => CommandResult.Ok(), // unknown → let Apply decide
            };
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

        private static CommandResult ValidateSeasonSummerOrAutumn(GameSession session)
        {
            var s = session.Phase.CurrentSeason;
            if (s != Season.Summer && s != Season.Autumn)
                return CommandResult.Invalid("Activate Crucible is only available during Summer or Autumn.");
            return CommandResult.Ok();
        }
    }
}
