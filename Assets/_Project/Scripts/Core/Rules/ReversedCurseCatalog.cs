namespace Kismeta.Core.Rules
{
    public enum ReversedCurseKind
    {
        None,
        HandLimitPenalty,
        OpponentDuelReroll,
        OpponentChoosesAnte,
        TradeDoubleOffer,
        CraftExtraCard,
        AdeptBuyExtraCard,
        DualAnte,
        SaltCostOppositionGambit,
        DuelBestOfThree,
        DefendDiceMinus,
        AttackDiceMinus,
        WinnerDrawTwoOnDuel
    }

    /// <summary>Classifies rank 4–6 V1 Reversed curse hooks by card id.</summary>
    public static class ReversedCurseCatalog
    {
        public static ReversedCurseKind KindFor(string cardId) => cardId switch
        {
            "minor.cups.four.1" => ReversedCurseKind.HandLimitPenalty,
            "minor.cups.five.1" => ReversedCurseKind.OpponentDuelReroll,
            "minor.cups.six.1" => ReversedCurseKind.OpponentChoosesAnte,
            "minor.pentacles.four.1" => ReversedCurseKind.TradeDoubleOffer,
            "minor.pentacles.five.1" => ReversedCurseKind.CraftExtraCard,
            "minor.pentacles.six.1" => ReversedCurseKind.AdeptBuyExtraCard,
            "minor.swords.four.1" => ReversedCurseKind.DualAnte,
            "minor.swords.five.1" => ReversedCurseKind.SaltCostOppositionGambit,
            "minor.swords.six.1" => ReversedCurseKind.DuelBestOfThree,
            "minor.wands.four.1" => ReversedCurseKind.WinnerDrawTwoOnDuel,
            "minor.wands.five.1" => ReversedCurseKind.DefendDiceMinus,
            "minor.wands.six.1" => ReversedCurseKind.AttackDiceMinus,
            _ => ReversedCurseKind.None
        };

        public static bool IsReversedCurseCard(string cardId)
            => KindFor(cardId) != ReversedCurseKind.None;
    }
}
