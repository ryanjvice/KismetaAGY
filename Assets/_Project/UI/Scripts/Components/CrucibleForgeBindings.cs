using Kismeta.Core.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Applies crucible forge progress art to the Autumn board-stage host.</summary>
    public static class CrucibleForgeBindings
    {
        const string CatalogResourcePath = "CrucibleForgeArt";

        static CrucibleForgeArt? _catalog;

        public static void Apply(
            VisualElement? artHost,
            Label? statusLabel,
            StonePosition position,
            string statusText)
        {
            if (statusLabel != null)
                statusLabel.text = statusText;

            if (artHost == null)
                return;

            var sprite = ResolveCatalog()?.Get(position);
            if (sprite != null)
            {
                artHost.style.backgroundImage = new StyleBackground(sprite);
                artHost.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                artHost.style.display = DisplayStyle.Flex;
            }
            else
            {
                artHost.style.backgroundImage = StyleKeyword.None;
                artHost.style.display = DisplayStyle.None;
            }
        }

        static CrucibleForgeArt? ResolveCatalog()
        {
            if (_catalog == null)
                _catalog = Resources.Load<CrucibleForgeArt>(CatalogResourcePath);
            return _catalog;
        }
    }
}
