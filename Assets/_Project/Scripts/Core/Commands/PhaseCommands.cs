namespace Kismeta.Core.Commands
{
    /// <summary>Advance the phase controller to the next step or season.</summary>
    public sealed class AdvancePhaseCommand : IGameCommand { }

    /// <summary>Engage or disengage the Card Lock between Hand and Spread.</summary>
    public sealed class SetCardLockCommand : IGameCommand
    {
        public bool Lock { get; }
        public SetCardLockCommand(bool lockCards) => Lock = lockCards;
    }
}
