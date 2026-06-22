using System.Collections;
using Kismeta.Core.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Shared die tumble timing per style guide (~12 frames @ 70ms).</summary>
    public static class DieAnimator
    {
        public const int DefaultFrames = 12;
        public const float DefaultStepSec = 0.07f;

        public static IEnumerator RollLabel(Label? pip, int result, int frames = DefaultFrames,
            float stepSec = DefaultStepSec, int maxFace = 12)
        {
            if (pip == null) yield break;
            for (int i = 0; i < frames; i++)
            {
                pip.text = Random.Range(1, maxFace + 1).ToString();
                yield return new WaitForSeconds(stepSec);
            }
            pip.text = result.ToString();
        }

        public static IEnumerator RollZodiacLabel(Label? face, ZodiacSign result, int frames = DefaultFrames,
            float stepSec = DefaultStepSec)
        {
            if (face == null) yield break;
            SymbolGlyphs.TagEmoji(face);
            var signs = SymbolGlyphs.AllSigns;
            for (int i = 0; i < frames; i++)
            {
                face.text = SymbolGlyphs.Zodiac(signs[Random.Range(0, signs.Length)]);
                yield return new WaitForSeconds(stepSec);
            }
            face.text = SymbolGlyphs.Zodiac(result);
        }
    }
}
