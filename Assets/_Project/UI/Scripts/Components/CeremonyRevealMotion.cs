using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class CeremonyRevealMotion
    {
        static readonly List<IReadOnlyList<VisualElement?>> Groups = new();

        public static void RevealGameOverviewIntro(VisualElement? root)
        {
            if (root == null)
                return;

            Groups.Clear();
            Groups.Add(Group(root.Q(className: "menu-screen__hero")));
            Groups.Add(Group(root.Q<VisualElement>("overview-content-panel")));
            Groups.Add(Group(root.Q(className: "menu-screen__footer")));
            UiMotion.StaggerFadeIn(Groups);
        }

        public static void RevealSeasonInfoRecap(VisualElement? root)
        {
            if (root == null)
                return;

            var sheet = SeasonInfoRecapBindings.ResolveSheetRoot(root);
            var overviewHost = sheet.Q<VisualElement>("overview-host");
            var panels = overviewHost?.Query(className: "ceremony-panel-wrap").ToList();

            Groups.Clear();

            var headerGroup = new List<VisualElement?> { sheet.Q("recap-hero") };
            var tabs = sheet.Q(className: "season-info-recap__tabs");
            if (tabs != null)
                headerGroup.Add(tabs);
            if (panels != null && panels.Count > 0)
                headerGroup.Add(panels[0]);
            Groups.Add(headerGroup);

            if (panels != null && panels.Count > 1)
                Groups.Add(Group(panels[1]));

            Groups.Add(new List<VisualElement?>
            {
                sheet.Q<Label>(SeasonInfoRecapBindings.HubRecapNoteName),
                sheet.Q(className: "menu-screen__footer")
            });

            UiMotion.StaggerFadeIn(Groups);
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
            if (wager != null && wager.style.display != DisplayStyle.None)
                Groups.Add(Group(wager));

            Groups.Add(Group(castPhase.Q(className: "menu-screen__footer")));
            UiMotion.StaggerFadeIn(Groups);
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
            UiMotion.StaggerFadeIn(Groups);
        }

        public static void RevealAgeClosing(VisualElement? root)
        {
            if (root == null)
                return;

            Groups.Clear();
            Groups.Add(Group(root.Q(className: "menu-screen__hero")));

            var panels = root.Query(className: "ceremony-panel-wrap").ToList();
            if (panels.Count > 0)
                Groups.Add(Group(panels[0]));
            if (panels.Count > 1)
                Groups.Add(Group(panels[1]));

            Groups.Add(Group(root.Q(className: "menu-screen__footer")));
            UiMotion.StaggerFadeIn(Groups);
        }

        public static void RevealAgekeeperContest(VisualElement? root)
        {
            if (root == null)
                return;

            Groups.Clear();
            Groups.Add(Group(root.Q(className: "menu-screen__hero")));
            Groups.Add(Group(root.Q<VisualElement>("player-rolls")));
            Groups.Add(Group(root.Q(className: "agekeeper-contest__footer")
                ?? root.Q(className: "menu-screen__footer")));
            UiMotion.StaggerFadeIn(Groups);
        }

        public static void RevealVictory(VisualElement? root)
        {
            if (root == null)
                return;

            Groups.Clear();
            Groups.Add(Group(root.Q(className: "victory-hero")));
            Groups.Add(Group(root.Q<VisualElement>("victory-labors")));
            Groups.Add(Group(root.Q<VisualElement>("victory-standings")));
            Groups.Add(Group(root.Q<VisualElement>("victory-actions")));
            UiMotion.StaggerFadeIn(Groups);
        }

        public static void RevealChronicle(VisualElement? root)
        {
            if (root == null)
                return;

            Groups.Clear();
            Groups.Add(Group(root.Q(className: "statusbar")));
            Groups.Add(Group(root.Q<VisualElement>("chronicle-stats")));
            Groups.Add(Group(root.Q<VisualElement>("chronicle-chart")));
            Groups.Add(Group(root.Q<VisualElement>("chronicle-record")));
            Groups.Add(Group(root.Q<VisualElement>("chronicle-actions")));
            UiMotion.StaggerFadeIn(Groups);
        }

        static IReadOnlyList<VisualElement?> Group(params VisualElement?[] elements) => elements;
    }
}
