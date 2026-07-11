using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    public enum HousePaymentMode
    {
        StandardPlanet,
        EntryFeeAce
    }

    public readonly struct HousePaymentOption
    {
        public readonly int CardCount;
        public readonly HousePaymentMode Mode;
        public readonly string Source;

        public HousePaymentOption(int cardCount, HousePaymentMode mode, string source)
        {
            CardCount = cardCount;
            Mode = mode;
            Source = source;
        }
    }

    /// <summary>Resolves Astral House build payment paths (standard planet vs Entry Fee ace).</summary>
    public static class HouseModifierService
    {
        public static IReadOnlyList<HousePaymentOption> GetPaymentOptions(
            GameSession session, int playerId, ZodiacSign sign)
        {
            var options = new List<HousePaymentOption>();
            int standardCount = AstralHouseService.RequiredPaymentCount(session.Mode);
            options.Add(new HousePaymentOption(standardCount, HousePaymentMode.StandardPlanet, "standard"));

            var db = session.Rules?.CardDatabase;
            if (db != null && IsEntryFeeElementAligned(sign, playerId, session, db))
                options.Add(new HousePaymentOption(1, HousePaymentMode.EntryFeeAce, "entry-fee"));

            return options;
        }

        public static bool TryResolvePaymentMode(
            GameSession session,
            int playerId,
            ZodiacSign sign,
            IReadOnlyList<string> paymentCardIds,
            ICardDatabase db,
            out HousePaymentMode mode,
            out string? error)
        {
            mode = HousePaymentMode.StandardPlanet;
            error = null;

            if (paymentCardIds.Count == 1
                && ValidateEntryFeePayment(session, playerId, sign, paymentCardIds, db, out error))
            {
                mode = HousePaymentMode.EntryFeeAce;
                return true;
            }

            int required = AstralHouseService.RequiredPaymentCount(session.Mode);
            if (paymentCardIds.Count != required)
            {
                error = required == 1
                    ? "Building an Astral House costs exactly 1 card (Quickplay) or 1 Entry Fee ace."
                    : "Building an Astral House costs exactly 2 planet-matching cards or 1 Entry Fee ace.";
                return false;
            }

            if (!ValidateStandardPayment(session, playerId, sign, paymentCardIds, db, out error))
                return false;

            mode = HousePaymentMode.StandardPlanet;
            return true;
        }

        public static bool ValidateStandardPayment(
            GameSession session,
            int playerId,
            ZodiacSign sign,
            IReadOnlyList<string> paymentCardIds,
            ICardDatabase db,
            out string? error)
        {
            error = null;
            var player = session.Players[playerId];
            var owned = BuildOwnedSet(player);
            var requiredPlanet = Correspondence.PlanetFor(sign);

            foreach (var id in paymentCardIds)
            {
                if (!owned.Contains(id))
                {
                    error = $"Card {id} does not belong to player {playerId}.";
                    return false;
                }

                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null || def.Planet != requiredPlanet)
                {
                    error = $"Payment cards must match the ruling Planet of {sign} ({requiredPlanet}).";
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateEntryFeePayment(
            GameSession session,
            int playerId,
            ZodiacSign sign,
            IReadOnlyList<string> paymentCardIds,
            ICardDatabase db,
            out string? error)
        {
            error = null;
            if (paymentCardIds.Count != 1)
            {
                error = "Entry Fee build requires exactly 1 ace.";
                return false;
            }

            if (Correspondence.ElementFor(sign) == Element.None)
            {
                error = "Your current Sign's element must match the ace's suit element.";
                return false;
            }

            var player = session.Players[playerId];
            var owned = BuildOwnedSet(player);
            var id = paymentCardIds[0];
            if (!owned.Contains(id))
            {
                error = $"Card {id} does not belong to player {playerId}.";
                return false;
            }

            var inst = session.GetCard(id);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null || !SpreadHouseEffectCatalog.IsEntryFeeAce(def))
            {
                error = "Entry Fee payment must be an Ace V1 Entry Fee card.";
                return false;
            }

            if (Correspondence.ElementFor(def.Suit) != Correspondence.ElementFor(sign))
            {
                error = "Ace suit element must match your current Sign's element.";
                return false;
            }

            return true;
        }

        static bool IsEntryFeeElementAligned(ZodiacSign sign, int playerId, GameSession session, ICardDatabase? db)
        {
            var signElement = Correspondence.ElementFor(sign);
            if (signElement == Element.None || db == null) return false;
            return HasEligibleEntryFeeAce(session.Players[playerId], signElement, session, db);
        }

        public static bool HasEligibleEntryFeeAce(PlayerState player, Element signElement, GameSession session, ICardDatabase db)
        {
            foreach (var id in player.Spread)
                if (IsEntryFeeAceForElement(session, id, signElement, db)) return true;
            foreach (var id in player.Hand)
                if (IsEntryFeeAceForElement(session, id, signElement, db)) return true;
            return false;
        }

        static bool IsEntryFeeAceForElement(GameSession session, string cardId, Element signElement, ICardDatabase db)
        {
            var inst = session.GetCard(cardId);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            return def != null
                   && SpreadHouseEffectCatalog.IsEntryFeeAce(def)
                   && Correspondence.ElementFor(def.Suit) == signElement;
        }

        static HashSet<string> BuildOwnedSet(PlayerState player)
        {
            var owned = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) owned.Add(id);
            foreach (var id in player.Hand) owned.Add(id);
            return owned;
        }
    }
}
