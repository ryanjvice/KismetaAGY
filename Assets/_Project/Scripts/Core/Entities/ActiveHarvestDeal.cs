namespace Kismeta.Core.Entities
{
    /// <summary>Tracks progress of a stepped harvest deal session.</summary>
    public sealed class ActiveHarvestDeal
    {
        public int PlayerId { get; set; }
        public int TotalDraws { get; set; }
        public int Remaining { get; set; }
        public int CardsDealt { get; set; }
        public int AdeptsBefore { get; set; }
        public bool PriestessActive { get; set; }
    }
}
