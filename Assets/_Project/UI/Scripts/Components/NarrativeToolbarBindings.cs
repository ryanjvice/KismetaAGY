using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.UI;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class NarrativeToolbarBindings
    {
        static readonly HashSet<Button> s_introRecapWired = new();

        public static void WireIntroRecap(
            VisualElement? root,
            Season season,
            Func<SeasonIntroRecapHost?>? resolveHost)
        {
            if (root == null || resolveHost == null)
                return;

            var btn = root.Q<Button>("info-btn");
            if (btn == null || !s_introRecapWired.Add(btn))
                return;

            btn.clicked += () => resolveHost()?.Show(season);
        }
    }
}
