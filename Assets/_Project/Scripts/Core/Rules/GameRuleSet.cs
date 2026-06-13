namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Bundles all rule service implementations for injection into GameSession.
    /// All fields are required; construct with the concrete implementations for the chosen game mode.
    /// </summary>
    public sealed class GameRuleSet
    {
        public ICardDatabase           CardDatabase  { get; }
        public ICrucibleCodexDatabase  CodexDatabase { get; }
        public GameSetupService        Setup         { get; }
        public IHarvestService         Harvest       { get; }
        public ICrucibleService        Crucible      { get; }
        public ICraftingService        Crafting      { get; }
        public WinterRules             Winter        { get; }
        public IActionValidator        Validator     { get; }
        public AstralHouseService?     AstralHouse   { get; }
        public CosmicEffectService?    CosmicEffect  { get; }
        public FateCardResolver?       FateResolver  { get; }

        public GameRuleSet(
            ICardDatabase           cardDatabase,
            ICrucibleCodexDatabase  codexDatabase,
            GameSetupService        setup,
            IHarvestService         harvest,
            ICrucibleService        crucible,
            ICraftingService        crafting,
            WinterRules             winter,
            IActionValidator        validator,
            AstralHouseService?     astralHouse  = null,
            CosmicEffectService?    cosmicEffect = null,
            FateCardResolver?       fateResolver = null)
        {
            CardDatabase  = cardDatabase;
            CodexDatabase = codexDatabase;
            Setup         = setup;
            Harvest       = harvest;
            Crucible      = crucible;
            Crafting      = crafting;
            Winter        = winter;
            Validator     = validator;
            AstralHouse   = astralHouse;
            CosmicEffect  = cosmicEffect;
            FateResolver  = fateResolver;
        }
    }
}
