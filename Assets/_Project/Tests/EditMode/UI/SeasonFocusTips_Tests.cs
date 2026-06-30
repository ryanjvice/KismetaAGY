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

        static VisualElement InstantiateRecapShell()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SeasonInfoRecapPath);
            Assert.IsNotNull(asset, SeasonInfoRecapPath);
            return asset!.Instantiate();
        }
    }
}
