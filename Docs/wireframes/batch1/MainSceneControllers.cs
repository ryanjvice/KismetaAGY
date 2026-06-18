using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Controllers for the five proof-of-pattern main scenes.
    /// These wire the action bars and menu; Bind() is where you'd push live
    /// game state (card strips, rival stats, step states) — left as hooks.
    /// </summary>

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

        // protected override void Bind() { /* rebuild #spread-strip, rival stats, your-stone-label */ }
    }

    public class SummerSceneController : ScreenController
    {
        public System.Action OnCraftBuild, OnConsort, OnActivate, OnOpenCardTable;

        protected override void Wire()
        {
            Btn("craftbuild-btn").clicked += () => OnCraftBuild?.Invoke();
            Btn("consort-btn").clicked    += () => OnConsort?.Invoke();
            Btn("activate-btn").clicked   += () => OnActivate?.Invoke();
            Btn("menu-btn").clicked       += () => OnOpenCardTable?.Invoke();
        }

        // protected override void Bind() { /* toggle cauldron lit/dormant, rebuild strip */ }
    }

    public class SpringHubController : ScreenController
    {
        public System.Action OnCommune, OnOpenCardTable;

        protected override void Wire()
        {
            Btn("commune-btn").clicked += () => OnCommune?.Invoke();
            Btn("menu-btn").clicked    += () => OnOpenCardTable?.Invoke();
        }

        // protected override void Bind() { /* set harvest-count, wheel meeple angle, harvest-strip */ }
    }

    public class WinterHubController : ScreenController
    {
        public System.Action OnLimits, OnOpenCardTable;

        protected override void Wire()
        {
            Btn("limits-btn").clicked += () => OnLimits?.Invoke();
            Btn("menu-btn").clicked   += () => OnOpenCardTable?.Invoke();
        }

        // protected override void Bind() { /* set bar widths, over-limit text, step rail states */ }
    }
}
