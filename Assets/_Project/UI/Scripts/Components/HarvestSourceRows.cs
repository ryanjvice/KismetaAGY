using System.Collections.Generic;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Builds Spring harvest source rows with aspect color classes.</summary>
    public static class HarvestSourceRows
    {
        public static void Populate(VisualElement? listHost, IReadOnlyList<HarvestSourceRow> rows)
        {
            if (listHost == null) return;
            listHost.Clear();

            foreach (var row in rows)
                listHost.Add(BuildRow(row));
        }

        static VisualElement BuildRow(HarvestSourceRow row)
        {
            var container = new VisualElement();
            container.AddToClassList("harvest-source-row");

            var textCol = new VisualElement();
            textCol.AddToClassList("harvest-source-row__text");

            var title = new Label(row.Title);
            title.AddToClassList("harvest-source-row__title");
            textCol.Add(title);

            var subtitle = new Label(row.Subtitle);
            subtitle.AddToClassList("harvest-source-row__subtitle");
            textCol.Add(subtitle);

            container.Add(textCol);

            var pts = new Label(row.ShowDash ? "—" : $"+{row.Points}");
            pts.AddToClassList("harvest-source-row__pts");
            pts.AddToClassList(ClassForTier(row.Tier));
            container.Add(pts);

            if (row.Title == "Agekeeper's Boon")
                container.AddToClassList("harvest-source-row--boon");

            return container;
        }

        static string ClassForTier(HarvestAspectTier tier) => tier switch
        {
            HarvestAspectTier.Sign => "tally__pts--sign",
            HarvestAspectTier.Planet => "tally__pts--planet",
            HarvestAspectTier.Element => "tally__pts--element",
            _ => "harvest-source-row__pts--neutral"
        };
    }
}
