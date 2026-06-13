using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Tracks a single Crucible Card slot for a player (they receive 4 per game).
    /// </summary>
    public sealed class PlayerCrucibleSlot
    {
        public string CardInstanceId { get; }
        public CrucibleCardState State { get; private set; }
        public bool HasCoal { get; private set; }
        public int WardCount { get; private set; }
        /// <summary>Round number in which the stone was fired via this slot. -1 = not yet fired.</summary>
        public int FiredAtRound { get; private set; } = -1;

        public PlayerCrucibleSlot(string cardInstanceId)
        {
            CardInstanceId = cardInstanceId;
            State = CrucibleCardState.Dormant;
            HasCoal = false;
        }

        public void Activate()
        {
            if (State == CrucibleCardState.Dormant)
                State = CrucibleCardState.Active;
        }

        public void Fire(int currentRound)
        {
            if (State == CrucibleCardState.Active)
            {
                State = CrucibleCardState.Fired;
                FiredAtRound = currentRound;
            }
        }

        public void Discard() => State = CrucibleCardState.Discarded;
        public void Arrest()  => State = CrucibleCardState.Arrested;

        public void PlaceCoal() => HasCoal = true;
        public void AddWard()   => WardCount++;
        public void RemoveWard()
        {
            if (WardCount > 0) WardCount--;
        }
    }
}
