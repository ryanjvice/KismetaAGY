using System.Collections.Generic;

namespace Kismeta.Core.Commands
{
    /// <summary>Player chooses to purchase an Adept card drawn during Harvest.</summary>
    public sealed class BuyAdeptCommand : IGameCommand
    {
        public int PlayerId { get; }
        /// <summary>Instance ID of the Adept card being offered.</summary>
        public string AdeptCardId { get; }
        /// <summary>3 cards from the player's Spread or Hand used as payment.</summary>
        public IReadOnlyList<string> PaymentCardIds { get; }
        /// <summary>
        /// Optional: if the Arcanum is already at the limit, this is the Adept card
        /// instance ID to return to the Common Deck in exchange.
        /// </summary>
        public string? SwapOutAdeptId { get; }

        public BuyAdeptCommand(int playerId, string adeptCardId,
            IReadOnlyList<string> paymentCardIds, string? swapOutAdeptId = null)
        {
            PlayerId       = playerId;
            AdeptCardId    = adeptCardId;
            PaymentCardIds = paymentCardIds;
            SwapOutAdeptId = swapOutAdeptId;
        }
    }

    /// <summary>Player declines to purchase an Adept card drawn during Harvest; it is discarded.</summary>
    public sealed class DeclineAdeptCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string AdeptCardId { get; }

        public DeclineAdeptCommand(int playerId, string adeptCardId)
        {
            PlayerId    = playerId;
            AdeptCardId = adeptCardId;
        }
    }
}
