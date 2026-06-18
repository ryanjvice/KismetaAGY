using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Minimal card model the swap/limit controllers operate on. Replace with
    /// your real card type; only the fields used by the view are listed.
    /// </summary>
    public class CardVM
    {
        public string Rank;        // "7", "K", "A"
        public string Suit;        // "wands","cups","pentacles","swords"
        public int AlignValue;     // 0 if not aligned to current age, else +1/+2/+3
    }

    /// <summary>Controller for SpringRollHarvest.uxml (#11-12).</summary>
    public class SpringRollHarvestController : ScreenController
    {
        public System.Action OnRolled;
        public System.Action OnContinue;

        protected override void Wire()
        {
            Btn("roll-btn").clicked += DoRoll;
            Btn("continue-btn").clicked += () => OnContinue?.Invoke();
        }

        void DoRoll()
        {
            // NATIVE: animate the wheel; on settle, fill rolled-sign + tally, then:
            Btn("roll-btn").style.display = DisplayStyle.None;
            Btn("continue-btn").style.display = DisplayStyle.Flex;
            OnRolled?.Invoke();
        }

        public void SetHarvestTotal(int n) => Lbl("harvest-total").text = n.ToString();
    }

    /// <summary>
    /// Shared tap-to-swap controller for Commune (#14) and Winter Unlock (#39).
    /// Holds two card lists and rebuilds the chip strips on every move.
    /// </summary>
    public class TapSwapController : ScreenController
    {
        public List<CardVM> Spread = new();
        public List<CardVM> Hand = new();
        public System.Action OnLock;   // Commune: lock; Unlock: continue

        protected override void Wire()
        {
            var lockBtn = Btn("lock-btn");
            if (lockBtn != null) lockBtn.clicked += () => OnLock?.Invoke();
        }

        protected override void Bind() => Rebuild();

        void Rebuild()
        {
            RebuildZone("spread-cards", Spread, fromSpread: true);
            RebuildZone("hand-cards", Hand, fromSpread: false);

            if (Lbl("spread-count") != null) Lbl("spread-count").text = Spread.Count.ToString();
            if (Lbl("hand-count") != null) Lbl("hand-count").text = Hand.Count.ToString();
            UpdateReadout();
        }

        void RebuildZone(string container, List<CardVM> cards, bool fromSpread)
        {
            var zone = El(container);
            if (zone == null) return;
            zone.Clear();
            foreach (var c in cards)
            {
                var chip = MakeChip(c);
                chip.RegisterCallback<ClickEvent>(_ => Move(c, fromSpread));
                zone.Add(chip);
            }
        }

        void Move(CardVM card, bool fromSpread)
        {
            if (fromSpread) { Spread.Remove(card); Hand.Add(card); }
            else { Hand.Remove(card); Spread.Add(card); }
            Rebuild();
        }

        void UpdateReadout()
        {
            if (Lbl("spread-align") == null) return;
            int align = 0; foreach (var c in Spread) align += c.AlignValue;
            Lbl("spread-align").text = "+" + align;
            Lbl("hand-safe").text = Hand.Count.ToString();
            Lbl("verdict").text = align >= 4 ? "strong" : align >= 2 ? "balanced" : "guarded";
        }

        static VisualElement MakeChip(CardVM c)
        {
            var chip = new VisualElement();
            chip.AddToClassList("card-chip");
            chip.AddToClassList($"card-chip--{c.Suit}");
            if (c.AlignValue > 0) chip.AddToClassList("card-chip--selected");
            var rank = new Label(c.Rank);
            rank.AddToClassList("card-chip__rank");
            chip.Add(rank);
            if (c.AlignValue > 0)
            {
                var tag = new Label("+" + c.AlignValue);
                tag.AddToClassList("card-chip__tag");
                chip.Add(tag);
            }
            return chip;
        }
    }

    /// <summary>Back-fill: controller for SpringHub.uxml (#13).</summary>
    public class SpringHubController : ScreenController
    {
        public System.Action OnCommune, OnOpenCardTable;
        protected override void Wire()
        {
            Btn("commune-btn").clicked += () => OnCommune?.Invoke();
            Btn("menu-btn").clicked    += () => OnOpenCardTable?.Invoke();
        }
        public void SetHarvest(int n) => Lbl("harvest-count").text = n.ToString();
    }
}
