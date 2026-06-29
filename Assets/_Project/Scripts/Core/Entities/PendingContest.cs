using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// A contest awaiting the defender's response. Only one may be in flight at a time.
    /// </summary>
    public sealed class PendingContest
    {
        public ContestKind Kind { get; set; }
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }

        // Trade
        public IReadOnlyList<string>? OfferCardIds { get; set; }
        public IReadOnlyList<string>? RequestCardIds { get; set; }

        // Duel
        public string? TargetCardId { get; set; }
        public string? AnteCardId { get; set; }

        // Gambit
        public string? OfferedCardId { get; set; }
    }
}
