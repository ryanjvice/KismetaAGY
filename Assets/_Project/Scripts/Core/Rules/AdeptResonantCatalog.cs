namespace Kismeta.Core.Rules
{
    /// <summary>Classifies which resonant hooks apply per adept arcana number (enforcement metadata).</summary>
    public enum AdeptResonantKind
    {
        None,
        ReversedNegation,      // Magician
        HandLimit7,            // Priestess
        CraftTwoMarks,         // Empress
        BroadProtection,       // Emperor
        ShiftPlusMinus2,       // Hierophant
        BanishAdept,           // Devil
        DuelReroll,            // Chariot
        CombatDicePlus2,       // Strength
        SaltWildCraft,         // Temperance
        NullifyCard,           // Star
        CrucibleWildcard,      // World
        DoubleElementAlign     // Hermit
    }

    public static class AdeptResonantCatalog
    {
        public static AdeptResonantKind KindFor(int arcanaNumber) => arcanaNumber switch
        {
            1  => AdeptResonantKind.ReversedNegation,
            2  => AdeptResonantKind.HandLimit7,
            3  => AdeptResonantKind.CraftTwoMarks,
            4  => AdeptResonantKind.BroadProtection,
            5  => AdeptResonantKind.ShiftPlusMinus2,
            7  => AdeptResonantKind.DuelReroll,
            8  => AdeptResonantKind.CombatDicePlus2,
            9  => AdeptResonantKind.DoubleElementAlign,
            14 => AdeptResonantKind.SaltWildCraft,
            15 => AdeptResonantKind.BanishAdept,
            17 => AdeptResonantKind.NullifyCard,
            21 => AdeptResonantKind.CrucibleWildcard,
            _  => AdeptResonantKind.None
        };

        public static bool HasResonantLayer(int arcanaNumber)
            => KindFor(arcanaNumber) != AdeptResonantKind.None;
    }
}
