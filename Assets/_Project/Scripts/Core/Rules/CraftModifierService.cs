using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>How payment cards must be composed for a craft action.</summary>
    public enum CraftPaymentMode
    {
        AnyCards,
        RequiredSuit,
        BuildSaltSuit,
        TemperanceWildSalt
    }

    public readonly struct CraftCostOption
    {
        public readonly int Cost;
        public readonly CraftPaymentMode Mode;
        public readonly Suit RequiredSuit;
        public readonly string Source;

        public CraftCostOption(int cost, CraftPaymentMode mode, Suit requiredSuit, string source)
        {
            Cost = cost;
            Mode = mode;
            RequiredSuit = requiredSuit;
            Source = source;
        }
    }

    /// <summary>
    /// Resolves effective craft costs from spread, cosmic, adept, and Empress overlays.
    /// Cost stacking order:
    /// 1. Base cost 3 (elemental) or Build-salt path 2 suit cards.
    /// 2. Cosmic/personal cheap (-1 to 2).
    /// 3. Spread rank 8 (-1, min 1).
    /// 4. Empress marked type (-1 to 2).
    /// 5. Temperance salt path (2 any) when crafting Salt.
    /// Lowest valid cost among applicable paths wins; payment validates against the chosen path.
    /// </summary>
    public static class CraftModifierService
    {
        public const int BaseElementalCost = 3;
        public const int BaseSaltCost = 3;
        public const int BuildSaltCost = 2;
        public const int CheapCost = 2;
        public const int EmpressMarkedCost = 2;
        public const int TemperanceSaltCost = 2;
        public const int MinElementalCost = 1;

        public static IReadOnlyList<CraftCostOption> GetCostOptions(
            GameSession session,
            int playerId,
            ReagentType reagentType)
        {
            var player = session.Players[playerId];
            var cos = session.Board.CosmicEffect;
            var personal = player.PersonalCosmicEffects;
            var options = new List<CraftCostOption>();

            if (reagentType == ReagentType.Salt)
            {
                options.Add(new CraftCostOption(BaseSaltCost, CraftPaymentMode.AnyCards, Suit.None, "base"));

                if (cos.SaltCostsTwo || personal.SaltCostsTwo)
                    options.Add(new CraftCostOption(CheapCost, CraftPaymentMode.AnyCards, Suit.None, "cosmic"));

                if (HasTemperance(session, player))
                    options.Add(new CraftCostOption(TemperanceSaltCost, CraftPaymentMode.AnyCards, Suit.None, "temperance"));

                foreach (var buildSuit in GetActiveBuildSaltSuits(session, player))
                {
                    options.Add(new CraftCostOption(
                        BuildSaltCost,
                        CraftPaymentMode.BuildSaltSuit,
                        buildSuit,
                        $"build-{buildSuit}"));
                }

                ApplyReversedCraftPenalty(session, playerId, options);
                return options;
            }

            var requiredSuit = Correspondence.SuitFor(reagentType);
            if (requiredSuit == Suit.None)
                return options;

            options.Add(new CraftCostOption(BaseElementalCost, CraftPaymentMode.RequiredSuit, requiredSuit, "base"));

            bool elemCheap = (cos.CheapCraftReagent == reagentType && cos.CheapCraftSuit != Suit.None)
                          || (personal.CheapCraftReagent == reagentType && personal.CheapCraftSuit != Suit.None);
            if (elemCheap)
                options.Add(new CraftCostOption(CheapCost, CraftPaymentMode.RequiredSuit, requiredSuit, "cosmic"));

            if (HasRank8Discount(session, player, reagentType))
            {
                options.Add(new CraftCostOption(
                    Math.Max(MinElementalCost, BaseElementalCost - 1),
                    CraftPaymentMode.RequiredSuit,
                    requiredSuit,
                    "rank8"));
            }

            if (player.EmpressMarkedReagents.Contains(reagentType))
            {
                options.Add(new CraftCostOption(
                    EmpressMarkedCost,
                    CraftPaymentMode.RequiredSuit,
                    requiredSuit,
                    "empress"));
            }

            if (HasTemperanceWild(session, player, reagentType))
            {
                options.Add(new CraftCostOption(
                    BaseElementalCost,
                    CraftPaymentMode.TemperanceWildSalt,
                    requiredSuit,
                    "temperance-wild"));
            }

            ApplyReversedCraftPenalty(session, playerId, options);
            return options;
        }

        static void ApplyReversedCraftPenalty(
            GameSession session,
            int playerId,
            List<CraftCostOption> options)
        {
            int extra = ReversedCurseService.CraftExtraCardCost(session, playerId);
            if (extra <= 0)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                options[i] = new CraftCostOption(
                    opt.Cost + extra,
                    opt.Mode,
                    opt.RequiredSuit,
                    opt.Source);
            }
        }

        public static CraftCostOption? FindMatchingOption(
            GameSession session,
            int playerId,
            ReagentType reagentType,
            IReadOnlyList<string> cardInstanceIds,
            ICardDatabase db)
        {
            var options = GetCostOptions(session, playerId, reagentType);
            CraftCostOption? best = null;

            foreach (var option in options)
            {
                if (cardInstanceIds.Count != option.Cost)
                    continue;
                if (!ValidatePayment(session, playerId, reagentType, cardInstanceIds, option, db))
                    continue;

                if (!best.HasValue || option.Cost < best.Value.Cost)
                    best = option;
            }

            return best;
        }

        public static int GetMinimumCost(GameSession session, int playerId, ReagentType reagentType)
        {
            int min = reagentType == ReagentType.Salt ? BaseSaltCost : BaseElementalCost;
            foreach (var option in GetCostOptions(session, playerId, reagentType))
                min = Math.Min(min, option.Cost);
            return min;
        }

        public static bool ValidatePayment(
            GameSession session,
            int playerId,
            ReagentType reagentType,
            IReadOnlyList<string> cardInstanceIds,
            CraftCostOption option,
            ICardDatabase db)
        {
            if (cardInstanceIds.Count != option.Cost)
                return false;

            var player = session.Players[playerId];
            var cos = session.Board.CosmicEffect;
            var personal = player.PersonalCosmicEffects;
            var activeWild = cos.WildCourtSuit != Suit.None ? cos.WildCourtSuit : personal.WildCourtSuit;

            switch (option.Mode)
            {
                case CraftPaymentMode.AnyCards:
                    return AllOwnedMinorCards(session, player, cardInstanceIds, db);

                case CraftPaymentMode.BuildSaltSuit:
                    return AllMatchSuit(session, player, cardInstanceIds, option.RequiredSuit, activeWild, db);

                case CraftPaymentMode.RequiredSuit:
                    return AllMatchSuit(session, player, cardInstanceIds, option.RequiredSuit, activeWild, db);

                case CraftPaymentMode.TemperanceWildSalt:
                    return AllMatchSuitOrSaltWild(session, player, reagentType, cardInstanceIds, option.RequiredSuit, activeWild, db);

                default:
                    return false;
            }
        }

        public static bool HasTemperance(GameSession session, PlayerState player)
            => FindAdept(session, player, 14) != null;

        public static bool HasEmpress(GameSession session, PlayerState player)
            => FindAdept(session, player, 3) != null;

        public static bool IsEmpressResonant(GameSession session, PlayerState player)
        {
            var empress = FindAdeptDef(session, player, 3);
            return empress != null && AdeptAttunement.IsResonant(player, empress);
        }

        public static bool IsTemperanceResonant(GameSession session, PlayerState player)
        {
            var temperance = FindAdeptDef(session, player, 14);
            return temperance != null && AdeptAttunement.IsResonant(player, temperance);
        }

        public static int EmpressMarkLimit(GameSession session, PlayerState player)
            => IsEmpressResonant(session, player) ? 2 : 1;

        static bool HasRank8Discount(GameSession session, PlayerState player, ReagentType reagent)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return false;

            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null && SpreadCraftEffectCatalog.IsRank8CraftDiscount(def, reagent))
                    return true;
            }

            return false;
        }

        static HashSet<Suit> GetActiveBuildSaltSuits(GameSession session, PlayerState player)
        {
            var suits = new HashSet<Suit>();
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return suits;

            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null && SpreadCraftEffectCatalog.IsBuildV1Salt(def))
                    suits.Add(def.Suit);
            }

            return suits;
        }

        static bool HasTemperanceWild(GameSession session, PlayerState player, ReagentType reagent)
        {
            if (!IsTemperanceResonant(session, player))
                return false;
            return player.TemperanceSaltWildReagent == reagent;
        }

        static string? FindAdept(GameSession session, PlayerState player, int arcanaNumber)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return null;

            foreach (var cardId in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(cardId))
                    continue;
                if (CardEffectSuppressionService.IsSuppressed(session, cardId))
                    continue;
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.ArcanaNumber == arcanaNumber)
                    return cardId;
            }

            return null;
        }

        static CardDefinition? FindAdeptDef(GameSession session, PlayerState player, int arcanaNumber)
        {
            var id = FindAdept(session, player, arcanaNumber);
            if (id == null)
                return null;
            var inst = session.GetCard(id);
            return inst != null ? session.Rules?.CardDatabase.GetById(inst.DefinitionId) : null;
        }

        static bool AllOwnedMinorCards(
            GameSession session,
            PlayerState player,
            IReadOnlyList<string> cardIds,
            ICardDatabase db)
        {
            var owned = BuildPlayerCardSet(player);
            foreach (var id in cardIds)
            {
                if (!owned.Contains(id))
                    return false;
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.IsMajorArcana == true)
                    return false;
            }

            return true;
        }

        static bool AllMatchSuit(
            GameSession session,
            PlayerState player,
            IReadOnlyList<string> cardIds,
            Suit requiredSuit,
            Suit wildCourtSuit,
            ICardDatabase db)
        {
            var owned = BuildPlayerCardSet(player);
            foreach (var id in cardIds)
            {
                if (!owned.Contains(id))
                    return false;

                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null)
                    return false;

                bool suitMatch = def.Suit == requiredSuit;
                bool wildMatch = wildCourtSuit != Suit.None
                    && def.Suit == wildCourtSuit
                    && IsCourtCard(def.Rank);
                if (!suitMatch && !wildMatch)
                    return false;
            }

            return true;
        }

        static bool AllMatchSuitOrSaltWild(
            GameSession session,
            PlayerState player,
            ReagentType reagentType,
            IReadOnlyList<string> cardIds,
            Suit requiredSuit,
            Suit wildCourtSuit,
            ICardDatabase db)
        {
            if (reagentType == ReagentType.Salt)
                return false;

            var owned = BuildPlayerCardSet(player);
            int wildSlots = 1;

            foreach (var id in cardIds)
            {
                if (!owned.Contains(id))
                    return false;

                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null || def.IsMajorArcana)
                    return false;

                bool suitMatch = def.Suit == requiredSuit;
                bool courtWild = wildCourtSuit != Suit.None
                    && def.Suit == wildCourtSuit
                    && IsCourtCard(def.Rank);

                if (suitMatch || courtWild)
                    continue;

                if (wildSlots > 0)
                {
                    wildSlots--;
                    continue;
                }

                return false;
            }

            return true;
        }

        static bool IsCourtCard(Rank rank) =>
            rank == Rank.Princess || rank == Rank.Knight || rank == Rank.Queen || rank == Rank.King;

        static HashSet<string> BuildPlayerCardSet(PlayerState player)
        {
            var set = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) set.Add(id);
            foreach (var id in player.Hand) set.Add(id);
            return set;
        }
    }
}
