using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class ExchangeBindings
    {
        public static string TitleFor(ExchangeKind kind) => kind switch
        {
            ExchangeKind.Trade => "Trade complete",
            ExchangeKind.Duel => "Duel resolved",
            ExchangeKind.Gambit => "Gambit resolved",
            ExchangeKind.FateLovers => "The Lovers",
            ExchangeKind.FateFool => "The Fool",
            ExchangeKind.FateHangedMan => "The Hanged Man",
            _ => "Resources exchanged"
        };

        public static void Populate(
            VisualElement root,
            GameSession session,
            PlayerExchangeEvent exchange)
        {
            var title = root.Q<Label>("exchange-title");
            if (title != null)
                title.text = TitleFor(exchange.Kind);

            var subtitle = root.Q<Label>("exchange-subtitle");
            if (subtitle != null)
            {
                bool hasContext = !string.IsNullOrWhiteSpace(exchange.ContextLine);
                subtitle.text = exchange.ContextLine ?? "";
                subtitle.style.display = hasContext ? DisplayStyle.Flex : DisplayStyle.None;
                bool isCombatVerdict = hasContext
                    && (exchange.Kind == ExchangeKind.Duel || exchange.Kind == ExchangeKind.Gambit);
                subtitle.EnableInClassList("exchange__subtitle--verdict", isCombatVerdict);
            }

            var bilateral = root.Q("bilateral-panel");
            var legsList = root.Q("legs-list");

            if (exchange.Kind == ExchangeKind.FateHangedMan)
            {
                if (bilateral != null)
                    bilateral.style.display = DisplayStyle.None;
                if (legsList != null)
                {
                    legsList.style.display = DisplayStyle.Flex;
                    PopulateLegsList(legsList, session, exchange.Legs);
                }
                return;
            }

            if (bilateral != null)
                bilateral.style.display = DisplayStyle.Flex;
            if (legsList != null)
                legsList.style.display = DisplayStyle.None;

            if (exchange.Kind == ExchangeKind.Duel || exchange.Kind == ExchangeKind.Gambit)
                PopulateCombatTrays(root, session, exchange);
            else
                ResolveBilateralTrays(root, session, exchange);
        }

        static void PopulateCombatTrays(VisualElement root, GameSession session, PlayerExchangeEvent exchange)
        {
            ExchangeLeg? playerTransfer = null;
            ExchangeLeg? toSink = null;
            var selfLegs = new List<ExchangeLeg>();

            foreach (var leg in exchange.Legs)
            {
                if (leg.FromPlayerId >= 0 && leg.ToPlayerId >= 0 && leg.FromPlayerId != leg.ToPlayerId)
                    playerTransfer = leg;
                else if (leg.ToPlayerId < 0)
                    toSink = leg;
                else if (leg.FromPlayerId >= 0 && leg.FromPlayerId == leg.ToPlayerId)
                    selfLegs.Add(leg);
            }

            if (playerTransfer != null)
            {
                var items = new List<ExchangeItem>(playerTransfer.Items);
                SetTray(root, "party-a-label", "party-a-items", session, playerTransfer.FromPlayerId, items,
                    $"{ContestBindings.RivalName(session, playerTransfer.FromPlayerId)} loses");
                SetTray(root, "party-b-label", "party-b-items", session, playerTransfer.ToPlayerId, items,
                    $"{ContestBindings.RivalName(session, playerTransfer.ToPlayerId)} wins");
                return;
            }

            if (toSink != null)
            {
                var lostItems = new List<ExchangeItem>(toSink.Items);
                SetTray(root, "party-a-label", "party-a-items", session, toSink.FromPlayerId, lostItems,
                    $"{ContestBindings.RivalName(session, toSink.FromPlayerId)} loses");

                var keptLeg = selfLegs.Count > 0
                    ? selfLegs.Find(l => l.FromPlayerId != toSink.FromPlayerId)
                    : null;
                if (keptLeg != null)
                {
                    var keptItems = new List<ExchangeItem>(keptLeg.Items);
                    SetTray(root, "party-b-label", "party-b-items", session, keptLeg.FromPlayerId, keptItems,
                        $"{ContestBindings.RivalName(session, keptLeg.FromPlayerId)} wins");
                }
                else
                {
                    SetTray(root, "party-b-label", "party-b-items", session, -1, lostItems,
                        "to common deck", isSink: true);
                }
                return;
            }

            if (selfLegs.Count >= 2)
            {
                var loseLeg = selfLegs[0];
                var winLeg = selfLegs[1];
                SetTray(root, "party-a-label", "party-a-items", session, loseLeg.FromPlayerId,
                    new List<ExchangeItem>(loseLeg.Items),
                    $"{ContestBindings.RivalName(session, loseLeg.FromPlayerId)} loses");
                SetTray(root, "party-b-label", "party-b-items", session, winLeg.FromPlayerId,
                    new List<ExchangeItem>(winLeg.Items),
                    $"{ContestBindings.RivalName(session, winLeg.FromPlayerId)} wins");
                return;
            }

            if (selfLegs.Count == 1)
            {
                var leg = selfLegs[0];
                var items = new List<ExchangeItem>(leg.Items);
                SetTray(root, "party-a-label", "party-a-items", session, leg.FromPlayerId, items,
                    $"{ContestBindings.RivalName(session, leg.FromPlayerId)} loses");
                SetTray(root, "party-b-label", "party-b-items", session, leg.FromPlayerId, items,
                    "contest resolved");
            }
        }

        static void ResolveBilateralTrays(VisualElement root, GameSession session, PlayerExchangeEvent exchange)
        {
            int playerA = -1;
            int playerB = -1;
            foreach (var leg in exchange.Legs)
            {
                if (leg.FromPlayerId >= 0)
                    playerA = leg.FromPlayerId;
                if (leg.ToPlayerId >= 0 && leg.ToPlayerId != leg.FromPlayerId)
                    playerB = leg.ToPlayerId;
                if (playerA >= 0 && playerB >= 0 && playerA != playerB)
                    break;
            }

            if (playerA < 0 && exchange.Legs.Count > 0)
                playerA = exchange.Legs[0].FromPlayerId;
            if (playerB < 0 && exchange.Legs.Count > 0)
                playerB = exchange.Legs[0].ToPlayerId;

            var itemsA = new List<ExchangeItem>();
            var itemsB = new List<ExchangeItem>();

            foreach (var leg in exchange.Legs)
            {
                if (leg.FromPlayerId == playerA && (leg.ToPlayerId == playerB || leg.ToPlayerId < 0))
                    itemsA.AddRange(leg.Items);
                else if (leg.FromPlayerId == playerB && (leg.ToPlayerId == playerA || leg.ToPlayerId < 0))
                    itemsB.AddRange(leg.Items);
                else if (leg.FromPlayerId == playerA && leg.ToPlayerId == playerA)
                    itemsA.AddRange(leg.Items);
                else if (leg.FromPlayerId == playerB && leg.ToPlayerId == playerB)
                    itemsB.AddRange(leg.Items);
                else if (leg.FromPlayerId == playerA)
                    itemsA.AddRange(leg.Items);
                else if (leg.FromPlayerId == playerB)
                    itemsB.AddRange(leg.Items);
            }

            SetTray(root, "party-a-label", "party-a-items", session, playerA, itemsA);
            SetTray(root, "party-b-label", "party-b-items", session, playerB, itemsB);
        }

        static void SetTray(
            VisualElement root,
            string labelName,
            string itemsName,
            GameSession session,
            int playerId,
            List<ExchangeItem> items,
            string? customLabel = null,
            bool isSink = false)
        {
            var label = root.Q<Label>(labelName);
            if (label != null)
            {
                if (!string.IsNullOrEmpty(customLabel))
                    label.text = customLabel;
                else if (playerId < 0 && items.Count > 0 && items[0].Kind == ExchangeItemKind.ToDiscard)
                    label.text = "to common deck";
                else if (playerId >= 0)
                    label.text = $"{ContestBindings.RivalName(session, playerId)} gives";
                else
                    label.text = "gives";

                label.EnableInClassList("exchange__tray-label--sink", isSink);
            }

            var host = root.Q(itemsName);
            if (host == null) return;
            host.Clear();
            PopulateItems(host, session, items);
        }

        static void PopulateLegsList(VisualElement host, GameSession session, IReadOnlyList<ExchangeLeg> legs)
        {
            host.Clear();
            foreach (var leg in legs)
            {
                var row = new VisualElement();
                row.AddToClassList("exchange__leg-row");

                var lbl = new Label($"{PlayerLabel(session, leg.FromPlayerId)} → {PlayerLabel(session, leg.ToPlayerId)}");
                lbl.AddToClassList("exchange__leg-label");
                row.Add(lbl);

                var items = new VisualElement();
                items.AddToClassList("tray__cards");
                items.AddToClassList("exchange__items");
                PopulateItems(items, session, leg.Items);
                row.Add(items);

                host.Add(row);
            }
        }

        static string PlayerLabel(GameSession session, int playerId)
        {
            if (playerId == PlayerExchangeEvent.SinkDiscard)
                return "common discard";
            if (playerId == PlayerExchangeEvent.SinkDeck)
                return "common deck";
            if (playerId < 0)
                return "supply";
            return ContestBindings.RivalName(session, playerId);
        }

        static void PopulateItems(VisualElement host, GameSession session, IReadOnlyList<ExchangeItem> items)
        {
            var db = session.Rules?.CardDatabase;
            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case ExchangeItemKind.Reagent:
                        host.Add(ReagentChipFactory.Create(item.ReagentType, item.Count));
                        break;

                    case ExchangeItemKind.SpreadCard:
                    case ExchangeItemKind.CrucibleCard:
                    case ExchangeItemKind.AdeptCard:
                    case ExchangeItemKind.ToDiscard:
                    case ExchangeItemKind.Arrested:
                        if (!string.IsNullOrEmpty(item.CardInstanceId))
                        {
                            var inst = session.GetCard(item.CardInstanceId);
                            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                            if (def != null)
                            {
                                var chip = CardChipFactory.CreateFromDefinition(def, instanceId: item.CardInstanceId);
                                if (item.Kind == ExchangeItemKind.Arrested)
                                {
                                    var badge = new Label("arrested");
                                    badge.AddToClassList("exchange__status-badge");
                                    chip.Add(badge);
                                }
                                host.Add(chip);
                            }
                        }
                        else if (item.Count > 1)
                        {
                            var more = new Label($"+{item.Count} cards");
                            more.AddToClassList("exchange__more-label");
                            host.Add(more);
                        }
                        break;
                }
            }
        }
    }
}
