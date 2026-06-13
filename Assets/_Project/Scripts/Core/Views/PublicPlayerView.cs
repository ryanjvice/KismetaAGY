using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

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

        public IReadOnlyList<string> Spread  { get; }
        public IReadOnlyList<string> Arcanum { get; }

        public int HandCardCount { get; }

        public StonePosition StonePosition { get; }
        public StoneState StoneState { get; }
        public int StoneWardCount { get; }

        public IReadOnlyDictionary<ReagentType, int> Reagents { get; }
        public IReadOnlyCollection<ZodiacSign> AstralHouses { get; }
        public int UnplacedAstralHouses { get; }
        public IReadOnlyList<CrucibleSlotView> CrucibleSlots { get; }

        private PublicPlayerView(PlayerState p)
        {
            PlayerId = p.PlayerId;
            Color = p.Color;
            IsAgekeeper = p.IsAgekeeper;
            CurrentSign = p.CurrentSign;
            Spread = p.Spread.AsReadOnly();
            Arcanum = p.Arcanum.AsReadOnly();
            HandCardCount = p.Hand.Count;
            StonePosition = p.StonePosition;
            StoneState = p.StoneState;
            StoneWardCount = p.StoneWardCount;

            var reagents = new Dictionary<ReagentType, int>();
            foreach (ReagentType t in System.Enum.GetValues(typeof(ReagentType)))
                reagents[t] = p.GetReagent(t);
            Reagents = reagents;

            AstralHouses         = p.AstralHouses;
            UnplacedAstralHouses = p.UnplacedAstralHouses;

            var slots = new List<CrucibleSlotView>(p.CrucibleSlots.Count);
            foreach (var slot in p.CrucibleSlots)
                slots.Add(new CrucibleSlotView(slot));
            CrucibleSlots = slots;
        }

        public static PublicPlayerView From(PlayerState player) => new(player);
    }

    /// <summary>Public view of a single Crucible Card slot (state + coal, not formulas).</summary>
    public sealed class CrucibleSlotView
    {
        public string CardInstanceId { get; }
        public CrucibleCardState State { get; }
        public bool HasCoal { get; }
        public int WardCount { get; }

        public CrucibleSlotView(PlayerCrucibleSlot slot)
        {
            CardInstanceId = slot.CardInstanceId;
            State = slot.State;
            HasCoal = slot.HasCoal;
            WardCount = slot.WardCount;
        }
    }
}
