using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class CardModalScrollLayout_Tests
    {
        const string CardModalsPath = "Assets/_Project/UI/UXML/batch7/CardModals.uxml";
        const string KismetaUssPath = "Assets/_Project/UI/USS/Kismeta.uss";
        const string EndOverlaysUssPath = "Assets/_Project/UI/USS/EndAndOverlays.uss";

        static readonly string[] ModalIdsWithFooter =
        {
            "inspect-modal",
            "adept-modal",
            "fate-modal",
            "moon-modal",
            "fool-altar-modal",
            "emperor-modal",
        };

        static readonly string[] ModalIdsScrollOnly =
        {
            "fate-reagent-modal",
            "lovers-target-modal",
            "lovers-choice-modal",
        };

        [Test]
        public void AllCardModals_UseScrollViewBody()
        {
            var tree = InstantiateCardModals();
            foreach (var modalId in AllModalIds())
            {
                var modal = tree.Q<VisualElement>(modalId);
                Assert.IsNotNull(modal, $"Missing modal {modalId}");
                Assert.IsNotNull(modal!.Q<ScrollView>(className: "card-modal__body-scroll"),
                    $"{modalId} should contain a ScrollView with class card-modal__body-scroll");
                Assert.IsNotNull(modal.Q(className: "card-modal__body-scroll-content"),
                    $"{modalId} should contain card-modal__body-scroll-content");
            }
        }

        [Test]
        public void ModalsWithActionButtons_HavePinnedFooter()
        {
            var tree = InstantiateCardModals();
            foreach (var modalId in ModalIdsWithFooter)
            {
                var modal = tree.Q<VisualElement>(modalId);
                Assert.IsNotNull(modal!.Q(className: "card-modal__footer"),
                    $"{modalId} should have a pinned card-modal__footer");
            }
        }

        [Test]
        public void ModalsWithoutDistinctFooter_ScrollViewIsLastChild()
        {
            var tree = InstantiateCardModals();
            foreach (var modalId in ModalIdsScrollOnly)
            {
                var modal = tree.Q<VisualElement>(modalId);
                Assert.IsNull(modal!.Q(className: "card-modal__footer"),
                    $"{modalId} should not have a footer");
                Assert.IsTrue(modal.ElementAt(modal.childCount - 1) is ScrollView,
                    $"{modalId} should end with a ScrollView");
            }
        }

        [Test]
        public void InspectModal_HasBoundedScrollSetupAndTallContent()
        {
            var cardModals = InstantiateCardModals();
            var inspect = cardModals.Q<VisualElement>("inspect-modal");
            var scroll = inspect!.Q<ScrollView>(className: "card-modal__body-scroll");
            var scrollContent = inspect.Q(className: "card-modal__body-scroll-content");

            Assert.IsNotNull(scroll, "Inspect modal should use a ScrollView body.");
            Assert.IsNotNull(scrollContent, "Inspect modal should wrap detail blocks in scroll content.");
            Assert.IsNotNull(inspect.Q(className: "card-modal__footer"),
                "Inspect modal should keep Done pinned outside the scroll region.");
            Assert.IsNotNull(scrollContent!.Q<Label>("inspect-align-age"),
                "Alignment block should live inside scroll content, not below a clipping edge.");

            var blocks = scrollContent.Query(className: "inspect-block").ToList();
            Assert.GreaterOrEqual(blocks.Count, 2,
                "Inspect content should include multiple blocks that require scroll on short viewports.");

            var ussPath = Path.Combine(Application.dataPath, "_Project/UI/USS/EndAndOverlays.uss");
            var uss = File.ReadAllText(ussPath);
            StringAssert.Contains(".card-modal__body-scroll .unity-scroll-view__content-viewport", uss);
            StringAssert.Contains(".overlay-clone-host--card-modals", uss);
            StringAssert.Contains(".card-modals > .card-modal", uss);
            StringAssert.Contains(".overlay-clone-host--card-modals > #card-modals", uss);
            StringAssert.Contains("height: 100%", uss);
            StringAssert.Contains("flex-basis: 0", uss);
        }

        static string[] AllModalIds()
        {
            var all = new string[ModalIdsWithFooter.Length + ModalIdsScrollOnly.Length];
            ModalIdsWithFooter.CopyTo(all, 0);
            ModalIdsScrollOnly.CopyTo(all, ModalIdsWithFooter.Length);
            return all;
        }

        static VisualElement InstantiateCardModals()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);
            Assert.IsNotNull(asset, $"Missing UXML at {CardModalsPath}");
            var tree = asset!.Instantiate();

            AddStylesheet(tree, KismetaUssPath);
            AddStylesheet(tree, EndOverlaysUssPath);
            return tree;
        }

        static void AddStylesheet(VisualElement root, string path)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            Assert.IsNotNull(sheet, $"Missing stylesheet at {path}");
            if (!root.styleSheets.Contains(sheet!))
                root.styleSheets.Add(sheet);
        }
    }
}
