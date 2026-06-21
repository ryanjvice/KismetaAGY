using System.Collections;
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
    }
}
