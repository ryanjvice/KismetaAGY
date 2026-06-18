using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Controller for Victory.uxml (#50).</summary>
    public class VictoryController : ScreenController
    {
        public System.Action OnChronicle, OnNewGame;
        protected override void Wire()
        {
            Btn("chronicle-btn").clicked += () => OnChronicle?.Invoke();
            Btn("newgame-btn").clicked += () => OnNewGame?.Invoke();
        }
        public void SetWinner(string name) =>
            Lbl("winner-name").text = $"{name} has completed the Great Work";
    }

    /// <summary>Controller for Chronicle.uxml (#51).</summary>
    public class ChronicleController : ScreenController
    {
        public System.Action OnBack, OnNewGame;
        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            Btn("newgame-btn").clicked += () => OnNewGame?.Invoke();
        }
        // NATIVE: render the race chart into #chart-host (Painter2D / VectorGraphics).
    }

    /// <summary>Controller for CardTable.uxml (#52) — the public-board overlay.</summary>
    public class CardTableController : ScreenController
    {
        public System.Action OnClose;
        public System.Action<string> OnDuel, OnGambit, OnTrade;
        public System.Action<string> OnSort;   // "threat" | "turn" | "arcanum"

        protected override void Wire()
        {
            Btn("close-btn").clicked += () => OnClose?.Invoke();

            HookSort("sort-threat", "threat");
            HookSort("sort-turn", "turn");
            HookSort("sort-arcanum", "arcanum");

            // Per-rival action buttons are named {action}-{rival}; wire them as you
            // build the player blocks in C#. Example for the sample rival:
            HookAction("duel-iolanthe", "Iolanthe", OnDuel);
            HookAction("gambit-iolanthe", "Iolanthe", OnGambit);
            HookAction("trade-iolanthe", "Iolanthe", OnTrade);
        }

        void HookSort(string btn, string key)
        {
            Btn(btn)?.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var (n, _) in new[] { ("sort-threat", 0), ("sort-turn", 0), ("sort-arcanum", 0) })
                    Btn(n)?.EnableInClassList("table-action--active", n == btn);
                OnSort?.Invoke(key);
            });
        }

        void HookAction(string btnName, string rival, System.Action<string> handler)
        {
            var b = Btn(btnName);
            if (b != null) b.clicked += () => handler?.Invoke(rival);
        }
    }

    /// <summary>
    /// Controller for CardModals.uxml (#53-55). Shows one of the three modals
    /// and wires its buttons. The Adept offers place/hold; the Fate only accepts.
    /// </summary>
    public class CardModalsController : ScreenController
    {
        public System.Action OnInspectDone;
        public System.Action OnAdeptPlace, OnAdeptHold;
        public System.Action OnFateAccept;

        public enum Modal { Inspect, Adept, Fate }

        protected override void Wire()
        {
            Btn("inspect-close").clicked += () => OnInspectDone?.Invoke();
            Btn("inspect-done").clicked  += () => OnInspectDone?.Invoke();
            Btn("adept-place").clicked   += () => OnAdeptPlace?.Invoke();
            Btn("adept-hold").clicked    += () => OnAdeptHold?.Invoke();
            Btn("fate-accept").clicked   += () => OnFateAccept?.Invoke();
        }

        /// <summary>Show exactly one modal.</summary>
        public void Show(Modal which)
        {
            El("inspect-modal").style.display = which == Modal.Inspect ? DisplayStyle.Flex : DisplayStyle.None;
            El("adept-modal").style.display   = which == Modal.Adept   ? DisplayStyle.Flex : DisplayStyle.None;
            El("fate-modal").style.display    = which == Modal.Fate    ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
