using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Maps screen ids to UXML assets and controllers; loads screens into
    /// <see cref="ViewportLayout"/>'s content layer and manages overlay sheets.
    /// </summary>
    [RequireComponent(typeof(ViewportLayout))]
    public sealed class ScreenRouter : MonoBehaviour
    {
        [Serializable]
        public struct ScreenAsset
        {
            public string Id;
            public VisualTreeAsset Uxml;
        }

        [SerializeField] private ScreenAsset[] _screens;
        [SerializeField] private VisualTreeAsset _setupSheet;

        private ViewportLayout _layout;
        private readonly Dictionary<string, VisualTreeAsset> _assets = new();
        private readonly Dictionary<string, ScreenController> _controllers = new();
        private ScreenController? _active;
        private string? _currentId;

        public string? CurrentScreenId => _currentId;
        public ScreenController? ActiveController => _active;
        public event Action<string>? ScreenChanged;

        public void RefreshControllers()
        {
            _controllers.Clear();
            foreach (var controller in GetComponents<ScreenController>())
                _controllers[controller.ScreenId] = controller;
        }

        private void Awake()
        {
            _layout = GetComponent<ViewportLayout>();
            RebuildRegistry();
            RefreshControllers();
        }

        /// <summary>Runtime wiring from <see cref="GameBootstrap"/> when assets are not set in the inspector.</summary>
        public void ConfigureScreens(
            VisualTreeAsset title,
            VisualTreeAsset? gameplayHud,
            VisualTreeAsset waitingHud,
            VisualTreeAsset setupSheet,
            VisualTreeAsset? join = null,
            VisualTreeAsset? resume = null,
            VisualTreeAsset? codex = null,
            VisualTreeAsset? agekeeperContest = null,
            VisualTreeAsset? springHub = null,
            VisualTreeAsset? summerMain = null,
            VisualTreeAsset? autumnMain = null,
            VisualTreeAsset? winterHub = null,
            VisualTreeAsset? roundOpen = null,
            VisualTreeAsset? springIntro = null,
            VisualTreeAsset? summerIntro = null,
            VisualTreeAsset? autumnIntro = null,
            VisualTreeAsset? winterIntro = null,
            VisualTreeAsset? ageClosing = null,
            VisualTreeAsset? commune = null,
            VisualTreeAsset? springHarvest = null,
            VisualTreeAsset? winterUnlock = null,
            VisualTreeAsset? fatefulWager = null,
            VisualTreeAsset? craftReagent = null,
            VisualTreeAsset? cardLimits = null,
            VisualTreeAsset? victory = null,
            VisualTreeAsset? chronicle = null)
        {
            var screens = new List<ScreenAsset>
            {
                new() { Id = ScreenIds.Title, Uxml = title },
                new() { Id = ScreenIds.Waiting, Uxml = waitingHud },
            };
            if (gameplayHud != null)
                screens.Insert(1, new ScreenAsset { Id = ScreenIds.GameplayHud, Uxml = gameplayHud });
            if (join != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.Join, Uxml = join });
            if (resume != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.Resume, Uxml = resume });
            if (codex != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.Codex, Uxml = codex });
            if (agekeeperContest != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.AgekeeperContest, Uxml = agekeeperContest });
            if (springHub != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.SpringHub, Uxml = springHub });
            if (summerMain != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.SummerMain, Uxml = summerMain });
            if (autumnMain != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.AutumnMain, Uxml = autumnMain });
            if (winterHub != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.WinterHub, Uxml = winterHub });
            if (roundOpen != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.RoundOpen, Uxml = roundOpen });
            if (springIntro != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.SpringIntro, Uxml = springIntro });
            if (summerIntro != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.SummerIntro, Uxml = summerIntro });
            if (autumnIntro != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.AutumnIntro, Uxml = autumnIntro });
            if (winterIntro != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.WinterIntro, Uxml = winterIntro });
            if (ageClosing != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.AgeClosing, Uxml = ageClosing });
            if (commune != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.Commune, Uxml = commune });
            if (springHarvest != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.SpringHarvest, Uxml = springHarvest });
            if (winterUnlock != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.WinterUnlock, Uxml = winterUnlock });
            if (fatefulWager != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.FatefulWager, Uxml = fatefulWager });
            if (craftReagent != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.CraftReagent, Uxml = craftReagent });
            if (cardLimits != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.CardLimits, Uxml = cardLimits });
            if (victory != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.Victory, Uxml = victory });
            if (chronicle != null)
                screens.Add(new ScreenAsset { Id = ScreenIds.Chronicle, Uxml = chronicle });

            _screens = screens.ToArray();
            _setupSheet = setupSheet;
            RebuildRegistry();
            RefreshControllers();
        }

        private void RebuildRegistry()
        {
            _assets.Clear();
            if (_screens == null) return;
            foreach (var entry in _screens)
            {
                if (string.IsNullOrEmpty(entry.Id) || entry.Uxml == null)
                    continue;
                _assets[entry.Id] = entry.Uxml;
            }
        }

        public bool GoTo(string screenId)
        {
            if (!_assets.TryGetValue(screenId, out var uxml))
            {
                Debug.LogWarning($"[ScreenRouter] Unknown screen '{screenId}'.");
                return false;
            }

            _layout.SetScreen(uxml);
            _active?.Detach();

            _controllers.TryGetValue(screenId, out var controller);
            _active = controller;

            var screenRoot = _layout.ContentScreenRoot;
            if (_active != null && screenRoot != null)
                _active.AttachTo(screenRoot);

            _currentId = screenId;
            ScreenChanged?.Invoke(screenId);
            return true;
        }

        public void ShowSetupSheet(Action<VisualElement>? onOpened = null, Action? onClosed = null, Action? onBegin = null)
        {
            if (_setupSheet == null)
            {
                Debug.LogWarning("[ScreenRouter] Setup sheet UXML not assigned.");
                return;
            }

            _layout.ShowBottomSheet(_setupSheet);

            var root = _layout.Root;
            var sheetRoot = root?.Q<VisualElement>("setup-sheet");
            if (sheetRoot != null)
                onOpened?.Invoke(sheetRoot);
            var close = root?.Q<Button>("close-btn");
            var begin = root?.Q<Button>("begin-btn");

            if (close != null)
            {
                close.clicked -= HandleClose;
                close.clicked += HandleClose;
            }

            if (begin != null && onBegin != null)
            {
                begin.clicked -= HandleBegin;
                begin.clicked += HandleBegin;
            }

            void HandleClose()
            {
                Cleanup();
                onClosed?.Invoke();
            }

            void HandleBegin()
            {
                onBegin?.Invoke();
                Cleanup();
            }

            void Cleanup()
            {
                _layout.DismissOverlay();
                if (close != null) close.clicked -= HandleClose;
                if (begin != null) begin.clicked -= HandleBegin;
            }
        }

        public void DismissOverlay() => _layout.DismissOverlay();

        public T? GetController<T>(string screenId) where T : ScreenController =>
            _controllers.TryGetValue(screenId, out var c) ? c as T : null;
    }
}
