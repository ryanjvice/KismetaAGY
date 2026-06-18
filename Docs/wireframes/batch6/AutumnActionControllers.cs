using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Controller for FireStone.uxml (#31). Reuses ReagentStepper.</summary>
    public class FireStoneController : ScreenController
    {
        public System.Action OnFired, OnClose;
        public int Cost = 2;
        readonly ReagentStepper _pay = new();

        protected override void Wire()
        {
            _pay.Cap = Cost;
            _pay.SetHave("salt", 3);
            _pay.SetHave("vitriol", 2);

            Btn("fire-close").clicked += () => OnClose?.Invoke();
            HookReagent("salt");
            HookReagent("vitriol");
            Btn("fire-btn").clicked += () =>
            {
                if (_pay.Total >= Cost) OnFired?.Invoke();
            };
            Refresh();
        }

        void HookReagent(string key)
        {
            Btn($"fire-{key}-plus").clicked  += () => { if (_pay.TryAdd(key)) Refresh(); };
            Btn($"fire-{key}-minus").clicked += () => { if (_pay.TryRemove(key)) Refresh(); };
        }

        void Refresh()
        {
            Lbl("fire-salt-val").text = _pay.Of("salt").ToString();
            Lbl("fire-vitriol-val").text = _pay.Of("vitriol").ToString();
            Lbl("pay-count").text = $"{_pay.Total} / {Cost}";

            var b = Btn("fire-btn");
            if (_pay.Total >= Cost)
            {
                b.RemoveFromClassList("btn--disabled"); b.AddToClassList("btn--primary");
                b.text = "Fire — advance to position 6";
            }
            else
            {
                b.AddToClassList("btn--disabled"); b.RemoveFromClassList("btn--primary");
                b.text = $"Spend {Cost - _pay.Total} more to advance";
            }
        }
    }

    /// <summary>Controller for TemperStone.uxml (#32).</summary>
    public class TemperStoneController : ScreenController
    {
        public System.Action OnTempered, OnClose;
        public bool IsWinningMove = true;

        protected override void Wire()
        {
            Btn("temper-close").clicked += () => OnClose?.Invoke();
            Btn("card-d").clicked += SelectCard;
            Btn("temper-btn").clicked += () =>
            {
                if (!Btn("temper-btn").ClassListContains("btn--disabled")) OnTempered?.Invoke();
            };
        }

        void SelectCard()
        {
            Btn("card-d").style.borderColor = new StyleColor(new UnityEngine.Color(0.8f, 0.69f, 0.88f));
            var b = Btn("temper-btn");
            b.RemoveFromClassList("btn--disabled"); b.AddToClassList("btn--primary");
            b.text = IsWinningMove ? "Temper to the Altar · complete the Great Work" : "Temper · advance a stage";
        }
    }

    /// <summary>Controller for ManageCards.uxml (#30) — read-only review.</summary>
    public class ManageCardsController : ScreenController
    {
        public System.Action OnClose;
        protected override void Wire()
        {
            Btn("close-btn").clicked += () => OnClose?.Invoke();
            Btn("forge-btn").clicked += () => OnClose?.Invoke();
        }
        // Bind() would repopulate crucible pills / spread / hand / reagents from state.
    }

    /// <summary>Controller for LeaveStasis.uxml (#34) + result (#35).</summary>
    public class LeaveStasisController : ScreenController
    {
        public System.Action OnReturned, OnStay, OnWard, OnBack;
        public bool SpotOpen = true;
        public int SaltHeld = 3;

        protected override void Wire()
        {
            Btn("back-btn").clicked += () => OnBack?.Invoke();
            Btn("stay-btn").clicked += () => OnStay?.Invoke();
            Btn("pay-salt-btn").clicked += PaySalt;
            Btn("ward-btn").clicked += () => OnWard?.Invoke();
            Btn("done-btn").clicked += () => OnReturned?.Invoke();
        }

        protected override void Bind()
        {
            // verify salt cost
            if (Lbl("salt-check") != null)
                Lbl("salt-check").text = SaltHeld >= 2 ? $"you hold {SaltHeld} · ok" : $"only {SaltHeld} — short";
        }

        void PaySalt()
        {
            if (SaltHeld < 2) return;
            El("escape-view").style.display = DisplayStyle.None;
            El("result-view").style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>Folded in from MainSceneControllers: AutumnMainScene.uxml (#29).</summary>
    public class AutumnSceneController : ScreenController
    {
        public System.Action OnFire, OnTemper, OnOppose, OnManageCards, OnOpenCardTable;
        protected override void Wire()
        {
            Btn("fire-btn").clicked         += () => OnFire?.Invoke();
            Btn("temper-btn").clicked       += () => OnTemper?.Invoke();
            Btn("oppose-btn").clicked       += () => OnOppose?.Invoke();
            Btn("manage-cards-btn").clicked += () => OnManageCards?.Invoke();
            Btn("menu-btn").clicked         += () => OnOpenCardTable?.Invoke();
        }
    }
}
