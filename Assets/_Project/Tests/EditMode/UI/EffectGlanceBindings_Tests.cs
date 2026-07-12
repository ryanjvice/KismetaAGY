using System.Collections.Generic;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class EffectGlanceBindings_Tests
    {
        const string OverlayPath = "Assets/_Project/UI/UXML/shell/PlayerInventoryOverlay.uxml";

        [Test]
        public void Overlay_HasNoEffectGlanceStrip()
        {
            var root = InstantiateOverlay();
            Assert.IsNull(root.Q<VisualElement>("effect-glance-strip"));
        }

        [Test]
        public void Populate_ZeroEffects_HidesBadgeAndPolarity()
        {
            var root = InstantiateOverlay();
            var fab = root.Q<Button>("effects-fab");
            Assert.IsNotNull(fab);

            EffectGlanceBindings.Populate(root, EffectGlanceSnapshot.Empty);

            Assert.IsTrue(fab!.Q<Label>("effects-fab-count")!.ClassListContains("is-hidden"));
            Assert.IsFalse(fab.ClassListContains("effects-fab--has-count"));
            Assert.IsFalse(fab.ClassListContains("effects-fab--debuff"));
            Assert.IsFalse(fab.ClassListContains("effects-fab--buff"));
            Assert.IsFalse(fab.ClassListContains("effects-fab--pending"));
            Assert.IsFalse(fab.ClassListContains("effects-fab--neutral"));
            Assert.AreEqual(string.Empty, fab.tooltip);
        }

        [Test]
        public void Populate_DebuffChip_AppliesDebuffPolarityAndCountBadge()
        {
            var root = InstantiateOverlay();
            var fab = root.Q<Button>("effects-fab");
            var debuff = new ActiveEffectItem(
                "curse-1",
                "Curse",
                "A lingering debuff.",
                new ActiveEffectBadge("active", ActiveEffectBadgeTone.Debuff),
                polarity: ActiveEffectPolarity.Debuff);
            var glance = new EffectGlanceSnapshot(
                5,
                new List<ActiveEffectItem> { debuff },
                4);

            EffectGlanceBindings.Populate(root, glance);

            Assert.AreEqual("5", fab!.Q<Label>("effects-fab-count")!.text);
            Assert.IsTrue(fab.ClassListContains("effects-fab--has-count"));
            Assert.IsTrue(fab.ClassListContains("effects-fab--debuff"));
            StringAssert.Contains("5 active effects", fab.tooltip);
            StringAssert.Contains("Curse", fab.tooltip);
            StringAssert.Contains("+4 more", fab.tooltip);
        }

        [Test]
        public void Populate_PendingChip_AppliesPendingPolarity()
        {
            var root = InstantiateOverlay();
            var fab = root.Q<Button>("effects-fab");
            var pending = new ActiveEffectItem(
                "fate-1",
                "Fate pending",
                "Resolves soon.",
                new ActiveEffectBadge("pending", ActiveEffectBadgeTone.Pending));
            var glance = new EffectGlanceSnapshot(
                1,
                new List<ActiveEffectItem> { pending },
                0);

            EffectGlanceBindings.Populate(root, glance);

            Assert.IsTrue(fab!.ClassListContains("effects-fab--pending"));
            Assert.IsFalse(fab.ClassListContains("effects-fab--debuff"));
        }

        [Test]
        public void Populate_CountWithoutChips_UsesNeutralPolarity()
        {
            var root = InstantiateOverlay();
            var fab = root.Q<Button>("effects-fab");
            var glance = new EffectGlanceSnapshot(2, new List<ActiveEffectItem>(), 0);

            EffectGlanceBindings.Populate(root, glance);

            Assert.AreEqual("2", fab!.Q<Label>("effects-fab-count")!.text);
            Assert.IsTrue(fab.ClassListContains("effects-fab--neutral"));
            StringAssert.Contains("2 active effects", fab.tooltip);
        }

        static VisualElement InstantiateOverlay()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OverlayPath);
            Assert.IsNotNull(asset, $"Missing UXML at {OverlayPath}");
            var tree = asset!.Instantiate();
            var overlay = tree.Q<VisualElement>("player-inventory-overlay");
            Assert.IsNotNull(overlay);
            return overlay!;
        }
    }
}
