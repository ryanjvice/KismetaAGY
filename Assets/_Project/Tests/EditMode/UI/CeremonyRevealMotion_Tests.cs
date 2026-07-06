using Kismeta.UI.Components;
using Kismeta.UI.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class CeremonyRevealMotion_Tests
    {
        const string SeasonInfoRecapPath = "Assets/_Project/UI/UXML/shell/SeasonInfoRecap.uxml";
        const string SpringIntroPath = "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml";
        const string GameOverviewIntroPath = "Assets/_Project/UI/UXML/batch1/GameOverviewIntro.uxml";

        [TearDown]
        public void TearDown() => GameSettings.SetReducedMotion(false);

        [Test]
        public void RevealSeasonInfoRecap_WithReducedMotion_RevealsAllSections()
        {
            GameSettings.SetReducedMotion(true);

            var shell = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SeasonInfoRecapPath);
            var overview = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringIntroPath);
            Assert.NotNull(shell);
            Assert.NotNull(overview);

            var root = shell.CloneTree();
            SeasonInfoRecapBindings.PopulateCeremony(root, Core.Domain.Season.Spring, overview);
            CeremonyRevealMotion.RevealSeasonInfoRecap(root);

            var sheet = SeasonInfoRecapBindings.ResolveSheetRoot(root);
            Assert.AreEqual(1f, sheet.Q("recap-hero")!.style.opacity.value);
            Assert.AreEqual(1f, sheet.Q(className: "menu-screen__footer")!.style.opacity.value);
        }

        [Test]
        public void RevealGameOverviewIntro_WithReducedMotion_RevealsHeroPanelAndFooter()
        {
            GameSettings.SetReducedMotion(true);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameOverviewIntroPath);
            Assert.NotNull(asset);

            var root = asset.CloneTree();
            CeremonyRevealMotion.RevealGameOverviewIntro(root, stepIndex: 0);

            Assert.AreEqual(1f, root.Q(className: "menu-screen__hero")!.style.opacity.value);
            Assert.AreEqual(1f, root.Q<VisualElement>("step-great-work")!.style.opacity.value);
            Assert.AreEqual(1f, root.Q(className: "menu-screen__footer")!.style.opacity.value);
        }
    }
}
