using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class ScreenRevealMotion_Tests
    {
        const string SeasonInfoRecapPath = "Assets/_Project/UI/UXML/shell/SeasonInfoRecap.uxml";
        const string SpringIntroPath = "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml";
        const string GameOverviewIntroPath = "Assets/_Project/UI/UXML/batch1/GameOverviewIntro.uxml";
        const string JoinScreenPath = "Assets/_Project/UI/UXML/batch1/JoinScreen.uxml";
        const string SetupSheetPath = "Assets/_Project/UI/UXML/batch1/SetupSheet.uxml";

        [TearDown]
        public void TearDown() => GameSettings.SetReducedMotion(false);

        [Test]
        public void ShouldRevealScreen_SkipsPersistentHudShells()
        {
            Assert.IsFalse(ScreenRevealMotion.ShouldRevealScreen(ScreenIds.GameplayHud));
            Assert.IsFalse(ScreenRevealMotion.ShouldRevealScreen(ScreenIds.Waiting));
            Assert.IsTrue(ScreenRevealMotion.ShouldRevealScreen(ScreenIds.SpringHub));
        }

        [Test]
        public void Reveal_SeasonIntro_WithReducedMotion_RevealsAllSections()
        {
            GameSettings.SetReducedMotion(true);

            var shell = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SeasonInfoRecapPath);
            var overview = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringIntroPath);
            Assert.NotNull(shell);
            Assert.NotNull(overview);

            var root = shell.CloneTree();
            SeasonInfoRecapBindings.PopulateCeremony(root, Core.Domain.Season.Spring, overview);
            ScreenRevealMotion.Reveal(ScreenIds.SpringIntro, root);

            var sheet = SeasonInfoRecapBindings.ResolveSheetRoot(root);
            Assert.AreEqual(1f, sheet.Q("recap-hero")!.style.opacity.value);
            Assert.AreEqual(1f, sheet.Q(className: "menu-screen__footer")!.style.opacity.value);
        }

        [Test]
        public void Reveal_GameOverviewIntro_WithReducedMotion_RevealsHeroPanelAndFooter()
        {
            GameSettings.SetReducedMotion(true);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameOverviewIntroPath);
            Assert.NotNull(asset);

            var root = asset.CloneTree();
            ScreenRevealMotion.Reveal(ScreenIds.GameOverviewIntro, root);

            Assert.AreEqual(1f, root.Q(className: "menu-screen__hero")!.style.opacity.value);
            Assert.AreEqual(1f, root.Q<VisualElement>("step-great-work")!.style.opacity.value);
            Assert.AreEqual(1f, root.Q(className: "menu-screen__footer")!.style.opacity.value);
        }

        [Test]
        public void RevealOverlay_SetupSheet_WithReducedMotion_RevealsHeaderAndSections()
        {
            GameSettings.SetReducedMotion(true);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SetupSheetPath);
            Assert.NotNull(asset);

            var root = asset.CloneTree();
            ScreenRevealMotion.RevealOverlay(root, "setup-sheet");

            Assert.AreEqual(1f, root.Q(className: "sheet__header")!.style.opacity.value);
            Assert.AreEqual(1f, root.Q<Button>("begin-btn")!.style.opacity.value);
        }

        [Test]
        public void Reveal_JoinScreen_WithReducedMotion_RevealsStatusbarAndBody()
        {
            GameSettings.SetReducedMotion(true);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(JoinScreenPath);
            Assert.NotNull(asset);

            var root = asset.CloneTree();
            ScreenRevealMotion.Reveal(ScreenIds.Join, root);

            Assert.AreEqual(1f, root.Q(className: "statusbar")!.style.opacity.value);
            Assert.AreEqual(1f, root.Q(className: "menu-screen__body")!.style.opacity.value);
        }
    }
}
