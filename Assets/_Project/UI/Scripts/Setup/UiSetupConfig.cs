using Kismeta.Core.Domain;

namespace Kismeta.UI.Setup
{
    public enum UiDeckBuild
    {
        Curated,
        Random
    }

    /// <summary>
    /// UI-side setup choices from the wireframe setup screens.
    /// Uses <see cref="GameMode"/> from Core to avoid a duplicate enum.
    /// </summary>
    public struct UiSetupConfig
    {
        public int Players;
        public GameMode Mode;
        public UiDeckBuild Deck;
        /// <summary>Set by the agekeeper contest ceremony before the session starts.</summary>
        public int FirstAgekeeperPlayerId;

        public static UiSetupConfig Default => new UiSetupConfig
        {
            Players = 3,
            Mode = GameMode.Standard,
            Deck = UiDeckBuild.Curated,
            FirstAgekeeperPlayerId = -1
        };
    }

    /// <summary>
    /// Maps wireframe setup values to core session configuration.
    /// Wireframe controllers historically used "Magnus"; core uses MagnusAlchemist.
    /// </summary>
    public static class UiSetupConfigMapper
    {
        public static GameMode NormalizeMode(GameMode mode) => mode;

        public static CrucibleBuildMode ToCrucibleBuild(UiDeckBuild deck) =>
            deck == UiDeckBuild.Random ? CrucibleBuildMode.LetTheFatesDecide : CrucibleBuildMode.Curated;

        public static (int playerCount, GameMode mode, CrucibleBuildMode crucibleBuild) ToSessionConfig(
            UiSetupConfig config)
        {
            var count = System.Math.Clamp(config.Players, 2, 4);
            return (count, NormalizeMode(config.Mode), ToCrucibleBuild(config.Deck));
        }

        public static string ModeDisplayName(GameMode mode) => mode switch
        {
            GameMode.Quickplay => "Quickplay",
            GameMode.MagnusAlchemist => "Magnus Alchemist",
            _ => "Standard"
        };

        public static string DeckDisplayName(UiDeckBuild deck) => deck switch
        {
            UiDeckBuild.Random => "let the fates decide",
            _ => "curated deck"
        };
    }
}
