namespace Kismeta.Core.Rules
{
    public enum AdeptUsageKind
    {
        Passive,
        OncePerAge
    }

    /// <summary>Classifies Adept powers for Active Effects badge copy.</summary>
    public static class AdeptEffectCatalog
    {
        public static AdeptUsageKind UsageFor(int arcanaNumber) => arcanaNumber switch
        {
            1 or 8 or 9 or 17 or 21 => AdeptUsageKind.Passive,
            _ => AdeptUsageKind.OncePerAge
        };

        public static ActiveEffectBadge BadgeFor(
            int arcanaNumber,
            bool isArrested,
            bool usedThisAge,
            bool isAttuned = false)
        {
            if (isArrested)
                return new ActiveEffectBadge("arrested · refresh with Salt", ActiveEffectBadgeTone.Arrested);

            if (isAttuned && AdeptResonantCatalog.HasResonantLayer(arcanaNumber))
                return new ActiveEffectBadge("attuned · resonant", ActiveEffectBadgeTone.Attuned);

            if (UsageFor(arcanaNumber) == AdeptUsageKind.Passive)
                return new ActiveEffectBadge("active · expires if removed", ActiveEffectBadgeTone.Active);

            return usedThisAge
                ? new ActiveEffectBadge("once per age · used", ActiveEffectBadgeTone.Used)
                : new ActiveEffectBadge("once per age · available", ActiveEffectBadgeTone.Available);
        }
    }
}
