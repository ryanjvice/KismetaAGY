namespace Kismeta.Core.Domain
{
    /// <summary>
    /// Current transmutation state of a Philosopher's Stone.
    /// Tempering: resting on a Mantle Ring position (even 0,2,4,6).
    /// Forging: inside the Forge (odd 1,3,5,7); vulnerable to Opposition.
    /// Stasis: sent to Stasis after a lost Opposition while Forging.
    /// Complete: reached the Altar (position 8); win condition triggered.
    /// </summary>
    public enum StoneState
    {
        Tempering = 0,
        Forging,
        Stasis,
        Complete
    }
}
