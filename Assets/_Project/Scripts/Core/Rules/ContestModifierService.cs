using System.Collections.Generic;
using System.Text;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Aggregates spread and adept modifiers for Duel/Gambit resolution.
    /// Stacking: sum all spread bonuses; Strength resonant replaces base; rerolls OR-combine.
    /// Reversed curses are always active until Phase 5 negation is defined.
    /// </summary>
    public static class ContestModifierService
    {
        public static ContestModifiers Build(
            GameSession session,
            ContestKind kind,
            int attackerId,
            int defenderId)
        {
            var attackerMods = ContestRollModifiers.Default;
            var defenderMods = ContestRollModifiers.Default;
            bool attackerForcesBestOfThree = false;

            var db = session.Rules?.CardDatabase;
            if (db == null)
                return ContestModifiers.None;

            if (attackerId >= 0 && attackerId < session.Players.Count)
                ScanSpread(session, db, session.Players[attackerId], ContestParticipantRole.Attacker,
                    kind, ref attackerMods, ref defenderMods, ref attackerForcesBestOfThree);

            if (defenderId >= 0 && defenderId < session.Players.Count && defenderId != attackerId)
                ScanSpread(session, db, session.Players[defenderId], ContestParticipantRole.Defender,
                    kind, ref attackerMods, ref defenderMods, ref attackerForcesBestOfThree);

            if (attackerId >= 0 && attackerId < session.Players.Count)
                ApplyAdeptModifiers(session, db, session.Players[attackerId], ContestParticipantRole.Attacker,
                    kind, ref attackerMods);

            if (defenderId >= 0 && defenderId < session.Players.Count && defenderId != attackerId)
                ApplyAdeptModifiers(session, db, session.Players[defenderId], ContestParticipantRole.Defender,
                    kind, ref defenderMods);

            return new ContestModifiers(attackerMods, defenderMods, attackerForcesBestOfThree);
        }

        public static bool ResolveBestOfThree(
            GameSession session,
            ContestKind kind,
            ContestModifiers modifiers) =>
            session.Board.ContestEffects.IsBestOfThree(kind)
            || (kind == ContestKind.Duel && modifiers.AttackerForcesBestOfThree);

        public static string DescribeForPlayer(
            GameSession session,
            ContestKind kind,
            int perspectivePlayerId,
            int attackerId,
            int defenderId)
        {
            var mods = Build(session, kind, attackerId, defenderId);
            bool bestOfThree = ResolveBestOfThree(session, kind, mods);
            bool isAttacker = perspectivePlayerId == attackerId;

            var lines = new List<string>();
            var self = isAttacker ? mods.Attacker : mods.Defender;
            var foe  = isAttacker ? mods.Defender : mods.Attacker;

            AppendSide(lines, "You", self);
            AppendSide(lines, "Foe", foe);

            if (bestOfThree)
                lines.Add("Best-of-three series");

            if (kind == ContestKind.Duel && mods.AttackerForcesBestOfThree
                && !session.Board.ContestEffects.DuelBestOfThree)
            {
                lines.Add("6 of Swords: challenger best-of-three");
            }

            return lines.Count == 0 ? string.Empty : string.Join(" · ", lines);
        }

        public static string DescribeExchangeContext(
            GameSession session,
            ContestKind kind,
            int attackerId,
            int defenderId)
        {
            var mods = Build(session, kind, attackerId, defenderId);
            if (!mods.Attacker.HasAnyEffect && !mods.Defender.HasAnyEffect
                && !mods.AttackerForcesBestOfThree)
                return string.Empty;

            var parts = new List<string>();
            AppendSide(parts, $"P{attackerId}", mods.Attacker);
            AppendSide(parts, $"P{defenderId}", mods.Defender);
            if (mods.AttackerForcesBestOfThree && kind == ContestKind.Duel)
                parts.Add("scoped best-of-3");
            return string.Join("; ", parts);
        }

        static void ScanSpread(
            GameSession session,
            ICardDatabase db,
            PlayerState player,
            ContestParticipantRole holderRole,
            ContestKind kind,
            ref ContestRollModifiers attackerMods,
            ref ContestRollModifiers defenderMods,
            ref bool attackerForcesBestOfThree)
        {
            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def  = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                ContestCardEffectCatalog.ApplySpreadCard(
                    def, holderRole, kind,
                    ref attackerMods, ref defenderMods, ref attackerForcesBestOfThree);
            }
        }

        static void ApplyAdeptModifiers(
            GameSession session,
            ICardDatabase db,
            PlayerState player,
            ContestParticipantRole holderRole,
            ContestKind kind,
            ref ContestRollModifiers sideMods)
        {
            foreach (var cardId in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(cardId))
                    continue;

                var inst = session.GetCard(cardId);
                var def  = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType != MajorArcanaType.Adept)
                    continue;

                switch (def.ArcanaNumber)
                {
                    case 8:
                        ApplyStrength(holderRole, kind, player, def, ref sideMods);
                        break;
                    case 7:
                        ApplyChariotResonant(holderRole, player, def, ref sideMods);
                        break;
                }
            }
        }

        static void ApplyStrength(
            ContestParticipantRole holderRole,
            ContestKind kind,
            PlayerState player,
            CardDefinition adeptDef,
            ref ContestRollModifiers sideMods)
        {
            if (holderRole != ContestParticipantRole.Attacker)
                return;

            bool resonant = AdeptAttunement.IsResonant(player, adeptDef);
            if (resonant && (kind == ContestKind.Duel || kind == ContestKind.Gambit))
                sideMods.AttackBonus += 2;
            else if (kind == ContestKind.Duel)
                sideMods.AttackBonus += 1;
        }

        static void ApplyChariotResonant(
            ContestParticipantRole holderRole,
            PlayerState player,
            CardDefinition adeptDef,
            ref ContestRollModifiers sideMods)
        {
            if (!AdeptAttunement.IsResonant(player, adeptDef))
                return;

            if (holderRole == ContestParticipantRole.Attacker)
                sideMods.MayRerollAttack = true;
            else
                sideMods.MayRerollDefend = true;
        }

        static void AppendSide(List<string> lines, string label, ContestRollModifiers mods)
        {
            var parts = new List<string>();
            if (mods.AttackBonus != 0)
                parts.Add($"{FormatBonus(mods.AttackBonus)} attack");
            if (mods.DefendBonus != 0)
                parts.Add($"{FormatBonus(mods.DefendBonus)} defend");
            if (mods.MayRerollAttack)
                parts.Add("reroll attack");
            if (mods.MayRerollDefend)
                parts.Add("reroll defend");
            if (parts.Count > 0)
                lines.Add($"{label}: {string.Join(", ", parts)}");
        }

        static string FormatBonus(int bonus) => bonus > 0 ? $"+{bonus}" : bonus.ToString();
    }
}
