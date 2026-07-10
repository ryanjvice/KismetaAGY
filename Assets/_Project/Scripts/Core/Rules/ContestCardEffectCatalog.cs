using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    public enum ContestParticipantRole
    {
        Attacker,
        Defender
    }

    /// <summary>Static combat effect table for spread cards (Knight, Princess, reversed curses).</summary>
    public static class ContestCardEffectCatalog
    {
        public static void ApplySpreadCard(
            CardDefinition def,
            ContestParticipantRole holderRole,
            ContestKind kind,
            ref ContestRollModifiers attackerMods,
            ref ContestRollModifiers defenderMods,
            ref bool attackerForcesBestOfThree)
        {
            if (def.EffectType.Equals("Reversed", System.StringComparison.OrdinalIgnoreCase))
                ApplyReversedCombat(def, holderRole, kind, ref attackerMods, ref defenderMods, ref attackerForcesBestOfThree);

            if (def.Rank == Rank.Knight && kind == ContestKind.Duel
                && def.EffectType.Equals("Duel", System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyKnight(def, holderRole, ref attackerMods, ref defenderMods);
                return;
            }

            if (def.Rank == Rank.Princess && kind == ContestKind.Gambit
                && def.EffectType.Equals("Gambit", System.StringComparison.OrdinalIgnoreCase))
                ApplyPrincess(def, holderRole, ref attackerMods, ref defenderMods);
        }

        static void ApplyKnight(
            CardDefinition def,
            ContestParticipantRole holderRole,
            ref ContestRollModifiers attackerMods,
            ref ContestRollModifiers defenderMods)
        {
            bool grantsAttackBonus = def.Suit is Suit.Swords or Suit.Wands;
            bool grantsDefendBonus = def.Suit is Suit.Cups or Suit.Pentacles;

            if (holderRole == ContestParticipantRole.Attacker && grantsAttackBonus)
                attackerMods.AttackBonus += 1;
            if (holderRole == ContestParticipantRole.Defender && grantsDefendBonus)
                defenderMods.DefendBonus += 1;
        }

        static void ApplyPrincess(
            CardDefinition def,
            ContestParticipantRole holderRole,
            ref ContestRollModifiers attackerMods,
            ref ContestRollModifiers defenderMods)
        {
            bool grantsAttackBonus = def.Suit is Suit.Swords or Suit.Wands;
            bool grantsDefendBonus = def.Suit is Suit.Cups or Suit.Pentacles;

            if (holderRole == ContestParticipantRole.Attacker && grantsAttackBonus)
                attackerMods.AttackBonus += 1;
            if (holderRole == ContestParticipantRole.Defender && grantsDefendBonus)
                defenderMods.DefendBonus += 1;
        }

        static void ApplyReversedCombat(
            CardDefinition def,
            ContestParticipantRole holderRole,
            ContestKind kind,
            ref ContestRollModifiers attackerMods,
            ref ContestRollModifiers defenderMods,
            ref bool attackerForcesBestOfThree)
        {
            if (!def.EffectType.Equals("Reversed", System.StringComparison.OrdinalIgnoreCase))
                return;

            switch (def.Id)
            {
                case "minor.wands.five.1":
                    if (holderRole == ContestParticipantRole.Defender)
                        defenderMods.DefendBonus -= 1;
                    break;

                case "minor.wands.six.1":
                    if (holderRole == ContestParticipantRole.Attacker)
                        attackerMods.AttackBonus -= 1;
                    break;

                case "minor.cups.five.1":
                    if (holderRole == ContestParticipantRole.Defender && kind == ContestKind.Duel)
                        attackerMods.MayRerollAttack = true;
                    break;

                case "minor.swords.six.1":
                    if (holderRole == ContestParticipantRole.Attacker && kind == ContestKind.Duel)
                        attackerForcesBestOfThree = true;
                    break;
            }
        }
    }
}
