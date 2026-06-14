using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;

namespace Kismeta.Core.Views
{
    /// <summary>
    /// Everything about a player that is visible to all players at the table:
    /// Spread, Arcanum, board pieces, reagents, Crucible slots (state only — not Hand).
    /// Hand card contents are never included here.
    /// </summary>
    public sealed class PublicPlayerView
    {
        public int PlayerId { get; }
        public PlayerColor Color { get; }
        public bool IsAgekeeper { get; }
        public ZodiacSign CurrentSign { get; }
        /// <summary>Codex variant assigned to this player (public information — not secret).</summary>
        public CodexVariant AssignedCodex { get; }

        public IReadOnlyList<string> Spread  { get; }
        public IReadOnlyList<string> Arcanum { get; }

        public int HandCardCount { get; }

        public StonePosition StonePosition              { get; }
        public StoneState StoneState                    { get; }
        public int StoneWardCount                       { get; }
        public bool ReturnedFromStasisThisRound         { get; }

        public IReadOnlyDictionary<ReagentType, int> Reagents { get; }
        public IReadOnlyCollection<ZodiacSign> AstralHouses { get; }
        public int UnplacedAstralHouses { get; }
        public IReadOnlyList<CrucibleSlotView> CrucibleSlots { get; }

        private PublicPlayerView(
            PlayerState p,
            ICardDatabase? db = null,
            IReadOnlyDictionary<string, string>? cardMap = null)
        {
            PlayerId      = p.PlayerId;
            Color         = p.Color;
            IsAgekeeper   = p.IsAgekeeper;
            CurrentSign   = p.CurrentSign;
            AssignedCodex = p.AssignedCodex;
            Spread = p.Spread.AsReadOnly();
            Arcanum = p.Arcanum.AsReadOnly();
            HandCardCount = p.Hand.Count;
            StonePosition               = p.StonePosition;
            StoneState                  = p.StoneState;
            StoneWardCount              = p.StoneWardCount;
            ReturnedFromStasisThisRound = p.ReturnedFromStasisThisRound;

            var reagents = new Dictionary<ReagentType, int>();
            foreach (ReagentType t in System.Enum.GetValues(typeof(ReagentType)))
                reagents[t] = p.GetReagent(t);
            Reagents = reagents;

            AstralHouses         = p.AstralHouses;
            UnplacedAstralHouses = p.UnplacedAstralHouses;

            var slots = new List<CrucibleSlotView>(p.CrucibleSlots.Count);
            foreach (var slot in p.CrucibleSlots)
            {
                string? formula = null;
                if (slot.State >= CrucibleCardState.Active && db != null && cardMap != null)
                {
                    if (cardMap.TryGetValue(slot.CardInstanceId, out var defId))
                        formula = db.GetById(defId)?.AlchemicalFormula;
                }
                slots.Add(new CrucibleSlotView(slot, formula));
            }
            CrucibleSlots = slots;
        }

        public static PublicPlayerView From(
            PlayerState player,
            ICardDatabase? db = null,
            IReadOnlyDictionary<string, string>? cardMap = null) => new(player, db, cardMap);
    }

    /// <summary>
    /// Public view of a single Crucible Card slot.
    /// AlchemicalFormula is null while the slot is Dormant (card is face-down, formula hidden).
    /// It becomes visible once the slot is Activated.
    /// </summary>
    public sealed class CrucibleSlotView
    {
        public string CardInstanceId { get; }
        public CrucibleCardState State { get; }
        public bool HasCoal { get; }
        public int WardCount { get; }
        /// <summary>Null when Dormant (formula hidden); populated once Active.</summary>
        public string? AlchemicalFormula { get; }

        public CrucibleSlotView(PlayerCrucibleSlot slot, string? alchemicalFormula = null)
        {
            CardInstanceId    = slot.CardInstanceId;
            State             = slot.State;
            HasCoal           = slot.HasCoal;
            WardCount         = slot.WardCount;
            AlchemicalFormula = alchemicalFormula;
        }
    }
}
