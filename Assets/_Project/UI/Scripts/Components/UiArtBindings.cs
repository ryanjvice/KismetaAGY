using Kismeta.Core.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Applies board-game art from <see cref="UiArtCatalog"/> to UI Toolkit elements.</summary>
    public static class UiArtBindings
    {
        const string CatalogResourcePath = "UiArtCatalog";
        const float StarChartOpacity = 0.15f;

        static UiArtCatalog? _catalog;

        public static UiArtCatalog? Catalog => ResolveCatalog();

        public static void ApplyBackground(
            VisualElement? host,
            Sprite? sprite,
            BackgroundSizeType size = BackgroundSizeType.Contain,
            float opacity = 1f)
        {
            if (host == null || sprite == null)
                return;

            host.style.backgroundImage = new StyleBackground(sprite);
            host.style.backgroundSize = new BackgroundSize(size);
            if (opacity < 1f)
                host.style.opacity = opacity;
        }

        public static void ApplyTitleHero(VisualElement? heroRoot)
        {
            var catalog = ResolveCatalog();
            if (heroRoot == null || catalog == null) return;

            ApplyBackground(heroRoot.Q("title-hero-bg"), catalog.HeroBurgundy, BackgroundSizeType.Cover);
            ApplyBackground(heroRoot.Q("title-wheel-star"), catalog.StarChart, BackgroundSizeType.Contain, StarChartOpacity);
            ApplyBackground(heroRoot.Q("title-wheel-zodiac"), catalog.ZodiacWheel);
            ApplyBackground(heroRoot.Q("title-wheel-mantle"), catalog.MantleRing);
            ApplyBackground(heroRoot.Q("title-wheel-forge"), catalog.CrucibleForge);
            ApplyBackground(heroRoot.Q("title-wordmark-img"), catalog.KismetaMetallic);
            ApplyBackground(heroRoot.Q("title-tagline-img"), catalog.AlchemistsMetallic);
        }

        public static void ApplyWheelStack(VisualElement? stackRoot)
        {
            var catalog = ResolveCatalog();
            if (stackRoot == null || catalog == null) return;

            ApplyBackground(stackRoot.Q("wheel-star-chart"), catalog.StarChart, BackgroundSizeType.Contain, StarChartOpacity);
            ApplyBackground(stackRoot.Q("wheel-zodiac"), catalog.ZodiacWheel, BackgroundSizeType.Contain);
        }

        public static void ApplyCauldronHubDecor(VisualElement? hubRoot)
        {
            var catalog = ResolveCatalog();
            if (hubRoot == null || catalog == null) return;

            ApplyBackground(hubRoot.Q("mantle-ring-backdrop"), catalog.MantleRing, BackgroundSizeType.Contain);

            foreach (var (id, suit) in CauldronSlotIds)
            {
                var el = hubRoot.Q<VisualElement>(id);
                if (el == null) continue;
                ApplyBackground(el.Q($"{id}-art"), catalog.CauldronFor(suit), BackgroundSizeType.Contain);
            }
        }

        public static void ApplyForgeStageDecor(VisualElement? forgeStage)
        {
            var catalog = ResolveCatalog();
            if (forgeStage == null || catalog == null) return;

            ApplyBackground(forgeStage.Q("forge-mantle-ring"), catalog.MantleRing, BackgroundSizeType.Contain);
        }

        public static void ApplyWinterSeal(VisualElement? stageRoot)
        {
            var catalog = ResolveCatalog();
            if (stageRoot == null || catalog == null) return;

            ApplyBackground(stageRoot.Q("winter-seal"), catalog.CrucibleForge, BackgroundSizeType.Contain);
        }

        public static void ApplyCeremonyStarfield(VisualElement? heroRoot)
        {
            var catalog = ResolveCatalog();
            if (heroRoot == null || catalog == null) return;

            ApplyBackground(heroRoot.Q("ceremony-starfield"), catalog.StarChart, BackgroundSizeType.Cover, 0.2f);
        }

        public static void ApplyIntroSigil(VisualElement? sigilHost)
        {
            var catalog = ResolveCatalog();
            if (sigilHost == null || catalog == null) return;

            ApplyBackground(sigilHost.Q("intro-sigil-art"), catalog.CrucibleForge, BackgroundSizeType.Contain);
        }

        public static void ApplyVictoryHalo(VisualElement? heroRoot)
        {
            var catalog = ResolveCatalog();
            if (heroRoot == null || catalog == null) return;

            ApplyBackground(heroRoot.Q("victory-halo"), catalog.CrucibleForge, BackgroundSizeType.Contain, 0.35f);
        }

        static readonly (string id, Suit suit)[] CauldronSlotIds =
        {
            ("cauldron-n", Suit.Wands),
            ("cauldron-e", Suit.Cups),
            ("cauldron-s", Suit.Pentacles),
            ("cauldron-w", Suit.Swords),
        };

        static UiArtCatalog? ResolveCatalog()
        {
            if (_catalog == null)
                _catalog = Resources.Load<UiArtCatalog>(CatalogResourcePath);
            return _catalog;
        }
    }
}
