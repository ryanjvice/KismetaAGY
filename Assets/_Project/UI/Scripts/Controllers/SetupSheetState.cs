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
            for (int n = 2; n <= 4; n++)
            {
                int count = n;
                _root.Q<Button>($"players-{n}")!.clicked += () => { _cfg.Players = count; Refresh(); };
            }
            _root.Q<Button>("mode-quickplay")!.clicked += () => { _cfg.Mode = GameMode.Quickplay; Refresh(); };
            _root.Q<Button>("mode-standard")!.clicked += () => { _cfg.Mode = GameMode.Standard; Refresh(); };
            _root.Q<Button>("mode-magnus")!.clicked += () => { _cfg.Mode = GameMode.MagnusAlchemist; Refresh(); };
            _root.Q<Button>("deck-curated")!.clicked += () => { _cfg.Deck = UiDeckBuild.Curated; Refresh(); };
            _root.Q<Button>("deck-random")!.clicked += () => { _cfg.Deck = UiDeckBuild.Random; Refresh(); };
        }

        void Unwire()
        {
            // Sheet is destroyed on dismiss; handlers die with the tree.
        }

        void Refresh()
        {
            if (_root == null) return;
            for (int n = 2; n <= 4; n++)
                _root.Q<Button>($"players-{n}")!.EnableInClassList("player-pip--active", n == _cfg.Players);

            _root.Q<Button>("mode-quickplay")!.EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Quickplay);
            _root.Q<Button>("mode-standard")!.EnableInClassList("mode-row--active", _cfg.Mode == GameMode.Standard);
            _root.Q<Button>("mode-magnus")!.EnableInClassList("mode-row--active", _cfg.Mode == GameMode.MagnusAlchemist);
            _root.Q<Button>("deck-curated")!.EnableInClassList("mode-row--active", _cfg.Deck == UiDeckBuild.Curated);
            _root.Q<Button>("deck-random")!.EnableInClassList("mode-row--active", _cfg.Deck == UiDeckBuild.Random);

            var summary = _root.Q<Label>("summary");
            if (summary != null)
                summary.text =
                    $"{_cfg.Players} alchemists · {UiSetupConfigMapper.ModeDisplayName(_cfg.Mode)} · {UiSetupConfigMapper.DeckDisplayName(_cfg.Deck)}";
        }
    }
}
