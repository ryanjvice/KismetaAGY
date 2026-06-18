using Kismeta.Core.Domain;

namespace Kismeta.UI.Setup
{
    public enum UiDeckBuild
    {
        Curated,
        Random
    }

    public enum UiKeeperPick
    {
        Random,
        Youngest,
        Choose
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
        public UiKeeperPick Keeper;

        public static UiSetupConfig Default => new UiSetupConfig
        {
            Players = 3,
            Mode = GameMode.Standard,
            Deck = UiDeckBuild.Curated,
            Keeper = UiKeeperPick.Random
        };
    }

    /// <summary>
    /// Maps wireframe setup values to core session configuration.
    /// Wireframe controllers historically used "Magnus"; core uses MagnusAlchemist.
    /// </summary>
    public static class UiSetupConfigMapper
    {
        public static GameMode NormalizeMode(GameMode mode) => mode;

        public static (int playerCount, GameMode mode) ToSessionConfig(UiSetupConfig config)
        {
            var count = System.Math.Clamp(config.Players, 2, 6);
            return (count, NormalizeMode(config.Mode));
        }
    }
}
