namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Bundles all rule service implementations for injection into GameSession.
    /// All fields are required; construct with the concrete implementations for the chosen game mode.
    /// </summary>
    public sealed class GameRuleSet
    {
        public ICardDatabase    CardDatabase { get; }
        public GameSetupService Setup        { get; }
        public IHarvestService  Harvest      { get; }
        public ICrucibleService Crucible     { get; }
        public ICraftingService Crafting     { get; }
        public WinterRules      Winter       { get; }
        public IActionValidator Validator    { get; }

        public GameRuleSet(
            ICardDatabase    cardDatabase,
            GameSetupService setup,
            IHarvestService  harvest,
            ICrucibleService crucible,
            ICraftingService crafting,
            WinterRules      winter,
            IActionValidator validator)
        {
            CardDatabase = cardDatabase;
            Setup        = setup;
            Harvest      = harvest;
            Crucible     = crucible;
            Crafting     = crafting;
            Winter       = winter;
            Validator    = validator;
        }
    }
}
