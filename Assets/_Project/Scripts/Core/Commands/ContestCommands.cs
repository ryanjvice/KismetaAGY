using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>Defender accepts or declines a pending trade offer.</summary>
    public sealed class RespondTradeCommand : IGameCommand
    {
        public int PlayerId { get; }
        public bool Accept { get; }

        public RespondTradeCommand(int playerId, bool accept)
        {
            PlayerId = playerId;
            Accept   = accept;
        }
    }

    /// <summary>Defender accepts (rolls) or declines a pending duel.</summary>
    public sealed class RespondDuelCommand : IGameCommand
    {
        public int PlayerId { get; }
        public bool Accept { get; }
        /// <summary>When attacker has opponent-chooses-ante curse, defender picks this spread card.</summary>
        public string? ChosenAnteCardId { get; }

        public RespondDuelCommand(int playerId, bool accept, string? chosenAnteCardId = null)
        {
            PlayerId          = playerId;
            Accept            = accept;
            ChosenAnteCardId  = chosenAnteCardId;
        }
    }

    /// <summary>
    /// Defender enters a pending gambit (paying ward reagents) or declines.
    /// When accepting, <see cref="ReagentPayments"/> must sum to the ward cost.
    /// </summary>
    public sealed class RespondGambitCommand : IGameCommand
    {
        public int PlayerId { get; }
        public bool Accept { get; }
        public IReadOnlyList<(ReagentType Type, int Count)> ReagentPayments { get; }

        public RespondGambitCommand(int playerId, bool accept,
            IReadOnlyList<(ReagentType Type, int Count)>? reagentPayments = null)
        {
            PlayerId         = playerId;
            Accept           = accept;
            ReagentPayments  = reagentPayments ?? System.Array.Empty<(ReagentType, int)>();
        }
    }

    /// <summary>Defender accepts a pending opposition (triggers defense roll) or declines.</summary>
    public sealed class RespondOppositionCommand : IGameCommand
    {
        public int PlayerId { get; }
        public bool Accept { get; }

        public RespondOppositionCommand(int playerId, bool accept)
        {
            PlayerId = playerId;
            Accept   = accept;
        }
    }
}
