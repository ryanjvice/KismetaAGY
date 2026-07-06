using System.Collections.Generic;
using Kismeta.UI;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class ScreenRevealMotion
    {
        public const string RevealSectionClass = "ui-reveal-section";

        static readonly List<IReadOnlyList<VisualElement?>> Groups = new();

        static readonly HashSet<string> SkipFullScreenReveal = new()
        {
            ScreenIds.GameplayHud,
            ScreenIds.Waiting
        };

        public static bool ShouldRevealScreen(string? screenId) =>
            !string.IsNullOrEmpty(screenId) && !SkipFullScreenReveal.Contains(screenId);

        public static void Reveal(string? screenId, VisualElement? root)
        {
            if (root == null || !ShouldRevealScreen(screenId))
                return;

            Groups.Clear();
            if (TryBuildNamedScreenRecipe(screenId!, root, Groups)
                || TryBuildMarkedSections(root, Groups)
                || TryBuildAutoPattern(root, Groups))
            {
                Stagger(Groups);
            }
        }

        public static void RevealOverlay(VisualElement? root, string? overlayKey = null)
        {
            if (root == null)
                return;

            var key = overlayKey ?? root.name;
            Groups.Clear();
            if (TryBuildNamedOverlayRecipe(key, root, Groups)
                || TryBuildMarkedSections(root, Groups)
                || TryBuildSheetPattern(root, Groups)
                || TryBuildContestPattern(root, Groups)
                || TryBuildAutoPattern(root, Groups))
            {
                Stagger(Groups);
            }
        }

        public static void RevealStep(params VisualElement?[] sections)
        {
            Groups.Clear();
            foreach (var section in sections)
            {
                if (IsVisible(section))
                    Groups.Add(Group(section));
            }

            if (Groups.Count > 0)
                Stagger(Groups);
        }

        public static void RevealGameOverviewIntroStep(VisualElement? root, int stepIndex)
        {
            if (root == null)
                return;

            Groups.Clear();

            if (stepIndex == 0)
                Groups.Add(Group(root.Q(className: "menu-screen__hero")));

            var stepPanel = stepIndex == 0
                ? root.Q<VisualElement>("step-great-work")
                : root.Q<VisualElement>("step-seasons");
            if (IsVisible(stepPanel))
                Groups.Add(Group(stepPanel));

            Groups.Add(Group(root.Q(className: "menu-screen__footer")));
            Stagger(Groups);
        }

        public static void RevealRoundOpenCast(VisualElement? root)
        {
            if (root == null)
                return;

            var castPhase = root.Q<VisualElement>("cast-phase");
            if (castPhase == null)
                return;

            Groups.Clear();
            Groups.Add(Group(castPhase.Q(className: "menu-screen__hero")));

            var wager = castPhase.Q<VisualElement>("pending-wager");
            if (IsVisible(wager))
                Groups.Add(Group(wager));

            Groups.Add(Group(castPhase.Q(className: "menu-screen__footer")));
            Stagger(Groups);
        }

        public static void RevealRoundOpenReveal(VisualElement? root)
        {
            if (root == null)
                return;

            var revealPhase = root.Q<VisualElement>("reveal-phase");
            if (revealPhase == null)
                return;

            Groups.Clear();
            Groups.Add(Group(revealPhase.Q(className: "menu-screen__hero")));

            var panels = revealPhase.Query(className: "ceremony-panel-wrap").ToList();
            if (panels.Count > 0)
                Groups.Add(Group(panels[0]));
            if (panels.Count > 1)
                Groups.Add(Group(panels[1]));

            Groups.Add(Group(revealPhase.Q(className: "menu-screen__footer")));
            Stagger(Groups);
        }

        static bool TryBuildNamedScreenRecipe(
            string screenId,
            VisualElement root,
            List<IReadOnlyList<VisualElement?>> groups)
        {
            switch (screenId)
            {
                case ScreenIds.GameOverviewIntro:
                    return BuildGameOverviewIntro(root, groups, stepIndex: 0);
                case ScreenIds.SpringIntro:
                case ScreenIds.SummerIntro:
                case ScreenIds.AutumnIntro:
                case ScreenIds.WinterIntro:
                    return BuildSeasonInfoRecap(root, groups);
                case ScreenIds.RoundOpen:
                    return BuildRoundOpen(root, groups);
                case ScreenIds.AgeClosing:
                    return BuildAgeClosing(root, groups);
                case ScreenIds.AgekeeperContest:
                    return BuildAgekeeperContest(root, groups);
                case ScreenIds.Victory:
                    return BuildVictory(root, groups);
                case ScreenIds.Chronicle:
                    return BuildChronicle(root, groups);
                case ScreenIds.SpringHub:
                    return BuildSpringHub(root, groups);
                case ScreenIds.SpringHarvest:
                    return BuildSpringHarvest(root, groups);
                case ScreenIds.SpringPassed:
                case ScreenIds.SummerPassed:
                case ScreenIds.AutumnPassed:
                    return BuildPassedScreen(root, groups);
                case ScreenIds.SummerHub:
                case ScreenIds.AutumnHub:
                case ScreenIds.WinterHub:
                case ScreenIds.SummerMain:
                case ScreenIds.AutumnMain:
                    return BuildSeasonHub(root, groups);
                case ScreenIds.FatefulWager:
                case ScreenIds.CraftReagent:
                case ScreenIds.CardLimits:
                case ScreenIds.WinterUnlock:
                case ScreenIds.Commune:
                    return BuildGameplayStepScreen(root, groups);
                case ScreenIds.Join:
                case ScreenIds.Resume:
                case ScreenIds.Codex:
                case ScreenIds.Settings:
                case ScreenIds.Title:
                    return BuildMenuShellScreen(root, groups);
                default:
                    return false;
            }
        }

        static bool TryBuildNamedOverlayRecipe(
            string overlayKey,
            VisualElement root,
            List<IReadOnlyList<VisualElement?>> groups)
        {
            switch (overlayKey)
            {
                case "setup-sheet":
                case "main-menu-sheet":
                    return BuildSheet(root, groups);
                case "season-info-recap":
                    return BuildSeasonInfoRecap(root, groups);
                case "game-overview-intro":
                    return BuildGameOverviewIntro(root, groups, stepIndex: 0);
                case "duel":
                case "trade":
                case "gambit":
                case "opposition":
                case "contest-response":
                    return BuildContestPattern(root, groups);
                default:
                    return false;
            }
        }

        static bool BuildGameOverviewIntro(
            VisualElement root,
            List<IReadOnlyList<VisualElement?>> groups,
            int stepIndex)
        {
            if (stepIndex == 0)
                groups.Add(Group(root.Q(className: "menu-screen__hero")));

            var stepPanel = stepIndex == 0
                ? root.Q<VisualElement>("step-great-work")
                : root.Q<VisualElement>("step-seasons");
            if (IsVisible(stepPanel))
                groups.Add(Group(stepPanel));

            groups.Add(Group(root.Q(className: "menu-screen__footer")));
            return groups.Count > 0;
        }

        static bool BuildSeasonInfoRecap(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var sheet = SeasonInfoRecapBindings.ResolveSheetRoot(root);
            var overviewHost = sheet.Q<VisualElement>("overview-host");
            var panels = overviewHost?.Query(className: "ceremony-panel-wrap").ToList();

            var headerGroup = new List<VisualElement?> { sheet.Q("recap-hero") };
            var tabs = sheet.Q(className: "season-info-recap__tabs");
            if (tabs != null)
                headerGroup.Add(tabs);
            if (panels != null && panels.Count > 0)
                headerGroup.Add(panels[0]);
            groups.Add(headerGroup);

            if (panels != null && panels.Count > 1)
                groups.Add(Group(panels[1]));

            groups.Add(new List<VisualElement?>
            {
                sheet.Q<Label>(SeasonInfoRecapBindings.HubRecapNoteName),
                sheet.Q(className: "menu-screen__footer")
            });
            return true;
        }

        static bool BuildRoundOpen(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var revealPhase = root.Q<VisualElement>("reveal-phase");
            if (IsVisible(revealPhase))
            {
                groups.Add(Group(revealPhase!.Q(className: "menu-screen__hero")));
                var panels = revealPhase.Query(className: "ceremony-panel-wrap").ToList();
                if (panels.Count > 0)
                    groups.Add(Group(panels[0]));
                if (panels.Count > 1)
                    groups.Add(Group(panels[1]));
                groups.Add(Group(revealPhase.Q(className: "menu-screen__footer")));
                return true;
            }

            var castPhase = root.Q<VisualElement>("cast-phase");
            if (castPhase == null)
                return false;

            groups.Add(Group(castPhase.Q(className: "menu-screen__hero")));
            var wager = castPhase.Q<VisualElement>("pending-wager");
            if (IsVisible(wager))
                groups.Add(Group(wager));
            groups.Add(Group(castPhase.Q(className: "menu-screen__footer")));
            return true;
        }

        static bool BuildAgeClosing(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "menu-screen__hero")));
            var panels = root.Query(className: "ceremony-panel-wrap").ToList();
            if (panels.Count > 0)
                groups.Add(Group(panels[0]));
            if (panels.Count > 1)
                groups.Add(Group(panels[1]));
            groups.Add(Group(root.Q(className: "menu-screen__footer")));
            return true;
        }

        static bool BuildAgekeeperContest(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "menu-screen__hero")));
            groups.Add(Group(root.Q<VisualElement>("player-rolls")));
            groups.Add(Group(root.Q(className: "agekeeper-contest__footer")
                ?? root.Q(className: "menu-screen__footer")));
            return true;
        }

        static bool BuildVictory(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "victory-hero")));
            groups.Add(Group(root.Q<VisualElement>("victory-labors")));
            groups.Add(Group(root.Q<VisualElement>("victory-standings")));
            groups.Add(Group(root.Q<VisualElement>("victory-actions")));
            return true;
        }

        static bool BuildChronicle(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "statusbar")));
            groups.Add(Group(root.Q<VisualElement>("chronicle-stats")));
            groups.Add(Group(root.Q<VisualElement>("chronicle-chart")));
            groups.Add(Group(root.Q<VisualElement>("chronicle-record")));
            groups.Add(Group(root.Q<VisualElement>("chronicle-actions")));
            return true;
        }

        static bool BuildSpringHub(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var body = root.Q(className: "screen__body");
            if (body == null)
                return false;

            groups.Add(Group(body.Q(className: "game-header") ?? body.ElementAt(0)));
            groups.Add(Group(body.Q(className: "spring-hub__toolbar")));

            var actionBar = body.Q<VisualElement>("hub-actionbar");
            if (IsVisible(actionBar))
                groups.Add(Group(actionBar));

            groups.Add(Group(body.Q(className: "central-panel")));

            foreach (var ctaName in new[] { "wheel-cta", "hub-cta", "commune-cta" })
            {
                var cta = body.Q<VisualElement>(ctaName);
                if (IsVisible(cta))
                    groups.Add(Group(cta));
            }

            var inventory = body.Q(className: "player-inventory");
            if (IsVisible(inventory))
                groups.Add(Group(inventory));

            return groups.Count > 0;
        }

        static bool BuildSpringHarvest(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "screen__chrome")));
            groups.Add(Group(root.Q(className: "harvest-heading-row")));
            groups.Add(Group(root.Q(className: "harvest-compare")));
            groups.Add(Group(root.Q(className: "harvest-sources")));
            groups.Add(Group(root.Q(className: "harvest-footer")));
            return true;
        }

        static bool BuildPassedScreen(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var body = root.Q(className: "screen__body");
            if (body == null)
                return false;

            groups.Add(Group(body.Q(className: "game-header") ?? body.ElementAt(0)));
            var toolbar = body.Q(className: "narrative-toolbar")?.parent;
            if (IsVisible(toolbar))
                groups.Add(Group(toolbar));
            groups.Add(Group(body.Q(className: "central-panel")));
            return groups.Count > 0;
        }

        static bool BuildSeasonHub(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var body = root.Q(className: "screen__body");
            if (body == null)
                return false;

            groups.Add(Group(body.Q(className: "game-header") ?? body.ElementAt(0)));

            var toolbar = body.Q(className: "narrative-toolbar")?.parent;
            if (toolbar == null)
            {
                foreach (var child in body.Children())
                {
                    if (child.ClassListContains("toolbar") || child.name.Contains("toolbar"))
                    {
                        toolbar = child;
                        break;
                    }
                }
            }

            if (IsVisible(toolbar))
                groups.Add(Group(toolbar));

            groups.Add(Group(body.Q(className: "central-panel")));

            var footer = root.Q(className: "harvest-footer")
                ?? root.Q(className: "menu-screen__footer")
                ?? root.Q(className: "stage-cta");
            if (IsVisible(footer))
                groups.Add(Group(footer));

            var inventory = body.Q(className: "player-inventory");
            if (IsVisible(inventory))
                groups.Add(Group(inventory));

            return groups.Count > 0;
        }

        static bool BuildGameplayStepScreen(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var chrome = root.Q(className: "screen__chrome");
            if (chrome != null)
                groups.Add(Group(chrome));

            VisualElement? footer = root.Q(className: "harvest-footer")
                ?? root.Q(className: "menu-screen__footer");

            foreach (var child in root.Children())
            {
                if (child == chrome || child == footer || !IsVisible(child))
                    continue;
                if (child.ClassListContains("sr-only"))
                    continue;
                groups.Add(Group(child));
            }

            if (IsVisible(footer))
                groups.Add(Group(footer));

            return groups.Count > 0;
        }

        static bool BuildMenuShellScreen(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            if (root.Q(className: "title-hero") != null)
                return BuildTitleScreen(root, groups);

            var header = root.Q(className: "menu-screen__hero") ?? root.Q(className: "statusbar");
            if (header != null)
                groups.Add(Group(header));

            var body = root.Q(className: "menu-screen__body");
            if (IsVisible(body))
                groups.Add(Group(body));

            var footer = root.Q(className: "menu-screen__footer");
            if (IsVisible(footer))
                groups.Add(Group(footer));

            return groups.Count > 0;
        }

        static bool BuildTitleScreen(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "title-hero")));
            groups.Add(Group(root.Q(className: "title-menu")));
            return true;
        }

        static bool BuildSheet(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "sheet__header")));

            var body = root.Q(className: "sheet__body") ?? root;
            foreach (var section in body.Query(className: "setup-section").ToList())
                groups.Add(Group(section));

            var beginBtn = root.Q<Button>("begin-btn");
            if (IsVisible(beginBtn))
                groups.Add(Group(beginBtn));

            return groups.Count > 0;
        }

        static bool TryBuildMarkedSections(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var sections = root.Query(className: RevealSectionClass).ToList();
            if (sections.Count == 0)
                return false;

            foreach (var section in sections)
            {
                if (IsVisible(section))
                    groups.Add(Group(section));
            }

            return groups.Count > 0;
        }

        static bool TryBuildAutoPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            if (TryBuildTitlePattern(root, groups)
                || TryBuildMenuShellPattern(root, groups)
                || TryBuildGameplayStepPattern(root, groups)
                || TryBuildHubBodyPattern(root, groups)
                || TryBuildGenericScreenPattern(root, groups))
            {
                return true;
            }

            return false;
        }

        static bool TryBuildTitlePattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var hero = root.Q(className: "title-hero");
            if (hero == null)
                return false;

            groups.Add(Group(hero));
            groups.Add(Group(root.Q(className: "title-menu")));
            return true;
        }

        static bool TryBuildMenuShellPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var hero = root.Q(className: "menu-screen__hero");
            var statusbar = root.Q(className: "statusbar");
            if (hero == null && statusbar == null)
                return false;

            if (hero != null)
                groups.Add(Group(hero));
            else
                groups.Add(Group(statusbar));

            var body = root.Q(className: "menu-screen__body");
            if (IsVisible(body))
                groups.Add(Group(body));

            var scrollBody = root.Q(className: "ceremony-reveal-body");
            if (IsVisible(scrollBody))
            {
                foreach (var child in scrollBody.Children())
                {
                    if (IsVisible(child))
                        groups.Add(Group(child));
                }
            }

            var footer = root.Q(className: "menu-screen__footer");
            if (IsVisible(footer))
                groups.Add(Group(footer));

            return groups.Count > 0;
        }

        static bool TryBuildGameplayStepPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var chrome = root.Q(className: "screen__chrome") ?? root.Q(className: "game-header")?.parent;
            if (chrome == null && root.Q(className: "game-header") == null)
                return false;

            if (chrome != null)
                groups.Add(Group(chrome));

            VisualElement? footer = root.Q(className: "harvest-footer")
                ?? root.Q(className: "menu-screen__footer");

            foreach (var child in root.Children())
            {
                if (child == chrome || child == footer || !IsVisible(child))
                    continue;
                if (child.ClassListContains("sr-only"))
                    continue;
                groups.Add(Group(child));
            }

            if (IsVisible(footer))
                groups.Add(Group(footer));

            return groups.Count > 0;
        }

        static bool TryBuildHubBodyPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var body = root.Q(className: "screen__body");
            if (body == null)
                return false;

            foreach (var child in body.Children())
            {
                if (!IsVisible(child) || child.ClassListContains("sr-only"))
                    continue;
                groups.Add(Group(child));
            }

            return groups.Count > 0;
        }

        static bool TryBuildGenericScreenPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            if (!root.ClassListContains("screen"))
                return false;

            VisualElement? footer = null;
            Button? primaryBtn = null;

            foreach (var btn in root.Query<Button>(className: "btn--primary").ToList())
            {
                if (!IsVisible(btn))
                    continue;
                primaryBtn = btn;
            }

            foreach (var child in root.Children())
            {
                if (!IsVisible(child) || child.ClassListContains("sr-only"))
                    continue;
                if (primaryBtn != null && child.Contains(primaryBtn))
                {
                    footer = child;
                    continue;
                }
                groups.Add(Group(child));
            }

            if (IsVisible(footer))
                groups.Add(Group(footer));
            else if (IsVisible(primaryBtn))
                groups.Add(Group(primaryBtn));

            return groups.Count > 0;
        }

        static bool TryBuildSheetPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            if (!root.ClassListContains("sheet"))
                return false;
            return BuildSheet(root, groups);
        }

        static bool TryBuildContestPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            var statusbar = root.Q(className: "statusbar");
            if (statusbar == null)
                return false;
            return BuildContestPattern(root, groups);
        }

        static bool BuildContestPattern(VisualElement root, List<IReadOnlyList<VisualElement?>> groups)
        {
            groups.Add(Group(root.Q(className: "statusbar")));

            var wizard = root.Q(className: "wizard-steps");
            if (IsVisible(wizard))
                groups.Add(Group(wizard));

            VisualElement? activeStep = null;
            foreach (var child in root.Children())
            {
                if (!child.name.StartsWith("step-") || !IsVisible(child))
                    continue;
                activeStep = child;
                break;
            }

            if (activeStep == null)
            {
                activeStep = root.Q<VisualElement>("step-setup")
                    ?? root.Q<VisualElement>("step-roll");
            }

            if (IsVisible(activeStep))
                groups.Add(Group(activeStep));

            Button? primaryBtn = null;
            foreach (var btn in root.Query<Button>(className: "btn--primary").ToList())
            {
                if (IsVisible(btn))
                    primaryBtn = btn;
            }

            if (primaryBtn != null)
                groups.Add(Group(primaryBtn));

            return groups.Count > 0;
        }

        static bool IsVisible(VisualElement? element)
        {
            if (element == null)
                return false;
            if (element.style.display == DisplayStyle.None)
                return false;

            foreach (var cls in element.GetClasses())
            {
                if (cls.EndsWith("--hidden"))
                    return false;
            }

            return true;
        }

        static void Stagger(List<IReadOnlyList<VisualElement?>> groups) =>
            UiMotion.StaggerFadeIn(groups);

        static IReadOnlyList<VisualElement?> Group(params VisualElement?[] elements) => elements;
    }
}
