using UnityEngine.UIElements;

namespace Kismeta.UI
{
    public enum Season { Spring, Summer, Autumn, Winter }

    /// <summary>
    /// Shared controller for the four season-intro screens. Attach to whichever
    /// intro UXML; they all expose a single "begin-btn". Set Season + OnBegin.
    /// </summary>
    public class SeasonIntroController : ScreenController
    {
        public Season Season;
        public System.Action OnBegin;

        protected override void Wire()
        {
            Btn("begin-btn").clicked += () => OnBegin?.Invoke();
        }

        // Bind() could inject the live age strip (current sign/aspects) and,
        // for state-aware intros (e.g. Autumn exposure), tailor the tip text.
    }

    /// <summary>
    /// Controller for AgeOpening.uxml. Populate the cast age + effect + favors,
    /// then hand to Spring on enter.
    /// </summary>
    public class AgeOpeningController : ScreenController
    {
        public System.Action OnEnter;

        // Call after the Agekeeper's die settles, before showing this screen.
        public void SetAge(string name, string planet, string element,
                           string effectName, string effectDesc, string agekeeper)
        {
            Lbl("age-name").text = name.ToUpper();
            Lbl("age-planet").text = planet;
            Lbl("age-element").text = element;
            Lbl("effect-name").text = effectName;
            Lbl("effect-desc").text = effectDesc;
            Lbl("agekeeper-line").text =
                $"{agekeeper} serves as Agekeeper — they cast the age and hold the key this round.";
        }

        protected override void Wire()
        {
            Btn("enter-btn").clicked += () => OnEnter?.Invoke();
        }
    }

    /// <summary>
    /// Controller for AgeClosing.uxml. Populate standings recap + next keeper.
    /// </summary>
    public class AgeClosingController : ScreenController
    {
        public System.Action OnNextAge;

        public void SetClose(string ageName, string nextKeeper)
        {
            Lbl("age-name").text = ageName.ToUpper();
            Lbl("keypass-line").text =
                $"The key passes to {nextKeeper}. They will cast the next age — its sign is unknown until the dice fall.";
            // Rebuild #standings-list from the round's events in your own code.
        }

        protected override void Wire()
        {
            Btn("next-age-btn").clicked += () => OnNextAge?.Invoke();
        }
    }
}

namespace Kismeta.UI
{
    /// <summary>
    /// Controller for RoundOpen.uxml — the Agekeeper's Cosmic Age die roll.
    /// Drives the die animation, then reports the rolled sign so the caller can
    /// resolve any pending wager and push the AgeOpening reveal.
    /// </summary>
    public class RoundOpenController : ScreenController
    {
        public System.Action OnRolled;     // fired after the die settles
        public bool HasPendingWager = false;

        protected override void Wire()
        {
            Btn("roll-btn").clicked += Roll;
        }

        protected override void Bind()
        {
            // hide the pending-wager block if nothing is staked
            var pending = El("pending-wager");
            if (pending != null)
                pending.style.display = HasPendingWager ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Roll()
        {
            // NATIVE: start die tumble coroutine; on settle, set die-face and call OnRolled.
            // Placeholder: immediately report.
            OnRolled?.Invoke();
        }

        public void SetDieFace(string faceGlyph) => Lbl("die-face").text = faceGlyph;
        public void SetKeeper(string name) => Lbl("keeper-eyebrow").text = $"{name} casts the age";
    }
}
