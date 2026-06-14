namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Base marker interface for all rules subsystems. Each service receives the game
    /// session for context and returns a CommandResult indicating whether the action
    /// was legal.
    /// </summary>
    public interface IRuleService { }
}
