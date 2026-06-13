namespace Kismeta.Core.Commands
{
    /// <summary>
    /// Marker interface for all player actions. Commands are the only way external
    /// code mutates GameSession state, enabling future command logging and networking.
    /// </summary>
    public interface IGameCommand { }
}
