using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Rival lists, spread pools, legality checks, and cauldron/ward readouts for contests.</summary>
    public static class ContestBindings
    {
        public static string RivalName(GameSession session, int playerId) =>
            CeremonyBindings.PlayerName(session, playerId);

        public static void SetContestTitle(VisualElement root, string prefix, int rivalId, GameSession session)
        {
            var title = root.Q<Label>("contest-title");
            if (title == null) return;
            string name = rivalId >= 0 ? RivalName(session, rivalId) : "rival";
            title.text = $"{prefix} · {name}";
        }

        public static int TotalReagents(PlayerState player)
        {
            int total = 0;
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
                total += player.GetReagent(rt);
            return total;
        }

        public static void BuildRivalChips(VisualElement? host, GameSession session, int localId,
            Func<PlayerState, bool> eligible, int selectedId, Action<int> onSelect)
        {
            if (host == null) return;
            host.Clear();

            for (int i = 0; i < session.Players.Count; i++)
            {
                if (i == localId) continue;
                var rival = session.Players[i];
                if (!eligible(rival)) continue;

                bool selected = i == selectedId;
                var chip = new Button { text = RivalName(session, i) };
                chip.AddToClassList("btn");
                if (selected)
                {
                    chip.AddToClassList("btn--primary");
                    chip.SetEnabled(false);
                }
                else
                {
                    int rivalIndex = i;
                    chip.clicked += () => onSelect(rivalIndex);
                }

                host.Add(chip);
            }
        }

        public static void BuildCardChips(VisualElement? host, GameSession session,
            IReadOnlyList<string> cardIds, HashSet<string> selected, bool multiSelect, Action<string> onToggle)
        {
            if (host == null) return;
            host.Clear();

            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            foreach (var cardId in cardIds)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool isSelected = selected.Contains(cardId);
                var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: isSelected);
                chip.userData = cardId;
                chip.RegisterCallback<ClickEvent>(_ =>
                {
                    if (!multiSelect)
                    {
                        selected.Clear();
                        selected.Add(cardId);
                    }
                    else if (selected.Contains(cardId))
                        selected.Remove(cardId);
                    else
                        selected.Add(cardId);
                    onToggle(cardId);
                });
                host.Add(chip);
            }
        }

        public static List<string> MinorSpreadCards(GameSession session, PlayerState player)
        {
            var list = new List<string>();
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id))
                    list.Add(id);
            return list;
        }

        public static List<string> PublicRivalSpread(GameSession session, int rivalId)
        {
            if (rivalId < 0 || rivalId >= session.Players.Count)
                return new List<string>();
            return MinorSpreadCards(session, session.Players[rivalId]);
        }

        public static List<string> GambitStakeCards(GameSession session, int playerId)
        {
            var list = new List<string>();
            if (playerId < 0 || playerId >= session.Players.Count)
                return list;

            var player = session.Players[playerId];
            foreach (var slot in player.CrucibleSlots)
            {
                if (slot.State == CrucibleCardState.Active && !string.IsNullOrEmpty(slot.CardInstanceId))
                    list.Add(slot.CardInstanceId);
            }
            foreach (var id in player.Arcanum)
                list.Add(id);
            return list;
        }

        public static bool CanTargetForOpposition(GameSession session, int attackerId, int defenderId)
        {
            if (attackerId == defenderId) return false;
            if (defenderId < 0 || defenderId >= session.Players.Count) return false;

            var defender = session.Players[defenderId];
            if (defender.StoneState != StoneState.Forging)
                return false;

            var firedThisAge = defender.CrucibleSlots.Find(
                s => s.State == CrucibleCardState.Fired && s.FiredAtRound == session.Board.RoundNumber);
            return firedThisAge == null;
        }

        public static bool RivalEligibleForSummerContest(PlayerState rival) => true;

        public static int FirstEligibleRival(GameSession session, int localId, Func<PlayerState, bool> eligible)
        {
            for (int i = 0; i < session.Players.Count; i++)
            {
                if (i == localId) continue;
                if (eligible(session.Players[i])) return i;
            }
            return -1;
        }

        public static bool ArePlayersAlignedForTrade(GameSession session, int traderId, int rivalId) =>
            PlayerAspectAlignment.ArePlayersAlignedForTrade(session, traderId, rivalId);

        public static bool IsTradeRatioValid(GameSession session, int traderId, int rivalId,
            int offerCount, int requestCount) =>
            PlayerAspectAlignment.IsMagnusTradeRatioValid(session, traderId, rivalId, offerCount, requestCount);

        public static string TradeRatioHint(GameSession session, int traderId, int rivalId)
        {
            if (rivalId < 0)
                return "Magnus mode: misaligned trades cost 2:1. Aligned players trade 1:1.";

            string rival = RivalName(session, rivalId);
            if (ArePlayersAlignedForTrade(session, traderId, rivalId))
                return $"You and {rival} share a zodiac aspect — trades are 1:1.";
            return $"You and {rival} are misaligned — offer 2 cards for every 1 you receive.";
        }
    }
}
