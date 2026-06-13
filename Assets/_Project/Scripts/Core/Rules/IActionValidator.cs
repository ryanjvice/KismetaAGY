using Kismeta.Core.Commands;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Gate that checks whether a command is currently legal given the session state,
    /// without executing it. Used by PlayerDirector before applying commands.
    /// </summary>
    public interface IActionValidator
    {
        CommandResult Validate(GameSession session, IGameCommand command);
    }
}
