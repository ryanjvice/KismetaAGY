using UnityEngine.UIElements;
using Kismeta.Core.Domain;
using Kismeta.UI.Setup;

namespace Kismeta.UI.Controllers
{
    /// <summary>
    /// Tracks setup sheet selections. Wire when the bottom sheet opens.
    /// </summary>
    public sealed class SetupSheetState
    {
        UiSetupConfig _cfg = UiSetupConfig.Default;
        VisualElement? _root;

        public UiSetupConfig Current => _cfg;

        public void Attach(VisualElement sheetRoot)
        {
            Detach();
            _root = sheetRoot;
            Wire();
            Refresh();
        }

        public void Detach()
        {
            if (_root == null) return;
            Unwire();
            _root = null;
        }

        void Wire()
        {
            if (_root == null) return;
            for (int n = 2; n <= 6; n++)
            {
                int count = n;
                _root.Q<Button>($"players-{n}")!.clicked += () => { _cfg.Players = count; Refresh(); };
            }
            _root.Q<Button>("mode-quickplay")!.clicked += () => { _cfg.Mode = GameMode.Quickplay; Refresh(); };
            _root.Q<Button>("mode-standard")!.clicked += () => { _cfg.Mode = GameMode.Standard; Refresh(); };
            _root.Q<Button>("mode-magnus")!.clicked += () => { _cfg.Mode = GameMode.MagnusAlchemist; Refresh(); };
            _root.Q<Button>("deck-curated")!.clicked += () => { _cfg.Deck = UiDeckBuild.Curated; Refresh(); };
            _root.Q<Button>("deck-random")!.clicked += () => { _cfg.Deck = UiDeckBuild.Random; Refresh(); };
            _root.Q<Button>("keeper-random")!.clicked += () => { _cfg.Keeper = UiKeeperPick.Random; Refresh(); };
            _root.Q<Button>("keeper-youngest")!.clicked += () => { _cfg.Keeper = UiKeeperPick.Youngest; Refresh(); };
            _root.Q<Button>("keeper-choose")!.clicked += () => { _cfg.Keeper = UiKeeperPick.Choose; Refresh(); };
        }

        void Unwire()
        {
            // Sheet is destroyed on dismiss; handlers die with the tree.
        }

        void Refresh()
        {
            if (_root == null) return;
            for (int n = 2; n <= 6; n++)
                _root.Q<Button>($"players-{n}")!.EnableInClassList("player-pip--active", n == _cfg.Players);

            _root.Q<Button>("mode-quickplay")!.EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Quickplay);
            _root.Q<Button>("mode-standard")!.EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Standard);
            _root.Q<Button>("mode-magnus")!.EnableInClassList("mode-row--active", _cfg.Mode == GameMode.MagnusAlchemist);
            _root.Q<Button>("deck-curated")!.EnableInClassList("player-pip--active", _cfg.Deck == UiDeckBuild.Curated);
            _root.Q<Button>("deck-random")!.EnableInClassList("player-pip--active", _cfg.Deck == UiDeckBuild.Random);
            _root.Q<Button>("keeper-random")!.EnableInClassList("player-pip--active", _cfg.Keeper == UiKeeperPick.Random);
            _root.Q<Button>("keeper-youngest")!.EnableInClassList("player-pip--active", _cfg.Keeper == UiKeeperPick.Youngest);
            _root.Q<Button>("keeper-choose")!.EnableInClassList("player-pip--active", _cfg.Keeper == UiKeeperPick.Choose);

            var summary = _root.Q<Label>("summary");
            if (summary != null)
                summary.text =
                    $"{_cfg.Players} alchemists · {_cfg.Mode} · {_cfg.Deck.ToString().ToLower()} deck · {_cfg.Keeper.ToString().ToLower()} agekeeper";
        }
    }
}
