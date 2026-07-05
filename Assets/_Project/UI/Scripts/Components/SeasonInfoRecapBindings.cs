using System;
using Kismeta.Core.Domain;
using Kismeta.UI.Controllers;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class SeasonInfoRecapBindings
    {
        public const string TabOverview = "overview";
        public const string TabFocus = "focus";
        public const string PrimaryActionBtnName = "primary-action-btn";
        public const string HubRecapNoteName = "hub-recap-note";
        public const string GreatYearOverviewBtnName = "great-year-overview-btn";
        public const string SeasonEmojiName = "season-emoji";
        public const string GreatYearOverviewArtName = "great-year-overview-art";

        static readonly string HubRecapNoteText =
            $"Need a refresher later? On any season screen, tap {SymbolGlyphs.SeasonRecapGlyph} on the story toolbar to reopen this guide.";

        sealed class IntroRecapState
        {
            public bool TabsWired;
            public bool ContentPopulated;
            public bool ShowHubRecapNote;
            public string ActiveTab = TabOverview;
        }

        public static VisualElement ResolveSheetRoot(VisualElement root) =>
            root.Q<VisualElement>("season-info-recap") ?? root;

        static IntroRecapState GetState(VisualElement root)
        {
            var sheet = ResolveSheetRoot(root);
            if (sheet.userData is IntroRecapState state)
                return state;

            state = new IntroRecapState();
            sheet.userData = state;
            return state;
        }

        public static void Populate(
            VisualElement root,
            Season season,
            VisualTreeAsset? seasonIntroAsset,
            Action? onTabChanged = null,
            bool resetTabToOverview = false)
        {
            if (root == null)
                return;

            var sheet = ResolveSheetRoot(root);
            var state = GetState(sheet);
            if (resetTabToOverview)
                state.ActiveTab = TabOverview;

            CeremonyBindings.ApplySeasonIntroClass(sheet, season);
            ApplyHeroChrome(sheet, season);

            if (FocusLadderDefinitions.TryGetHero(season, out var hero))
            {
                SetText(sheet, "intro-name", hero.Name);
                SetText(sheet, "intro-tagline", hero.Tagline);
            }

            PopulateOverview(sheet, season, seasonIntroAsset);
            PopulateFocus(sheet, season);
            ApplyTabSelection(sheet, state.ActiveTab, onTabChanged);
            state.ContentPopulated = true;
        }

        public static void PopulateCeremony(
            VisualElement root,
            Season season,
            VisualTreeAsset? overviewAsset,
            Action? onTabChanged = null)
        {
            if (root == null)
                return;

            var sheet = ResolveSheetRoot(root);
            sheet.EnableInClassList("season-info-recap--ceremony", true);
            GetState(sheet).ShowHubRecapNote = true;

            var state = GetState(sheet);
            if (state.ContentPopulated)
            {
                ApplyPrimaryAction(sheet, ResolveCeremonyPrimaryLabel(season));
                RefreshHubRecapNote(sheet);
                return;
            }

            Populate(sheet, season, overviewAsset, onTabChanged);
            ApplyPrimaryAction(sheet, ResolveCeremonyPrimaryLabel(season));
        }

        public static void PopulateRecap(
            VisualElement root,
            Season season,
            VisualTreeAsset? overviewAsset,
            Action? onTabChanged = null)
        {
            if (root == null)
                return;

            var sheet = ResolveSheetRoot(root);
            sheet.EnableInClassList("season-info-recap--ceremony", false);
            var state = GetState(sheet);
            state.ContentPopulated = false;
            state.ShowHubRecapNote = false;
            Populate(sheet, season, overviewAsset, onTabChanged, resetTabToOverview: true);
            ApplyPrimaryAction(sheet, "Close");
        }

        static void RefreshHubRecapNote(VisualElement sheet)
        {
            var note = sheet.Q<Label>(HubRecapNoteName);
            if (note == null)
                return;

            var state = GetState(sheet);
            bool visible = state.ShowHubRecapNote && state.ActiveTab == TabOverview;
            if (visible)
            {
                note.text = HubRecapNoteText;
                note.style.display = DisplayStyle.Flex;
            }
            else
            {
                note.text = string.Empty;
                note.style.display = DisplayStyle.None;
            }
        }

        public static void ApplyPrimaryAction(VisualElement root, string label)
        {
            var btn = ResolveSheetRoot(root).Q<Button>(PrimaryActionBtnName);
            if (btn != null)
                btn.text = label ?? string.Empty;
        }

        public static string ResolveCeremonyPrimaryLabel(Season season)
        {
            var catalog = NarrativeSlotCatalog.Load();
            var introId = NarrativeStepResolver.ResolveSeasonIntro(season);
            if (catalog.TryGet(introId, out var entry) && entry.Verbs.Length > 0)
                return entry.Verbs[0];

            return "Continue";
        }

        public static void ApplyHeroChrome(VisualElement root, Season season)
        {
            var sheet = ResolveSheetRoot(root);
            var emoji = sheet.Q<Label>(SeasonEmojiName);
            if (emoji != null)
                emoji.text = SymbolGlyphs.SeasonEmoji(season);

            UiArtBindings.ApplyBackground(
                sheet.Q(GreatYearOverviewArtName),
                UiArtBindings.Catalog?.CrucibleForge,
                BackgroundSizeType.Contain);
        }

        public static void WireGreatYearOverview(VisualElement root, Action? onClick)
        {
            var btn = ResolveSheetRoot(root).Q<Button>(GreatYearOverviewBtnName);
            if (btn == null || onClick == null)
                return;

            UnwireGreatYearOverview(root);
            btn.userData = onClick;
            btn.clicked += onClick;
        }

        public static void UnwireGreatYearOverview(VisualElement root)
        {
            var btn = ResolveSheetRoot(root).Q<Button>(GreatYearOverviewBtnName);
            if (btn?.userData is not Action onClick)
                return;

            btn.clicked -= onClick;
            btn.userData = null;
        }

        public static void WireTabs(VisualElement root, Action? onTabChanged = null)
        {
            if (root == null)
                return;

            var sheet = ResolveSheetRoot(root);
            var state = GetState(sheet);
            if (state.TabsWired)
                return;

            state.TabsWired = true;
            WireTabTarget(sheet, "tab-overview", TabOverview, onTabChanged);
            WireTabTarget(sheet, "tab-focus", TabFocus, onTabChanged);
        }

        public static void SelectTab(VisualElement root, string tabId, Action? onTabChanged = null)
        {
            if (root == null)
                return;

            ApplyTabSelection(ResolveSheetRoot(root), tabId, onTabChanged);
        }

        static void ApplyTabSelection(VisualElement sheet, string tabId, Action? onTabChanged = null)
        {
            if (tabId != TabOverview && tabId != TabFocus)
                tabId = TabOverview;

            GetState(sheet).ActiveTab = tabId;
            bool overview = tabId == TabOverview;

            sheet.Q<VisualElement>("tab-overview")
                ?.EnableInClassList("codex-tab--active", overview);
            sheet.Q<VisualElement>("tab-focus")
                ?.EnableInClassList("codex-tab--active", !overview);

            var overviewPane = sheet.Q<VisualElement>("overview-pane");
            var focusPane = sheet.Q<VisualElement>("focus-pane");
            if (overviewPane != null)
                overviewPane.EnableInClassList("season-info-recap__pane--hidden", !overview);
            if (focusPane != null)
                focusPane.EnableInClassList("season-info-recap__pane--hidden", overview);

            RefreshHubRecapNote(sheet);
            onTabChanged?.Invoke();
        }

        static void WireTabTarget(VisualElement sheet, string name, string tabId, Action? onTabChanged)
        {
            var tab = sheet.Q<VisualElement>(name);
            if (tab == null)
                return;

            tab.pickingMode = PickingMode.Position;
            tab.focusable = true;
            SetIgnorePicking(tab);

            tab.Q(className: "season-info-recap__tab-hit")?.RemoveFromHierarchy();

            var hit = new VisualElement();
            hit.AddToClassList("season-info-recap__tab-hit");
            hit.pickingMode = PickingMode.Position;
            tab.Add(hit);
            hit.AddManipulator(new Clickable(() => ApplyTabSelection(sheet, tabId, onTabChanged)));
        }

        static void SetIgnorePicking(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                if (child.ClassListContains("season-info-recap__tab-hit"))
                    continue;
                child.pickingMode = PickingMode.Ignore;
                SetIgnorePicking(child);
            }
        }

        static void PopulateOverview(VisualElement root, Season season, VisualTreeAsset? seasonIntroAsset)
        {
            var host = root.Q<VisualElement>("overview-host");
            if (host == null || seasonIntroAsset == null)
                return;

            host.Clear();
            var instance = seasonIntroAsset.Instantiate();
            host.Add(instance);

            var cloneRoot = instance;
            cloneRoot.Q(className: "menu-screen__hero")?.AddToClassList("season-info-recap__clone-hidden");
            cloneRoot.Q(className: "menu-screen__footer")?.AddToClassList("season-info-recap__clone-hidden");

            var bindRoot = cloneRoot.Q(className: "ceremony-reveal-body")
                ?? cloneRoot.Q(className: "season-intro-overview")
                ?? cloneRoot;
            NarrativeSlotBindings.BindById(bindRoot, NarrativeStepResolver.ResolveSeasonIntro(season));
        }

        static void PopulateFocus(VisualElement root, Season season)
        {
            var catalog = NarrativeSlotCatalog.Load();
            var focusId = NarrativeStepResolver.ResolveSeasonFocus(season);
            var tip = root.Q<Label>("focus-tip");
            if (tip != null)
            {
                if (catalog.TryGet(focusId, out var entry) && !string.IsNullOrWhiteSpace(entry.Charge))
                {
                    tip.text = entry.Charge;
                    tip.style.display = DisplayStyle.Flex;
                }
                else
                {
                    tip.text = string.Empty;
                    tip.style.display = DisplayStyle.None;
                }
            }

            var tipsHost = root.Q<VisualElement>("focus-tips");
            if (tipsHost != null)
            {
                tipsHost.Clear();
                var tips = FocusLadderDefinitions.TipsFor(season);
                for (int i = 0; i < tips.Count; i++)
                {
                    var tipEntry = tips[i];
                    tipsHost.Add(BuildStepRow(i + 1, tipEntry.Title, tipEntry.Detail));
                }
            }
        }

        static VisualElement BuildStepRow(int number, string title, string description)
        {
            var row = new VisualElement();
            row.AddToClassList("intro-row");
            row.pickingMode = PickingMode.Ignore;

            var numWrap = new VisualElement();
            numWrap.AddToClassList("intro-row__num");
            numWrap.AddToClassList(FocusLadderDefinitions.RowNumClassForIndex(number - 1));
            numWrap.pickingMode = PickingMode.Ignore;

            var numLabel = new Label { text = number.ToString() };
            numLabel.AddToClassList("intro-row__num-label");
            numLabel.pickingMode = PickingMode.Ignore;
            numWrap.Add(numLabel);
            row.Add(numWrap);

            var textWrap = new VisualElement();
            textWrap.style.flexGrow = 1;
            textWrap.pickingMode = PickingMode.Ignore;

            var titleLabel = new Label { text = title };
            titleLabel.AddToClassList("intro-row__label");
            titleLabel.pickingMode = PickingMode.Ignore;
            textWrap.Add(titleLabel);

            if (!string.IsNullOrWhiteSpace(description) && description != title)
            {
                var descLabel = new Label { text = description };
                descLabel.AddToClassList("text-muted");
                descLabel.AddToClassList("text-muted--detail");
                descLabel.pickingMode = PickingMode.Ignore;
                textWrap.Add(descLabel);
            }

            row.Add(textWrap);
            return row;
        }

        static void SetText(VisualElement root, string name, string text)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null)
                lbl.text = text ?? string.Empty;
        }
    }
}
