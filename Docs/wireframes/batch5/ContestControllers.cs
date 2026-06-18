using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Shared helpers for the contest wizards: advancing the step-dot rail and
    /// animating a die. Inherit ContestController for the common plumbing.
    /// </summary>
    public abstract class ContestController : ScreenController
    {
        protected void ShowStep(string panelName, params string[] allPanels)
        {
            foreach (var p in allPanels)
            {
                var el = El(p);
                if (el != null) el.style.display = (p == panelName) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        protected void SetWizard(string prefix, int total, int activeIndex)
        {
            for (int i = 1; i <= total; i++)
            {
                var dot = El($"{prefix}-{i}");
                if (dot == null) continue;
                dot.RemoveFromClassList("wizard-dot--active");
                dot.RemoveFromClassList("wizard-dot--done");
                if (i < activeIndex) dot.AddToClassList("wizard-dot--done");
                else if (i == activeIndex) dot.AddToClassList("wizard-dot--active");
            }
        }

        /// <summary>
        /// Animate a die label through random faces, settling on `result`.
        /// NATIVE: replace the glyph swap with sprite frames or a 3D die.
        /// </summary>
        protected IEnumerator RollDie(Label pip, int result, int frames = 12, float step = 0.07f)
        {
            for (int i = 0; i < frames; i++)
            {
                pip.text = Random.Range(1, 7).ToString();
                yield return new WaitForSeconds(step);
            }
            pip.text = result.ToString();
        }
    }

    /// <summary>Controller for Duel.uxml (#47).</summary>
    public class DuelController : ContestController
    {
        public System.Action<bool> OnResolved;   // arg: didWin
        static readonly string[] Panels = { "step-target", "step-ante", "step-roll" };

        protected override void Wire()
        {
            Btn("target-next").clicked += () => { ShowStep("step-ante", Panels); SetWizard("wd", 3, 2); };
            Btn("ante-next").clicked   += () => { ShowStep("step-roll", Panels); SetWizard("wd", 3, 3); };
            Btn("roll-btn").clicked    += DoRoll;
            Btn("back-btn").clicked    += () => OnResolved?.Invoke(false);
        }

        void DoRoll()
        {
            int you = Random.Range(1, 7), foe = Random.Range(1, 7);
            while (you == foe) foe = Random.Range(1, 7); // ties reroll
            StartCoroutine(Resolve(you, foe));
        }

        IEnumerator Resolve(int you, int foe)
        {
            yield return RollDie(Lbl("die-you-pip"), you);
            yield return RollDie(Lbl("die-foe-pip"), foe, 8);
            bool win = you > foe;
            var outcome = Lbl("roll-outcome");
            outcome.style.display = DisplayStyle.Flex;
            outcome.text = win ? "You win — take the card." : "You lose — your ante returns to the deck.";
            OnResolved?.Invoke(win);
        }
    }

    /// <summary>Controller for Gambit.uxml (#48). Skips the fee step if unwarded.</summary>
    public class GambitController : ContestController
    {
        public System.Action<bool> OnResolved;
        public bool TargetWarded = true;
        public int Fee = 2;
        readonly ReagentStepper _fee = new();
        static readonly string[] Panels = { "step-target", "step-fee", "step-stake", "step-roll" };

        protected override void Wire()
        {
            _fee.Cap = Fee; _fee.SetHave("salt", 3);

            Btn("g-target-next").clicked += () =>
            {
                if (TargetWarded) { ShowStep("step-fee", Panels); SetWizard("wg", 4, 2); }
                else { ShowStep("step-stake", Panels); SetWizard("wg", 4, 3); }
            };
            Btn("fee-salt-plus").clicked  += () => { if (_fee.TryAdd("salt")) RefreshFee(); };
            Btn("fee-salt-minus").clicked += () => { if (_fee.TryRemove("salt")) RefreshFee(); };
            Btn("g-fee-next").clicked += () =>
            {
                if (_fee.Total >= Fee) { ShowStep("step-stake", Panels); SetWizard("wg", 4, 3); }
            };
            Btn("g-stake-next").clicked += () => { ShowStep("step-roll", Panels); SetWizard("wg", 4, 4); };
            Btn("g-roll-btn").clicked   += DoRoll;
            Btn("back-btn").clicked     += () => OnResolved?.Invoke(false);
        }

        void RefreshFee()
        {
            Lbl("fee-salt-val").text = _fee.Of("salt").ToString();
            Lbl("fee-count").text = $"{_fee.Total} / {Fee}";
            var b = Btn("g-fee-next");
            if (_fee.Total >= Fee) { b.RemoveFromClassList("btn--disabled"); b.AddToClassList("btn--primary"); b.text = "Pay the fee · continue"; }
            else { b.AddToClassList("btn--disabled"); b.text = $"Pay {Fee - _fee.Total} more"; }
        }

        void DoRoll()
        {
            int you = Random.Range(1, 7), foe = Random.Range(1, 7);
            while (you == foe) foe = Random.Range(1, 7);
            StartCoroutine(Resolve(you, foe));
        }

        IEnumerator Resolve(int you, int foe)
        {
            yield return RollDie(Lbl("die-you-pip"), you);
            yield return RollDie(Lbl("die-foe-pip"), foe, 8);
            OnResolved?.Invoke(you > foe);
        }
    }

    /// <summary>Controller for Opposition.uxml (#49) + Stasis result (#33).</summary>
    public class OppositionController : ContestController
    {
        public System.Action<bool> OnResolved;
        public int Fee = 2;
        readonly ReagentStepper _fee = new();
        static readonly string[] Panels = { "step-fee", "step-roll", "step-tally", "stasis-result" };

        protected override void Wire()
        {
            _fee.Cap = Fee; _fee.SetHave("vitriol", 2);

            Btn("o-fee-plus").clicked  += () => { if (_fee.TryAdd("vitriol")) RefreshFee(); };
            Btn("o-fee-minus").clicked += () => { if (_fee.TryRemove("vitriol")) RefreshFee(); };
            Btn("o-fee-next").clicked  += () =>
            {
                if (_fee.Total >= Fee) { ShowStep("step-roll", Panels); SetWizard("wo", 3, 2); }
            };
            Btn("o-roll-btn").clicked += DoSetSign;
            Btn("o-resolve-btn").clicked += Resolve;
            Btn("stasis-done-btn").clicked += () => OnResolved?.Invoke(true);
            Btn("back-btn").clicked += () => OnResolved?.Invoke(false);
        }

        void RefreshFee()
        {
            Lbl("o-fee-val").text = _fee.Of("vitriol").ToString();
            Lbl("fee-count").text = $"{_fee.Total} / {Fee}";
            var b = Btn("o-fee-next");
            if (_fee.Total >= Fee) { b.RemoveFromClassList("btn--disabled"); b.AddToClassList("btn--primary"); b.text = "Pay the fee · roll"; }
            else { b.AddToClassList("btn--disabled"); b.text = $"Pay {Fee - _fee.Total} more"; }
        }

        void DoSetSign()
        {
            int face = Random.Range(1, 7);
            StartCoroutine(SetSign(face));
        }

        IEnumerator SetSign(int face)
        {
            yield return RollDie(Lbl("die-set-pip"), face);
            Lbl("set-sign").style.display = DisplayStyle.Flex;
            Lbl("set-sign").text = "your sign is set — see the tally";
            ShowStep("step-tally", Panels); SetWizard("wo", 3, 3);
        }

        void Resolve()
        {
            // higher TOTAL wins (computed from the tally in real code)
            int you = int.Parse(Lbl("you-total").text);
            int foe = int.Parse(Lbl("foe-total").text);
            if (you > foe) ShowStep("stasis-result", Panels);
            else OnResolved?.Invoke(false);
        }
    }

    /// <summary>Controller for Trade.uxml (#26) — proposes, needs consent.</summary>
    public class TradeController : ScreenController
    {
        public System.Action OnPropose, OnBack;
        protected override void Wire()
        {
            Btn("propose-btn").clicked += () => OnPropose?.Invoke();
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            // NATIVE: tapping give/get cards adjusts the trays + ratio line.
        }
    }
}
