using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>
    /// Initiates a Duel: attacker names a card in the defender's Spread to steal and antes one
    /// Spread card. Higher dice roll wins; on loss the ante returns to the Common Deck.
    /// </summary>
    public sealed class InitiateDuelCommand : IGameCommand
    {
        public int AttackerId     { get; }
        public int DefenderId     { get; }
        /// <summary>Instance ID of the defender Spread card the attacker is targeting.</summary>
        public string TargetCardId { get; }
        /// <summary>Instance ID of the card the attacker antes from their Spread; null/empty for Chariot no-ante.</summary>
        public string? AnteCardId  { get; }
        /// <summary>Second ante when Four of Swords curse requires dual ante.</summary>
        public string? SecondAnteCardId { get; }

        public InitiateDuelCommand(int attackerId, int defenderId, string targetCardId,
            string? anteCardId = null, string? secondAnteCardId = null)
        {
            AttackerId        = attackerId;
            DefenderId        = defenderId;
            TargetCardId      = targetCardId;
            AnteCardId        = anteCardId;
            SecondAnteCardId  = secondAnteCardId;
        }
    }

    /// <summary>
    /// Initiates a Gambit: attacker offers an Active Crucible Card or Adept from Arcanum.
    /// Defender must pay their Ward count in reagents to participate.
    /// Winner keeps both cards; loser's card is arrested.
    /// </summary>
    public sealed class InitiateGambitCommand : IGameCommand
    {
        public int AttackerId        { get; }
        public int DefenderId        { get; }
        /// <summary>Instance ID of the Crucible or Adept card the attacker offers (must be in an Active Crucible slot or Arcanum).</summary>
        public string OfferedCardId  { get; }

        public InitiateGambitCommand(int attackerId, int defenderId, string offeredCardId)
        {
            AttackerId    = attackerId;
            DefenderId    = defenderId;
            OfferedCardId = offeredCardId;
        }
    }

    /// <summary>
    /// Pays 1 Salt to release an Arrested Crucible Card slot back to Active.
    /// </summary>
    public sealed class FreeArrestedCommand : IGameCommand
    {
        public int PlayerId  { get; }
        public int SlotIndex { get; }

        public FreeArrestedCommand(int playerId, int slotIndex)
        {
            PlayerId  = playerId;
            SlotIndex = slotIndex;
        }
    }
}
