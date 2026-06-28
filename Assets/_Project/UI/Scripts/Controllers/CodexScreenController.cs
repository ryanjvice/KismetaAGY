using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public static class CodexTabs
    {
        public const string Cards = "cards";
        public const string Ages = "ages";
        public const string States = "states";
        public const string Terms = "terms";
    }

    public sealed class CodexScreenController : ScreenController
    {
        public override string ScreenId => ScreenIds.Codex;

        public Action? OnBack;
        public Action<string>? OnSearch;
        public Action<string>? OnTabChanged;

        string _activeTab = CodexTabs.Cards;
        string _searchQuery = "";

        static readonly Dictionary<string, (string title, string body)[]> TabEntries = new()
        {
            [CodexTabs.Cards] = new (string title, string body)[]
            {
                ("Minor Arcana", "Pip cards — rank + suit drive crafting, sets and alignment."),
                ("Adept (Major Arcana)", "Active ability placed in your Arcanum; up to 2 active."),
                ("Fate (Major Arcana)", "Resolves the instant it's drawn; stays face-up after."),
                ("Crucible Cards", "Four formulas in your codex; activate by matching spread sets."),
            },
            [CodexTabs.Ages] = new (string title, string body)[]
            {
                ("Cosmic Age", "The sign rolled each Spring; drives alignment bonuses and wagers."),
                ("Transit the Age", "Key passes between rounds; lit cauldrons and houses persist."),
                ("Round", "Spring → Summer → Autumn → Winter, then the next cosmic age."),
            },
            [CodexTabs.States] = new (string title, string body)[]
            {
                ("Spread", "Face-up public cards in front of you."),
                ("Hand", "Private cards — rivals see counts only."),
                ("Arcanum", "Active Adept slots (up to 2)."),
                ("Stasis", "Stone stalled on the forge until Salt or Opposition."),
            },
            [CodexTabs.Terms] = new (string title, string body)[]
            {
                ("Agekeeper", "Rotating first player; breaks ties and receives setup boons."),
                ("Magnus Alchemist", "Hard mode — misaligned trades cost 2:1; tighter economy."),
                ("Opposition", "Contest to swap forge positions with a rival."),
                ("Fateful Wager", "Spring bet on the cosmic age sign for bonus harvest."),
            },
        };

        static readonly string[] TabButtonNames =
        {
            "tab-cards", "tab-ages", "tab-states", "tab-terms"
        };

        static readonly Dictionary<string, string> TabToButton = new()
        {
            [CodexTabs.Cards] = "tab-cards",
            [CodexTabs.Ages] = "tab-ages",
            [CodexTabs.States] = "tab-states",
            [CodexTabs.Terms] = "tab-terms",
        };

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();

            var search = Root?.Q<TextField>("codex-search");
            if (search != null)
                search.RegisterValueChangedCallback(OnSearchChanged);

            foreach (var name in TabButtonNames)
            {
                var btn = Btn(name);
                if (btn == null) continue;
                var captured = name;
                btn.clicked += () => SelectTab(ButtonToTab(captured));
            }
        }

        protected override void Bind()
        {
            ApplyTabUi();
            RebuildList();
        }

        public void ShowTab(string tabKey)
        {
            _activeTab = TabEntries.ContainsKey(tabKey) ? tabKey : CodexTabs.Cards;
            if (!IsAttached) return;
            ApplyTabUi();
            RebuildList();
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _searchQuery = evt.newValue?.Trim() ?? "";
            OnSearch?.Invoke(_searchQuery);
            RebuildList();
        }

        private void SelectTab(string tabKey)
        {
            _activeTab = tabKey;
            OnTabChanged?.Invoke(tabKey);
            ApplyTabUi();
            RebuildList();
        }

        private void ApplyTabUi()
        {
            if (Root == null) return;
            foreach (var kv in TabToButton)
            {
                var btn = Btn(kv.Value);
                if (btn != null)
                    btn.EnableInClassList("codex-tab--active", kv.Key == _activeTab);
            }
        }

        private void RebuildList()
        {
            var list = Root?.Q<ScrollView>("codex-list");
            if (list == null) return;

            list.Clear();
            if (!TabEntries.TryGetValue(_activeTab, out var entries))
                return;

            var query = _searchQuery.ToLowerInvariant();
            foreach (var (title, body) in entries)
            {
                if (!string.IsNullOrEmpty(query)
                    && !title.ToLowerInvariant().Contains(query)
                    && !body.ToLowerInvariant().Contains(query))
                    continue;

                var row = new VisualElement();
                row.AddToClassList("panel");
                row.style.marginBottom = 7;

                var titleLbl = new Label(title);
                titleLbl.style.fontSize = 12;
                titleLbl.style.color = new StyleColor(new UnityEngine.Color(0.95f, 0.91f, 0.82f));

                var bodyLbl = new Label(body);
                bodyLbl.style.fontSize = 10;
                bodyLbl.style.color = new StyleColor(new UnityEngine.Color(0.72f, 0.6f, 0.43f));
                bodyLbl.style.whiteSpace = WhiteSpace.Normal;

                row.Add(titleLbl);
                row.Add(bodyLbl);
                list.Add(row);
            }
        }

        private static string ButtonToTab(string buttonName) => buttonName switch
        {
            "tab-ages" => CodexTabs.Ages,
            "tab-states" => CodexTabs.States,
            "tab-terms" => CodexTabs.Terms,
            _ => CodexTabs.Cards,
        };
    }
}
