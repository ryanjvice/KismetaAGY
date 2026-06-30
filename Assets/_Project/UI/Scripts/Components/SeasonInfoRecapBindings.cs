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

        public static void Populate(
            VisualElement root,
            Season season,
            VisualTreeAsset? seasonIntroAsset,
            Action? onTabChanged = null)
        {
            if (root == null)
                return;

            CeremonyBindings.ApplySeasonIntroClass(root, season);
            UiArtBindings.ApplyIntroSigil(root);

            if (FocusLadderDefinitions.TryGetHero(season, out var hero))
            {
                SetText(root, "intro-name", hero.Name);
                SetText(root, "intro-tagline", hero.Tagline);
            }

            PopulateOverview(root, season, seasonIntroAsset);
            PopulateFocus(root, season);
            SelectTab(root, TabOverview, onTabChanged);
        }

        public static void WireTabs(VisualElement root, Action? onTabChanged = null)
        {
            if (root == null)
                return;

            root.Q<Button>("tab-overview")?.RegisterCallback<ClickEvent>(_ =>
                SelectTab(root, TabOverview, onTabChanged));
            root.Q<Button>("tab-focus")?.RegisterCallback<ClickEvent>(_ =>
                SelectTab(root, TabFocus, onTabChanged));
        }

        public static void SelectTab(VisualElement root, string tabId, Action? onTabChanged = null)
        {
            bool overview = tabId == TabOverview;

            root.Q<Button>("tab-overview")
                ?.EnableInClassList("codex-tab--active", overview);
            root.Q<Button>("tab-focus")
                ?.EnableInClassList("codex-tab--active", !overview);

            var overviewPane = root.Q<VisualElement>("overview-pane");
            var focusPane = root.Q<VisualElement>("focus-pane");
            if (overviewPane != null)
                overviewPane.EnableInClassList("season-info-recap__pane--hidden", !overview);
            if (focusPane != null)
                focusPane.EnableInClassList("season-info-recap__pane--hidden", overview);

            onTabChanged?.Invoke();
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

            NarrativeSlotBindings.BindById(cloneRoot, NarrativeStepResolver.ResolveSeasonIntro(season));
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

            var numWrap = new VisualElement();
            numWrap.AddToClassList("intro-row__num");
            numWrap.AddToClassList(FocusLadderDefinitions.RowNumClassForIndex(number - 1));

            var numLabel = new Label { text = number.ToString() };
            numLabel.AddToClassList("intro-row__num-label");
            numWrap.Add(numLabel);
            row.Add(numWrap);

            var textWrap = new VisualElement();
            textWrap.style.flexGrow = 1;

            var titleLabel = new Label { text = title };
            titleLabel.AddToClassList("intro-row__label");
            textWrap.Add(titleLabel);

            if (!string.IsNullOrWhiteSpace(description) && description != title)
            {
                var descLabel = new Label { text = description };
                descLabel.AddToClassList("text-muted");
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                descLabel.style.fontSize = 11;
                descLabel.style.marginTop = 2;
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
