namespace Kismeta.Core.Commands
{
    /// <summary>Triggers full game setup: build decks, deal crucibles, determine Agekeeper.</summary>
    public sealed class SetupGameCommand : IGameCommand { }

    /// <summary>Trims Spread to 5 cards and Hand to 5 cards. Excess goes to discard. Winter Step 3.</summary>
    public sealed class EnforceCardLimitsCommand : IGameCommand { }

    /// <summary>Shuffles the common discard back into the deck, increments round, rotates Agekeeper. Winter Step 4.</summary>
    public sealed class TransitAgeCommand : IGameCommand { }
}
