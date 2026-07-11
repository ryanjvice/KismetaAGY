using System.Collections.Generic;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Populates the central-panel inspect modal for the Spring hub zodiac board.</summary>
    public static class SpringBoardInspectBindings
    {
        const string ContentName = "central-panel-inspect-content";

        public static void Bind(VisualElement? screenRoot, GameSession? session)
        {
            var content = screenRoot?.Q<VisualElement>(ContentName);
            if (content == null || session == null) return;

            content.Clear();

            content.Add(BuildCosmicAgeSection(session));
            content.Add(BuildPlayersSection(session));
            content.Add(BuildAstralHousesSection(session));
        }

        static VisualElement BuildCosmicAgeSection(GameSession session)
        {
            var section = MakeSection("Cosmic Age");
            var body = section.Q(className: "central-panel-inspect-section__body")!;

            var featuredHost = new VisualElement { name = "cosmic-age-featured" };
            featuredHost.AddToClassList("active-effects-featured");
            ActiveEffectsRows.PopulateFeatured(featuredHost, BuildCosmicAgeItem(session));
            body.Add(featuredHost);

            return section;
        }

        static ActiveEffectItem BuildCosmicAgeItem(GameSession session)
        {
            var cosmic = session.Board.CosmicAgeSign;
            var planet = Correspondence.PlanetFor(cosmic);
            var element = Correspondence.ElementFor(cosmic);
            var (name, desc) = CosmicEffectDescriber.DescribeCosmicAge(cosmic);
            int keeperId = CeremonyBindings.FindAgekeeperId(session);
            string keeperName = CeremonyBindings.PlayerName(session, keeperId);

            string body = cosmic == ZodiacSign.None
                ? "The age has not been cast yet."
                : $"{name} — {desc} while {cosmic} reigns.";

            if (session.Board.ContestEffects.DuelBestOfThree
                || session.Board.ContestEffects.GambitBestOfThree)
                body += " Duels and Gambits resolve as best-of-three this age (Justice).";

            string footer = cosmic == ZodiacSign.None
                ? string.Empty
                : $"{planet} · {element} · rolled by {keeperName} (Agekeeper)";

            return new ActiveEffectItem(
                "cosmic-age",
                name,
                body,
                new ActiveEffectBadge("expires at age end", ActiveEffectBadgeTone.Active),
                iconKey: "cosmic-age",
                footer: footer);
        }

        static VisualElement BuildPlayersSection(GameSession session)
        {
            var section = MakeSection("Players on the wheel");
            var body = section.Q(className: "central-panel-inspect-section__body")!;

            foreach (var player in session.Players)
            {
                var view = PublicPlayerView.From(player);
                body.Add(BuildPlayerRow(view));
            }

            return section;
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

            var signRow = new VisualElement();
            signRow.AddToClassList("central-panel-inspect-player-row__sign-row");

            if (player.CurrentSign == ZodiacSign.None)
            {
                signRow.Add(new Label("—") { name = "detail-empty" });
            }
            else
            {
                signRow.Add(SymbolGlyphs.CreateZodiacLabel(
                    SymbolGlyphs.Zodiac(player.CurrentSign),
                    "detail-zodiac__glyph"));
                var signName = new Label(player.CurrentSign.ToString());
                signName.AddToClassList("detail-zodiac__name");
                signRow.Add(signName);

                var meta = new Label(
                    $"{Correspondence.PlanetFor(player.CurrentSign)} · {Correspondence.ElementFor(player.CurrentSign)}");
                meta.AddToClassList("central-panel-inspect-player-row__meta");
                signRow.Add(meta);
            }

            info.Add(signRow);
            row.Add(info);
            return row;
        }

        static VisualElement BuildAstralHousesSection(GameSession session)
        {
            var section = MakeSection("Astral houses on the board");
            var body = section.Q(className: "central-panel-inspect-section__body")!;

            var houses = new List<(ZodiacSign Sign, int PlayerId)>();
            foreach (var player in session.Players)
            {
                foreach (var sign in player.AstralHouses)
                    houses.Add((sign, player.PlayerId));
            }

            if (houses.Count == 0)
            {
                var empty = new Label("No astral houses built yet.");
                empty.AddToClassList("central-panel-inspect-empty");
                body.Add(empty);
                return section;
            }

            foreach (var (sign, playerId) in houses.OrderBy(h => (int)h.Sign))
                body.Add(BuildHouseRow(session, sign, playerId));

            return section;
        }

        static VisualElement BuildHouseRow(GameSession session, ZodiacSign sign, int playerId)
        {
            var cosmic = session.Board.CosmicAgeSign;
            var row = new VisualElement();
            row.AddToClassList("central-panel-inspect-house-row");

            var chip = MakeHouseChip(sign);
            chip.AddToClassList("central-panel-inspect-house-row__chip");
            row.Add(chip);

            var info = new VisualElement();
            info.AddToClassList("central-panel-inspect-house-row__info");

            var ownerRow = new VisualElement();
            ownerRow.AddToClassList("central-panel-inspect-house-row__owner");

            var dot = new VisualElement();
            dot.AddToClassList("central-panel-inspect-player-row__dot");
            dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(playerId));
            ownerRow.Add(dot);

            var ownerName = new Label($"{PlayerUiNames.ForPlayer(playerId)} · {sign}");
            ownerName.AddToClassList("central-panel-inspect-house-row__owner-name");
            ownerRow.Add(ownerName);

            int alignPts = HarvestBreakdownService.AlignmentBonus(sign, cosmic);
            if (alignPts > 0)
            {
                var alignChip = new Label($"+{alignPts} alignment");
                alignChip.AddToClassList("active-effects-badge");
                alignChip.AddToClassList("active-effects-badge--aligned");
                alignChip.style.marginLeft = 6;
                ownerRow.Add(alignChip);
            }

            info.Add(ownerRow);

            string description = CosmicEffectDescriber.DescribeAlignmentContribution(sign, cosmic);
            if (cosmic != sign)
            {
                string personal = CosmicEffectDescriber.DescribePersonalEffect(sign);
                if (!string.IsNullOrEmpty(personal))
                    description = $"{description} {personal}".Trim();
            }

            var desc = new Label(description);
            desc.AddToClassList("central-panel-inspect-house-row__desc");
            info.Add(desc);

            row.Add(info);
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

        static VisualElement MakeHouseChip(ZodiacSign sign)
        {
            var chip = new VisualElement();
            chip.AddToClassList("house-chip");
            chip.Add(SymbolGlyphs.CreateZodiacLabel(SymbolGlyphs.Zodiac(sign), "house-chip__glyph"));
            return chip;
        }
    }
}
