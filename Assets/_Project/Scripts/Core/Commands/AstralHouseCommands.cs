using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>Player builds an Astral House on their current Zodiac Sign during Spring Hub.</summary>
    public sealed class BuildAstralHouseCommand : IGameCommand
    {
        public int PlayerId { get; }
        /// <summary>Must match the player's current rolled Sign this round.</summary>
        public ZodiacSign Sign { get; }
        /// <summary>Exactly 2 cards matching the Sign's Planet, discarded as payment.</summary>
        public IReadOnlyList<string> PaymentCardIds { get; }

        public BuildAstralHouseCommand(int playerId, ZodiacSign sign, IReadOnlyList<string> paymentCardIds)
        {
            PlayerId       = playerId;
            Sign           = sign;
            PaymentCardIds = paymentCardIds;
        }
    }
}
