using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Phases;
using Kismeta.UI.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class SeasonFocusTips_Tests
    {
        const string SeasonInfoRecapPath = "Assets/_Project/UI/UXML/shell/SeasonInfoRecap.uxml";

        static readonly Season[] AllSeasons =
        {
            Season.Spring,
            Season.Summer,
            Season.Autumn,
            Season.Winter
        };

        [Test]
        public void TipsFor_EachSeason_HasAtLeastFiveNonEmptyTips()
        {
            foreach (var season in AllSeasons)
            {
                var tips = FocusLadderDefinitions.TipsFor(season);
                Assert.GreaterOrEqual(tips.Count, 5, season.ToString());

                foreach (var tip in tips)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(tip.Title), season.ToString());
                    Assert.IsFalse(string.IsNullOrWhiteSpace(tip.Detail), season.ToString());
                }
            }
        }

        [Test]
        public void TipsFor_DoesNotRepeatPhaseStepDescriptionsVerbatim()
        {
            foreach (var season in AllSeasons)
            {
                var phaseDescriptions = PhaseDefinitions.StepsFor(season)
                    .Select(s => s.Description)
                    .ToHashSet();

                foreach (var tip in FocusLadderDefinitions.TipsFor(season))
                {
                    Assert.IsFalse(
                        phaseDescriptions.Contains(tip.Title),
                        $"{season}: tip title should not duplicate a phase step description");
                    Assert.IsFalse(
                        phaseDescriptions.Contains(tip.Detail),
                        $"{season}: tip detail should not duplicate a phase step description");
                }
            }
        }

        [Test]
        public void ApplyHeroChrome_SetsSeasonEmoji([Values] Season season)
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.ApplyHeroChrome(root, season);

            var emoji = root.Q<Label>(SeasonInfoRecapBindings.SeasonEmojiName);
            Assert.IsNotNull(emoji);
            Assert.AreEqual(SymbolGlyphs.SeasonEmoji(season), emoji!.text);
        }

        [Test]
        public void Populate_RendersFocusTipCountMatchingDefinitions([Values] Season season)
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.Populate(root, season, seasonIntroAsset: null);

            var tipsHost = root.Q<VisualElement>("focus-tips");
            Assert.IsNotNull(tipsHost);
            Assert.AreEqual(
                FocusLadderDefinitions.TipsFor(season).Count,
                tipsHost!.childCount,
                season.ToString());
        }

        [Test]
        public void PopulateCeremony_SetsPrimaryLabelFromNarrativeVerb()
        {
            var root = InstantiateRecapShell();
            var overview = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml");

            SeasonInfoRecapBindings.PopulateCeremony(root, Season.Spring, overview);

            var btn = root.Q<Button>(SeasonInfoRecapBindings.PrimaryActionBtnName);
            Assert.IsNotNull(btn);
            Assert.AreEqual("Begin Spring", btn!.text);
            Assert.IsTrue(
                SeasonInfoRecapBindings.ResolveSheetRoot(root)
                    .ClassListContains("season-info-recap--ceremony"));
        }

        [Test]
        public void PopulateCeremony_LoadsOverviewFragment([Values] Season season)
        {
            var overviewPath = season switch
            {
                Season.Spring => "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml",
                Season.Summer => "Assets/_Project/UI/UXML/batch2/SummerIntro.uxml",
                Season.Autumn => "Assets/_Project/UI/UXML/batch2/AutumnIntro.uxml",
                Season.Winter => "Assets/_Project/UI/UXML/batch2/WinterIntro.uxml",
                _ => "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml"
            };

            var root = InstantiateRecapShell();
            var overview = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(overviewPath);
            Assert.IsNotNull(overview, overviewPath);

            SeasonInfoRecapBindings.PopulateCeremony(root, season, overview);

            var host = root.Q<VisualElement>("overview-host");
            Assert.IsNotNull(host);
            Assert.AreEqual(1, host!.childCount, season.ToString());
            Assert.IsNotNull(host[0].Q(className: "season-intro-overview"));
        }

        [Test]
        public void SelectTab_PersistsAcrossPopulate()
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.WireTabs(root);
            SeasonInfoRecapBindings.SelectTab(root, SeasonInfoRecapBindings.TabFocus);

            SeasonInfoRecapBindings.Populate(root, Season.Summer, seasonIntroAsset: null);

            Assert.IsTrue(
                root.Q<VisualElement>("tab-focus")!.ClassListContains("codex-tab--active"));
            Assert.IsFalse(
                root.Q<VisualElement>("focus-pane")!.ClassListContains("season-info-recap__pane--hidden"));
        }

        [Test]
        public void PopulateRecap_ResetsToOverviewTab()
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.WireTabs(root);
            SeasonInfoRecapBindings.SelectTab(root, SeasonInfoRecapBindings.TabFocus);

            SeasonInfoRecapBindings.PopulateRecap(root, Season.Summer, overviewAsset: null);

            Assert.IsTrue(
                root.Q<VisualElement>("tab-overview")!.ClassListContains("codex-tab--active"));
        }

        [Test]
        public void PopulateCeremony_SkipsContentRebuildWhenAlreadyPopulated()
        {
            var root = InstantiateRecapShell();
            var overview = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml");

            SeasonInfoRecapBindings.PopulateCeremony(root, Season.Spring, overview);
            SeasonInfoRecapBindings.SelectTab(root, SeasonInfoRecapBindings.TabFocus);

            var host = root.Q<VisualElement>("overview-host");
            Assert.IsNotNull(host);
            var overviewChild = host!.ElementAt(0);

            SeasonInfoRecapBindings.PopulateCeremony(root, Season.Spring, overview);

            Assert.AreSame(overviewChild, host.ElementAt(0));
            Assert.IsTrue(root.Q<VisualElement>("tab-focus")!.ClassListContains("codex-tab--active"));
        }

        [Test]
        public void PopulateCeremony_ShowsHubRecapNote()
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.PopulateCeremony(root, Season.Spring, overviewAsset: null);

            var note = root.Q<Label>(SeasonInfoRecapBindings.HubRecapNoteName);
            Assert.IsNotNull(note);
            Assert.AreEqual(DisplayStyle.Flex, note!.style.display.value);
            StringAssert.Contains(SymbolGlyphs.SeasonRecapGlyph, note.text);
        }

        [Test]
        public void PopulateRecap_HidesHubRecapNote()
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.PopulateCeremony(root, Season.Summer, overviewAsset: null);
            SeasonInfoRecapBindings.PopulateRecap(root, Season.Summer, overviewAsset: null);

            var note = root.Q<Label>(SeasonInfoRecapBindings.HubRecapNoteName);
            Assert.IsNotNull(note);
            Assert.AreEqual(DisplayStyle.None, note!.style.display.value);
        }

        [Test]
        public void SelectTab_Focus_HidesHubRecapNoteOnCeremony()
        {
            var root = InstantiateRecapShell();
            SeasonInfoRecapBindings.PopulateCeremony(root, Season.Spring, overviewAsset: null);

            SeasonInfoRecapBindings.SelectTab(root, SeasonInfoRecapBindings.TabFocus);

            var note = root.Q<Label>(SeasonInfoRecapBindings.HubRecapNoteName);
            Assert.IsNotNull(note);
            Assert.AreEqual(DisplayStyle.None, note!.style.display.value);
        }

        static VisualElement InstantiateRecapShell()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SeasonInfoRecapPath);
            Assert.IsNotNull(asset, SeasonInfoRecapPath);
            return asset!.Instantiate();
        }
    }
}
