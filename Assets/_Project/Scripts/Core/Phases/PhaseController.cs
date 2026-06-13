using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Phases
{
    /// <summary>
    /// Tracks current season and step. Validates legal transitions and exposes
    /// the step list for the current season. Rule resolution is delegated to
    /// IRuleService implementations — this class only manages flow.
    /// </summary>
    public sealed class PhaseController
    {
        public Season CurrentSeason { get; private set; } = Season.Spring;
        public int CurrentStepIndex { get; private set; } = 0;

        public IReadOnlyList<PhaseStepDefinition> CurrentSteps =>
            PhaseDefinitions.StepsFor(CurrentSeason);

        public PhaseStepDefinition CurrentStep =>
            CurrentSteps[CurrentStepIndex];

        public bool IsFirstStepOfSeason => CurrentStepIndex == 0;
        public bool IsLastStepOfSeason  => CurrentStepIndex == CurrentSteps.Count - 1;

        /// <summary>
        /// Advance to the next step. If the last step of a season is complete,
        /// transitions to the first step of the next season (Winter → Spring increments the round).
        /// Returns the season after advancing (may be the same season at a new step).
        /// </summary>
        public Season Advance()
        {
            if (CurrentStepIndex < CurrentSteps.Count - 1)
            {
                CurrentStepIndex++;
            }
            else
            {
                CurrentSeason = NextSeason(CurrentSeason);
                CurrentStepIndex = 0;
            }
            return CurrentSeason;
        }

        /// <summary>Jump directly to a season's first step. Used for game setup or skip-ahead.</summary>
        public void SetSeason(Season season)
        {
            CurrentSeason = season;
            CurrentStepIndex = 0;
        }

        public static Season NextSeason(Season season) => season switch
        {
            Season.Spring => Season.Summer,
            Season.Summer => Season.Autumn,
            Season.Autumn => Season.Winter,
            Season.Winter => Season.Spring,
            _             => Season.Spring
        };
    }
}
