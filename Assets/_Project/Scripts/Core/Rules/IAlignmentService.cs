using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Computes Aspect Alignment scores used in Harvest bonuses and Opposition contests.
    /// Rule: highest matching aspect per source only (+3 Sign, +2 Planet, +1 Element).
    /// No double-counting if personal Sign equals Cosmic Age Sign.
    /// </summary>
    public interface IAlignmentService : IRuleService
    {
        int CalculateAlignmentPoints(GameSession session, int playerId, ZodiacSign referenceSign);
    }
}
