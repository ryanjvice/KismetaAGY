using System.Collections.Generic;
using Kismeta.Core.Domain;

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

    /// <summary>Magician: swap one card between Hand and Spread while Card Lock is active.</summary>
    public sealed class MagicianSwapCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string CardId { get; }
        public bool ToSpread { get; }

        public MagicianSwapCommand(int playerId, string cardId, bool toSpread)
        {
            PlayerId = playerId;
            CardId = cardId;
            ToSpread = toSpread;
        }
    }

    /// <summary>Emperor: mark 2 Spread cards as protected from duel loss this age.</summary>
    public sealed class ProtectSpreadCardsCommand : IGameCommand
    {
        public int PlayerId { get; }
        public IReadOnlyList<string> CardIds { get; }

        public ProtectSpreadCardsCommand(int playerId, IReadOnlyList<string> cardIds)
        {
            PlayerId = playerId;
            CardIds = cardIds;
        }
    }

    /// <summary>Hierophant: shift personal Zodiac sign ±1 for Harvest alignment.</summary>
    public sealed class ShiftZodiacCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int Delta { get; }

        public ShiftZodiacCommand(int playerId, int delta)
        {
            PlayerId = playerId;
            Delta = delta;
        }
    }

    /// <summary>Hierophant: apply ±1 shift to attacker's Opposition alignment this age.</summary>
    public sealed class ShiftOppositionZodiacCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int Delta { get; }

        public ShiftOppositionZodiacCommand(int playerId, int delta)
        {
            PlayerId = playerId;
            Delta = delta;
        }
    }

    /// <summary>Devil: sacrifice one card to steal an opponent's Spread card.</summary>
    public sealed class DevilStealCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string SacrificeCardId { get; }
        public int TargetPlayerId { get; }
        public string StolenCardId { get; }

        public DevilStealCommand(int playerId, string sacrificeCardId, int targetPlayerId, string stolenCardId)
        {
            PlayerId = playerId;
            SacrificeCardId = sacrificeCardId;
            TargetPlayerId = targetPlayerId;
            StolenCardId = stolenCardId;
        }
    }

    /// <summary>Priestess: return 2 reviewed harvest cards to the deck.</summary>
    public sealed class CompletePriestessHarvestCommand : IGameCommand
    {
        public int PlayerId { get; }
        public IReadOnlyList<string> ReturnCardIds { get; }

        public CompletePriestessHarvestCommand(int playerId, IReadOnlyList<string> returnCardIds)
        {
            PlayerId = playerId;
            ReturnCardIds = returnCardIds;
        }
    }

    /// <summary>Devil resonant: sacrifice the Devil adept to banish an opponent's adept to the deck.</summary>
    public sealed class DevilBanishAdeptCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int TargetPlayerId { get; }
        public string TargetAdeptCardId { get; }

        public DevilBanishAdeptCommand(int playerId, int targetPlayerId, string targetAdeptCardId)
        {
            PlayerId = playerId;
            TargetPlayerId = targetPlayerId;
            TargetAdeptCardId = targetAdeptCardId;
        }
    }

    /// <summary>Star resonant: nullify an opponent Fate or Adept in Arcanum until refreshed.</summary>
    public sealed class StarNullifyCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int TargetPlayerId { get; }
        public string TargetCardId { get; }

        public StarNullifyCommand(int playerId, int targetPlayerId, string targetCardId)
        {
            PlayerId = playerId;
            TargetPlayerId = targetPlayerId;
            TargetCardId = targetCardId;
        }
    }

    /// <summary>Star resonant: spend 1 Salt to restore a nullified card's effects.</summary>
    public sealed class RefreshStarNullifyCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string TargetCardId { get; }

        public RefreshStarNullifyCommand(int playerId, string targetCardId)
        {
            PlayerId = playerId;
            TargetCardId = targetCardId;
        }
    }

    /// <summary>World resonant: flip World to enable crucible wildcard on the next Fire.</summary>
    public sealed class ActivateWorldWildcardCommand : IGameCommand
    {
        public int PlayerId { get; }

        public ActivateWorldWildcardCommand(int playerId) => PlayerId = playerId;
    }

    /// <summary>World resonant: spend 1 Salt to re-enable flip after consumption this age.</summary>
    public sealed class RefreshWorldWildcardCommand : IGameCommand
    {
        public int PlayerId { get; }

        public RefreshWorldWildcardCommand(int playerId) => PlayerId = playerId;
    }
}
