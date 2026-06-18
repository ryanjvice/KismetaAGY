using UnityEngine.UIElements;
using Kismeta.Core.Domain;
using Kismeta.Core.Rules;

namespace Kismeta.UI.Components
{
    /// <summary>Factory for compact card chips used in spreads, docks, and trays.</summary>
    public static class CardChipFactory
    {
        public static VisualElement Create(string rankLabel, Suit suit, bool selected = false, bool aligned = false)
        {
            var chip = new VisualElement();
            chip.AddToClassList("card-chip");
            chip.AddToClassList(SuitClass(suit));
            if (selected || aligned)
                chip.AddToClassList("card-chip--selected");

            var rank = new Label(rankLabel);
            rank.AddToClassList("card-chip__rank");
            chip.Add(rank);
            return chip;
        }

        public static VisualElement CreateFromDefinition(string rankLabel, string definitionId,
            ICardDatabase? db, bool selected = false)
        {
            var suit = Suit.Wands;
            if (db != null)
            {
                var def = db.GetById(definitionId);
                if (def != null)
                    suit = def.Suit;
            }
            return Create(rankLabel, suit, selected);
        }

        static string SuitClass(Suit suit) => suit switch
        {
            Suit.Cups => "card-chip--cups",
            Suit.Pentacles => "card-chip--pentacles",
            Suit.Swords => "card-chip--swords",
            _ => "card-chip--wands"
        };
    }
}
