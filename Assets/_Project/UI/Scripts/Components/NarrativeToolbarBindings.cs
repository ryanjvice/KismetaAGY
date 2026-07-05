using System;
using Kismeta.Core.Domain;
using Kismeta.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class NarrativeToolbarBindings
    {
        const string IntroRecapWiredKey = "intro-recap-wired";
        const string TableFabWiredKey = "table-fab-wired";

        static SeasonIntroRecapHost? s_host;

        public static void ConfigureIntroRecapHost(SeasonIntroRecapHost? host) => s_host = host;

        public static void WireTableFab(VisualElement? root, Action? onOpenCardTable)
        {
            if (root == null)
                return;

            var btn = root.Q<Button>("table-fab");
            if (btn == null || ReferenceEquals(btn.userData, TableFabWiredKey))
                return;

            btn.userData = TableFabWiredKey;
            btn.text = SymbolGlyphs.AlchemistsGlyph;
            btn.pickingMode = PickingMode.Position;
            if (onOpenCardTable != null)
                btn.clicked += () => onOpenCardTable();
        }

        public static void WireIntroRecap(
            VisualElement? root,
            Season season,
            Func<SeasonIntroRecapHost?>? resolveHost = null)
        {
            if (root == null)
                return;

            var btn = root.Q<Button>("info-btn");
            if (btn == null || ReferenceEquals(btn.userData, IntroRecapWiredKey))
                return;

            btn.userData = IntroRecapWiredKey;
            btn.text = SymbolGlyphs.SeasonRecapGlyph;
            btn.pickingMode = PickingMode.Position;
            btn.clicked += () =>
            {
                var host = s_host ?? resolveHost?.Invoke();
                if (host == null)
                {
                    Debug.LogWarning("[NarrativeToolbar] Info recap host is not configured.");
                    return;
                }

                host.Show(season);
            };
        }
    }
}
