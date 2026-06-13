namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Bundles all rule service implementations for injection into GameSession.
    /// Pass null to run in stub/test mode (commands return NotImplemented).
    /// </summary>
    public sealed class GameRuleSet
    {
        public ICardDatabase CardDatabase { get; }
        public IHarvestService Harvest { get; }
        public ICrucibleService Crucible { get; }
        public IActionValidator Validator { get; }

        public GameRuleSet(
            ICardDatabase cardDatabase,
            IHarvestService harvest,
            ICrucibleService crucible,
            IActionValidator validator)
        {
            CardDatabase = cardDatabase;
            Harvest = harvest;
            Crucible = crucible;
            Validator = validator;
        }
    }
}
