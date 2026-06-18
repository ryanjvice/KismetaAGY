using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Controller for FatefulWager.uxml (#40).</summary>
    public class FatefulWagerController : ScreenController
    {
        public System.Action<string> OnPlace;   // arg: chosen sign id
        public System.Action OnSkip;

        static readonly string[] Signs = {
            "aries","taurus","gemini","cancer","leo","virgo",
            "libra","scorpio","sagittarius","capricorn","aquarius","pisces"
        };
        string _selected = "pisces";

        protected override void Wire()
        {
            foreach (var s in Signs)
            {
                var sign = s;
                var b = Btn($"sign-{s}");
                if (b != null) b.clicked += () => Select(sign);
            }
            Btn("place-btn").clicked += () => OnPlace?.Invoke(_selected);
            Btn("skip-btn").clicked  += () => OnSkip?.Invoke();
        }

        void Select(string sign)
        {
            _selected = sign;
            foreach (var s in Signs)
                Btn($"sign-{s}")?.EnableInClassList("sign-cell--selected", s == sign);
        }
    }

    /// <summary>Controller for CardLimits.uxml (#42) + Transit gate (#44).</summary>
    public class CardLimitsController : ScreenController
    {
        public int HandLimit = 5;
        public System.Action OnTransit;

        protected override void Wire()
        {
            Btn("transit-btn").clicked += () =>
            {
                if (!Btn("transit-btn").ClassListContains("btn--disabled"))
                    OnTransit?.Invoke();
            };
        }

        /// <summary>Call after each discard to update the gate.</summary>
        public void Refresh(int handCount)
        {
            int over = handCount - HandLimit;
            var t = Btn("transit-btn");
            if (over > 0)
            {
                t.AddToClassList("btn--disabled");
                t.text = $"Discard {over} more to transit";
            }
            else
            {
                t.RemoveFromClassList("btn--disabled");
                t.AddToClassList("btn--primary");
                t.text = "Transit the age · pass the key";
            }
        }
    }

    /// <summary>Back-fill: controller for WinterHub.uxml (#41 and its sibling states).</summary>
    public class WinterHubController : ScreenController
    {
        public System.Action OnAdvance, OnOpenCardTable;

        protected override void Wire()
        {
            // the active-step CTA varies by hub state; the UXML names it per state.
            // Common names: limits-btn (post-wager), wager-btn/skip (post-unlock),
            // transit-btn (post-limits). Wire whichever exists.
            HookIfPresent("limits-btn");
            HookIfPresent("transit-btn");
            HookIfPresent("continue-btn");
            Btn("menu-btn").clicked += () => OnOpenCardTable?.Invoke();
        }

        void HookIfPresent(string name)
        {
            var b = Btn(name);
            if (b != null) b.clicked += () => OnAdvance?.Invoke();
        }
    }
}
