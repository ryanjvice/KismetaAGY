using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Commands
{
    /// <summary>Internal command used during setup to register a card instance with the session.</summary>
    public sealed class RegisterCardCommand : IGameCommand
    {
        public CardInstance Card { get; }
        public RegisterCardCommand(CardInstance card) => Card = card;
    }

    /// <summary>Player draws cards from the common deck during Harvest. Spring Step 3.</summary>
    public sealed class HarvestCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int Count    { get; }
        public HarvestCommand(int playerId, int count) { PlayerId = playerId; Count = count; }
    }

    /// <summary>
    /// Player assigns their harvested cards to Spread or Hand zones. Spring Step 4.
    /// SpreadCardIds and HandCardIds must cover all cards currently in the player's Hand
    /// (harvested cards land in Hand first, then Commune redistributes them).
    /// Hand limit: max 5. Spread: no limit during the round.
    /// </summary>
    public sealed class CommuneCommand : IGameCommand
    {
        public int PlayerId { get; }
        public IReadOnlyList<string> SpreadCardIds { get; }
        public IReadOnlyList<string> HandCardIds   { get; }

        public CommuneCommand(int playerId, IReadOnlyList<string> spreadCards, IReadOnlyList<string> handCards)
        {
            PlayerId = playerId;
            SpreadCardIds = spreadCards;
            HandCardIds   = handCards;
        }

        /// <summary>Convenience: put all cards into Spread, nothing in Hand.</summary>
        public static CommuneCommand AllToSpread(int playerId, IReadOnlyList<string> allCardIds) =>
            new(playerId, allCardIds, System.Array.Empty<string>());
    }

    /// <summary>Player passes without taking an action (Summer free-order pool).</summary>
    public sealed class PassActionCommand : IGameCommand
    {
        public int PlayerId { get; }
        public PassActionCommand(int playerId) => PlayerId = playerId;
    }
}
