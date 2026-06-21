using Kismeta.Core.Commands;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Gate that checks whether a command is currently legal given the session state,
    /// Used by command validation before applying player actions.
    /// </summary>
    public interface IActionValidator
    {
        CommandResult Validate(GameSession session, IGameCommand command);
    }
}
