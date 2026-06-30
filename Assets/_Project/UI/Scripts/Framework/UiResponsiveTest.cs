using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Phase 0b smoke test: Title → setup sheet overlay → card inspect modal.
    /// Attach alongside <see cref="ViewportLayout"/> in the UI test scene.
    /// </summary>
    [RequireComponent(typeof(ViewportLayout))]
    public sealed class UiResponsiveTest : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _setupSheet;
        [SerializeField] private VisualTreeAsset _cardModals;

        private ViewportLayout _layout;
        private Button _newGameBtn;
        private Button _rulesBtn;

        private void Awake() => _layout = GetComponent<ViewportLayout>();

        private void OnEnable()
        {
            var root = _layout.Root;
            if (root == null) return;
            root.schedule.Execute(WireDemo).StartingIn(1);
        }

        private void WireDemo()
        {
            var root = _layout.Root;
            if (root == null) return;

            _newGameBtn = root.Q<Button>("new-game-btn");
            if (_newGameBtn != null)
                _newGameBtn.clicked += OnNewGame;

            _rulesBtn = root.Q<Button>("rules-btn");
            if (_rulesBtn != null)
                _rulesBtn.clicked += OnInspectModal;
        }

        private void OnDisable()
        {
            if (_newGameBtn != null)
                _newGameBtn.clicked -= OnNewGame;
            if (_rulesBtn != null)
                _rulesBtn.clicked -= OnInspectModal;
        }

        private void OnNewGame()
        {
            if (_setupSheet == null) return;
            _layout.ShowBottomSheet(_setupSheet, ViewportLayout.SheetVerticalAlign.Center);
            var root = _layout.Root;
            var close = root?.Q<Button>("close-btn");
            if (close != null)
            {
                close.clicked -= Dismiss;
                close.clicked += Dismiss;
            }

            var begin = root?.Q<Button>("begin-btn");
            if (begin != null)
            {
                begin.clicked -= Dismiss;
                begin.clicked += Dismiss;
            }
        }

        private void OnInspectModal()
        {
            if (_cardModals == null) return;
            _layout.ShowModal(_cardModals);
            var close = _layout.Root?.Q<Button>("inspect-close");
            if (close != null)
            {
                close.clicked -= Dismiss;
                close.clicked += Dismiss;
            }
        }

        private void Dismiss() => _layout.DismissOverlay();
    }
}
