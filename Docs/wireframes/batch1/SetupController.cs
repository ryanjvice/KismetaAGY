using UnityEngine.UIElements;

namespace Kismeta.UI
{
    public enum GameMode { Quickplay, Standard, Magnus }
    public enum DeckBuild { Curated, Random }
    public enum KeeperPick { Random, Youngest, Choose }

    /// <summary>
    /// Holds the chosen setup options. Shared by SetupScreen and SetupSheet.
    /// </summary>
    public struct SetupConfig
    {
        public int Players;
        public GameMode Mode;
        public DeckBuild Deck;
        public KeeperPick Keeper;

        public static SetupConfig Default => new SetupConfig
        {
            Players = 3, Mode = GameMode.Standard,
            Deck = DeckBuild.Curated, Keeper = KeeperPick.Random
        };
    }

    /// <summary>
    /// Controller for SetupScreen.uxml (dedicated players + difficulty screen).
    /// Updates the live "what changes" panel and the begin button as selections change.
    /// </summary>
    public class SetupController : ScreenController
    {
        SetupConfig _cfg = SetupConfig.Default;
        public System.Action<SetupConfig> OnBegin;

        static readonly string[] PlayerNotes = {
            "", "",
            "a focused duel of two alchemists — fast, tense, fewer rivals to fear",
            "the classic balance — enough rivals for real intrigue",
            "a full table — alliances shift and the forge gets crowded",
            "a sprawling great year — long, chaotic, many stones racing",
            "the grand melee — maximum players, longest game"
        };

        protected override void Wire()
        {
            for (int n = 2; n <= 6; n++)
            {
                int count = n;
                Btn($"players-{n}").clicked += () => SetPlayers(count);
            }
            Btn("mode-quickplay").clicked += () => SetMode(GameMode.Quickplay);
            Btn("mode-standard").clicked  += () => SetMode(GameMode.Standard);
            Btn("mode-magnus").clicked    += () => SetMode(GameMode.Magnus);
            Btn("begin-btn").clicked      += () => OnBegin?.Invoke(_cfg);
        }

        protected override void Bind() => Refresh();

        void SetPlayers(int n) { _cfg.Players = n; Refresh(); }
        void SetMode(GameMode m) { _cfg.Mode = m; Refresh(); }

        void Refresh()
        {
            // player pip active states
            for (int n = 2; n <= 6; n++)
                Btn($"players-{n}").EnableInClassList("player-pip--active", n == _cfg.Players);

            // mode row active states
            Btn("mode-quickplay").EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Quickplay);
            Btn("mode-standard").EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Standard);
            Btn("mode-magnus").EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Magnus);

            if (Lbl("player-count-readout") != null)
                Lbl("player-count-readout").text = $"{_cfg.Players} players";
            if (Lbl("player-note") != null)
                Lbl("player-note").text = PlayerNotes[_cfg.Players];

            BindModePanel();

            if (Btn("begin-btn") != null)
                Btn("begin-btn").text = $"Begin · {_cfg.Players} players, {ModeName(_cfg.Mode)}";
        }

        void BindModePanel()
        {
            var title = Lbl("rules-title");
            var body = Lbl("rules-body");
            if (title == null || body == null) return;

            switch (_cfg.Mode)
            {
                case GameMode.Quickplay:
                    title.text = "QUICKPLAY CHANGES";
                    body.text = "Streamlined rules for a first game. Houses cost 1 card; light-then-craft gateway; a forgiving economy.";
                    break;
                case GameMode.Magnus:
                    title.text = "MAGNUS CHANGES";
                    body.text = "Misaligned trades cost 2:1; misalignment grants foes +1 in Opposition; a tight, unforgiving economy.";
                    break;
                default:
                    title.text = "STANDARD CHANGES";
                    body.text = "All actions, all four contests in play. Balanced costs. Trade freely — no alignment penalties.";
                    break;
            }
        }

        static string ModeName(GameMode m) => m switch
        {
            GameMode.Quickplay => "Quickplay",
            GameMode.Magnus => "Magnus Alchemist",
            _ => "Standard"
        };
    }
}
