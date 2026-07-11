using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Shared adept queries and helpers for Phase 3 base passives.</summary>
    public static class AdeptEffectService
    {
        public const int MagicianArcana = 1;
        public const int PriestessArcana = 2;
        public const int EmperorArcana = 4;
        public const int HierophantArcana = 5;
        public const int ChariotArcana = 7;
        public const int DevilArcana = 15;
        public const int StarArcana = 17;
        public const int WorldArcana = 21;

        public static string? FindAdeptInstance(GameSession session, PlayerState player, int arcanaNumber)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return null;

            foreach (var cardId in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(cardId))
                    continue;
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.ArcanaNumber == arcanaNumber)
                    return cardId;
            }

            return null;
        }

        public static bool HasAdept(GameSession session, PlayerState player, int arcanaNumber)
            => FindAdeptInstance(session, player, arcanaNumber) != null;

        public static bool CanUseOncePerAge(GameSession session, PlayerState player, int arcanaNumber)
        {
            if (AdeptEffectCatalog.UsageFor(arcanaNumber) == AdeptUsageKind.Passive)
                return true;

            var adeptId = FindAdeptInstance(session, player, arcanaNumber);
            return adeptId != null && !player.UsedAdeptInstanceIdsThisAge.Contains(adeptId);
        }

        public static bool TryMarkUsed(GameSession session, int playerId, int arcanaNumber)
        {
            var adeptId = FindAdeptInstance(session, session.Players[playerId], arcanaNumber);
            if (adeptId == null)
                return false;
            if (AdeptEffectCatalog.UsageFor(arcanaNumber) == AdeptUsageKind.Passive)
                return true;
            ActiveEffectsService.MarkAdeptUsed(session, playerId, adeptId);
            return true;
        }

        public static ZodiacSign ShiftSign(ZodiacSign sign, int delta)
        {
            if (sign == ZodiacSign.None || delta == 0)
                return sign;

            int value = (int)sign + delta;
            while (value < 1) value += 12;
            while (value > 12) value -= 12;
            return (ZodiacSign)value;
        }

        public static void TryApplyStarPostLossDraw(GameSession session, int loserId)
        {
            var loser = session.Players[loserId];
            if (HasAdept(session, loser, StarArcana))
                DrawCardsToHand(session, loserId, 2);
        }

        public static void DrawCardsToHand(GameSession session, int playerId, int count)
        {
            var player = session.Players[playerId];
            for (int i = 0; i < count; i++)
            {
                if (session.Board.CommonDeck.Count == 0)
                    SpringRules.ReshuffleDiscardStatic(session);
                if (session.Board.CommonDeck.Count == 0)
                    break;

                var id = session.Board.CommonDeck.Pop();
                var inst = session.GetCard(id);
                if (inst == null)
                    continue;

                inst.MoveTo(CardZone.Hand, playerId);
                player.Hand.Add(id);
            }
        }

        public static bool IsEmperorProtected(GameSession session, PlayerState player, string cardId)
            => player.EmperorProtectedCardIds.Contains(cardId);

        public static void ReturnCardToDeckTop(GameSession session, string cardId)
        {
            session.GetCard(cardId)?.MoveTo(CardZone.Deck, -1);
            session.Board.CommonDeck.Push(cardId);
        }
    }
}
