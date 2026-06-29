using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Populates the central-panel inspect modal for Autumn forge screens.</summary>
    public static class AutumnForgeInspectBindings
    {
        const string ContentName = "central-panel-inspect-content";

        static readonly (Suit suit, string label)[] CauldronSuits =
        {
            (Suit.Wands, "Wands"),
            (Suit.Cups, "Cups"),
            (Suit.Pentacles, "Pentacles"),
            (Suit.Swords, "Swords"),
        };

        public static void Bind(VisualElement? screenRoot, GameSession? session)
        {
            var content = screenRoot?.Q<VisualElement>(ContentName);
            if (content == null || session == null) return;

            content.Clear();

            var section = MakeSection("Players at the forge");
            var body = section.Q(className: "central-panel-inspect-section__body")!;

            foreach (var player in session.Players)
                body.Add(BuildPlayerRow(PublicPlayerView.From(player)));

            content.Add(section);
        }

        static VisualElement BuildPlayerRow(PublicPlayerView player)
        {
            var row = new VisualElement();
            row.AddToClassList("central-panel-inspect-player-row");

            var dot = new VisualElement();
            dot.AddToClassList("central-panel-inspect-player-row__dot");
            dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(player.PlayerId));
            row.Add(dot);

            var info = new VisualElement();
            info.AddToClassList("central-panel-inspect-player-row__info");

            var name = new Label(PlayerUiNames.ForPlayer(player.PlayerId));
            name.AddToClassList("central-panel-inspect-player-row__name");
            info.Add(name);

            bool inStasis = player.StoneState == StoneState.Stasis;
            var stone = new Label(StoneLine(player));
            stone.AddToClassList("central-panel-inspect-player-row__stone");
            if (inStasis)
                stone.AddToClassList("central-panel-inspect-player-row__stone--stasis");
            info.Add(stone);

            if (inStasis)
            {
                var note = new Label("cannot be opposed this age");
                note.AddToClassList("central-panel-inspect-player-row__stasis-note");
                info.Add(note);
            }

            info.Add(BuildCauldronRow(player));
            row.Add(info);
            return row;
        }

        static string StoneLine(PublicPlayerView player)
        {
            if (player.StoneState == StoneState.Stasis)
                return $"In stasis — halted at {player.StonePosition}";

            return $"{player.StonePosition} ({player.StonePosition.Value}) · {player.StoneState}";
        }

        static VisualElement BuildCauldronRow(PublicPlayerView player)
        {
            var row = new VisualElement();
            row.AddToClassList("central-panel-inspect-cauldron-row");

            bool anyLit = false;
            foreach (var (suit, label) in CauldronSuits)
            {
                bool lit = player.IsCauldronLit(suit);
                anyLit |= lit;

                var chip = new Label(label);
                chip.AddToClassList("central-panel-inspect-cauldron-chip");
                chip.EnableInClassList("central-panel-inspect-cauldron-chip--lit", lit);
                chip.EnableInClassList("central-panel-inspect-cauldron-chip--dormant", !lit);
                row.Add(chip);
            }

            if (!anyLit)
            {
                row.Clear();
                var empty = new Label("None lit");
                empty.AddToClassList("central-panel-inspect-empty");
                row.Add(empty);
            }

            return row;
        }

        static VisualElement MakeSection(string eyebrowText)
        {
            var section = new VisualElement();
            section.AddToClassList("central-panel-inspect-section");

            var eyebrow = new Label(eyebrowText);
            eyebrow.AddToClassList("eyebrow");
            section.Add(eyebrow);

            var body = new VisualElement();
            body.AddToClassList("central-panel-inspect-section__body");
            section.Add(body);

            return section;
        }
    }
}
