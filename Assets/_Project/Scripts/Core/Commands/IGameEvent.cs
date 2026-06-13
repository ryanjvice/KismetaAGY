namespace Kismeta.Core.Commands
{
    /// <summary>
    /// Marker interface for all state-change notifications published by GameSession.
    /// Listeners (UI, AI, network) react to events without holding session references.
    /// </summary>
    public interface IGameEvent { }
}
