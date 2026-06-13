using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Abstraction over the card definition store, kept in Core so rule services can resolve
    /// definitions without depending on the Unity-aware CardDatabase in Kismeta.Data.
    /// </summary>
    public interface ICardDatabase
    {
        CardDefinition? GetById(string id);
        IEnumerable<CardDefinition> GetAll();
        IEnumerable<CardDefinition> GetCrucibleByGroup(CrucibleGroup group);
        IEnumerable<CardDefinition> GetBySuit(Suit suit);
        int Count { get; }
    }
}
