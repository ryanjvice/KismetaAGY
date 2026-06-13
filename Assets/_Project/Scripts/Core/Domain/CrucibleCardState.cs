namespace Kismeta.Core.Domain
{
    /// <summary>
    /// Lifecycle state of a player's Crucible Card.
    /// Dormant → Active (Codex activation formula satisfied) → Fired (Alchemical Formula satisfied, Stone in Forge).
    /// Arrested: card seized after a failed Gambit.
    /// Discarded: consumed on a successful Temper; removed from play.
    /// </summary>
    public enum CrucibleCardState
    {
        Dormant = 0,
        Active,
        Fired,
        Arrested,
        Discarded
    }
}
