using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>
    /// Initiates a Duel: attacker antes one Spread card. Dice are rolled; winner steals the ante.
    /// If attacker wins, they take the defender's ante card.
    /// If defender wins, they take the attacker's ante card.
    /// </summary>
    public sealed class InitiateDuelCommand : IGameCommand
    {
        public int AttackerId    { get; }
        public int DefenderId    { get; }
        /// <summary>Instance ID of the card the attacker antes from their Spread.</summary>
        public string AnteCardId { get; }

        public InitiateDuelCommand(int attackerId, int defenderId, string anteCardId)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
            AnteCardId = anteCardId;
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
