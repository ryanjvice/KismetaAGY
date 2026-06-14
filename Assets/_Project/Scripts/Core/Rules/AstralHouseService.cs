using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Validates and executes the "Build Astral House" Summer action.
    ///
    /// Rules:
    ///  - Player must have at least one unplaced Astral House token.
    ///  - The target Sign must be the player's current rolled Sign this round.
    ///  - No other player may already have a House on that Sign.
    ///  - Payment is exactly 2 cards whose Planet matches the Sign's ruling Planet.
    ///    Cards may come from Spread or Hand.
    ///  - Once built, the House earns Alignment bonuses each Harvest and grants
    ///    that Sign's Cosmic Effect permanently.
    /// </summary>
    public sealed class AstralHouseService
    {
        private readonly ICardDatabase _db;

        public AstralHouseService(ICardDatabase db) => _db = db;

        public CommandResult TryBuild(GameSession session, int playerId,
            ZodiacSign sign, IReadOnlyList<string> paymentCardIds)
        {
            var player = session.Players[playerId];

            if (player.UnplacedAstralHouses <= 0)
                return CommandResult.Invalid("No Astral House tokens remaining.");

            if (sign == ZodiacSign.None)
                return CommandResult.Invalid("Invalid sign.");

            if (sign != player.CurrentSign)
                return CommandResult.Invalid(
                    $"You may only build on your current Sign ({player.CurrentSign}), not {sign}.");

            if (player.AstralHouses.Contains(sign))
                return CommandResult.Invalid($"You already have a House on {sign}.");

            foreach (var p in session.Players)
                if (p.PlayerId != playerId && p.AstralHouses.Contains(sign))
                    return CommandResult.Invalid($"{sign} is already claimed by P{p.PlayerId}.");

            if (paymentCardIds.Count != 2)
                return CommandResult.Invalid("Building an Astral House costs exactly 2 cards.");

            // Build player card set (Spread + Hand)
            var playerCards = new HashSet<string>(player.Spread.Count + player.Hand.Count);
            foreach (var id in player.Spread) playerCards.Add(id);
            foreach (var id in player.Hand)   playerCards.Add(id);

            // Validate ownership and planet matching
            var requiredPlanet = Correspondence.PlanetFor(sign);
            foreach (var id in paymentCardIds)
            {
                if (!playerCards.Contains(id))
                    return CommandResult.Invalid($"Card {id} does not belong to player {playerId}.");

                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def == null || def.Planet != requiredPlanet)
                    return CommandResult.Invalid(
                        $"Payment cards must match the ruling Planet of {sign} ({requiredPlanet}).");
            }

            // Pay the cards
            foreach (var id in paymentCardIds)
            {
                player.Spread.Remove(id);
                player.Hand.Remove(id);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }

            // Place the House
            player.AstralHouses.Add(sign);
            player.UnplacedAstralHouses--;

            // Recompute personal cosmic effects now that the house roster changed
            player.PersonalCosmicEffects = CosmicEffectService.ComputePersonalEffects(
                player.CurrentSign, player.AstralHouses, session.Board.CosmicAgeSign);

            session.EmitEvent(new AstralHouseBuiltEvent(playerId, sign));
            return CommandResult.Ok($"Astral House built on {sign}.");
        }
    }
}
