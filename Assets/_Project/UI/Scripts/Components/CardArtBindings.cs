using Kismeta.Core.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Loads card art from Resources/CardArt/{definitionId} with icon fallback.</summary>
    public static class CardArtBindings
    {
        public static void Apply(VisualElement artHost, Label? fallbackIcon, CardDefinition def)
        {
            if (artHost == null) return;

            var sprite = Resources.Load<Sprite>($"CardArt/{def.Id}");
            if (sprite != null)
            {
                artHost.style.backgroundImage = new StyleBackground(sprite);
                artHost.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                if (fallbackIcon != null)
                    fallbackIcon.style.display = DisplayStyle.None;
            }
            else
            {
                artHost.style.backgroundImage = StyleKeyword.None;
                if (fallbackIcon != null)
                    fallbackIcon.style.display = DisplayStyle.Flex;
            }
        }
    }
}
