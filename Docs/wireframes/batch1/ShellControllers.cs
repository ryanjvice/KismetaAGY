using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Controller for SetupSheet.uxml (compact quick-start overlay).</summary>
    public class SetupSheetController : ScreenController
    {
        SetupConfig _cfg = SetupConfig.Default;
        public System.Action<SetupConfig> OnBegin;
        public System.Action OnClose;

        protected override void Wire()
        {
            for (int n = 2; n <= 6; n++)
            {
                int count = n;
                Btn($"players-{n}").clicked += () => { _cfg.Players = count; Refresh(); };
            }
            Btn("mode-quickplay").clicked += () => { _cfg.Mode = GameMode.Quickplay; Refresh(); };
            Btn("mode-standard").clicked  += () => { _cfg.Mode = GameMode.Standard; Refresh(); };
            Btn("mode-magnus").clicked    += () => { _cfg.Mode = GameMode.Magnus; Refresh(); };
            Btn("deck-curated").clicked   += () => { _cfg.Deck = DeckBuild.Curated; Refresh(); };
            Btn("deck-random").clicked    += () => { _cfg.Deck = DeckBuild.Random; Refresh(); };
            Btn("keeper-random").clicked  += () => { _cfg.Keeper = KeeperPick.Random; Refresh(); };
            Btn("keeper-youngest").clicked+= () => { _cfg.Keeper = KeeperPick.Youngest; Refresh(); };
            Btn("keeper-choose").clicked  += () => { _cfg.Keeper = KeeperPick.Choose; Refresh(); };
            Btn("begin-btn").clicked      += () => OnBegin?.Invoke(_cfg);
            Btn("close-btn").clicked      += () => OnClose?.Invoke();
        }

        protected override void Bind() => Refresh();

        void Refresh()
        {
            for (int n = 2; n <= 6; n++)
                Btn($"players-{n}").EnableInClassList("player-pip--active", n == _cfg.Players);
            Btn("mode-quickplay").EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Quickplay);
            Btn("mode-standard").EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Standard);
            Btn("mode-magnus").EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Magnus);
            Btn("deck-curated").EnableInClassList("player-pip--active", _cfg.Deck == DeckBuild.Curated);
            Btn("deck-random").EnableInClassList("player-pip--active", _cfg.Deck == DeckBuild.Random);
            Btn("keeper-random").EnableInClassList("player-pip--active", _cfg.Keeper == KeeperPick.Random);
            Btn("keeper-youngest").EnableInClassList("player-pip--active", _cfg.Keeper == KeeperPick.Youngest);
            Btn("keeper-choose").EnableInClassList("player-pip--active", _cfg.Keeper == KeeperPick.Choose);

            if (Lbl("summary") != null)
                Lbl("summary").text =
                    $"{_cfg.Players} alchemists · {_cfg.Mode} · {_cfg.Deck.ToString().ToLower()} deck · {_cfg.Keeper.ToString().ToLower()} agekeeper";
        }
    }

    /// <summary>Controller for JoinScreen.uxml — room-code entry.</summary>
    public class JoinController : ScreenController
    {
        public System.Action<string> OnJoin;
        public System.Action OnBack;

        protected override void Wire()
        {
            Btn("join-confirm-btn").clicked += Confirm;
            Btn("back-btn").clicked += () => OnBack?.Invoke();
        }

        void Confirm()
        {
            var field = Root.Q<TextField>("room-code");
            var code = field?.value?.Trim().ToUpper();
            if (!string.IsNullOrEmpty(code) && code.Length == 6)
                OnJoin?.Invoke(code);
        }
    }

    /// <summary>Controller for CodexScreen.uxml — searchable reference.</summary>
    public class CodexController : ScreenController
    {
        public System.Action OnBack;
        public System.Action<string> OnSearch;
        public System.Action<string> OnTabChanged;

        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();

            var search = Root.Q<TextField>("codex-search");
            if (search != null)
                search.RegisterValueChangedCallback(e => OnSearch?.Invoke(e.newValue));

            HookTab("tab-cards", "cards");
            HookTab("tab-ages", "ages");
            HookTab("tab-states", "states");
            HookTab("tab-terms", "terms");
        }

        void HookTab(string btnName, string key)
        {
            Btn(btnName).clicked += () =>
            {
                foreach (var n in new[] { "tab-cards", "tab-ages", "tab-states", "tab-terms" })
                    Btn(n).EnableInClassList("codex-tab--active", n == btnName);
                OnTabChanged?.Invoke(key);
            };
        }
    }
}
