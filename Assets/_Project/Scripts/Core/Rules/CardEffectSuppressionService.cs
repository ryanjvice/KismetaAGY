using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Star resonant nullification — suppresses ongoing Fate/Adept card effects.</summary>
    public static class CardEffectSuppressionService
    {
        public static bool IsSuppressed(GameSession session, string cardInstanceId)
        {
            var inst = session.GetCard(cardInstanceId);
            return inst?.IsEffectSuppressed == true;
        }

        public static void Suppress(GameSession session, string cardInstanceId)
        {
            var inst = session.GetCard(cardInstanceId);
            if (inst == null)
                return;

            inst.SetEffectSuppressed(true);

            var db = session.Rules?.CardDatabase;
            var def = db?.GetById(inst.DefinitionId);
            if (def?.MajorArcanaType == MajorArcanaType.Fate && def.ArcanaNumber == 11)
            {
                var effects = session.Board.ContestEffects;
                effects.DuelBestOfThree = false;
                effects.GambitBestOfThree = false;
                session.Board.ContestEffects = effects;
            }
        }

        public static void ClearSuppression(GameSession session, string cardInstanceId)
        {
            var inst = session.GetCard(cardInstanceId);
            if (inst == null)
                return;

            inst.SetEffectSuppressed(false);

            var db = session.Rules?.CardDatabase;
            var def = db?.GetById(inst.DefinitionId);
            if (def?.MajorArcanaType == MajorArcanaType.Fate && def.ArcanaNumber == 11)
            {
                var effects = session.Board.ContestEffects;
                effects.DuelBestOfThree = true;
                effects.GambitBestOfThree = true;
                session.Board.ContestEffects = effects;
            }
        }
    }
}
