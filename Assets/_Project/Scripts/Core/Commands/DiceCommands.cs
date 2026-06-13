using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>Agekeeper rolls the Cosmic Age Die (12-sided, black). Spring Step 1.</summary>
    public sealed class RollCosmicAgeCommand : IGameCommand
    {
        public int PlayerId { get; }
        public RollCosmicAgeCommand(int playerId) => PlayerId = playerId;
    }

    /// <summary>A player rolls their personal Zodiac Die. Spring Step 2.</summary>
    public sealed class RollZodiacCommand : IGameCommand
    {
        public int PlayerId { get; }
        public RollZodiacCommand(int playerId) => PlayerId = playerId;
    }
}
