using System;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerSheetsController : OverlayController
    {
        public Action OnCraft;
        public Action OnLight;
        public Action OnBuild;
        public Action OnWard;
        public Action OnClose;

        protected override void Wire()
        {
            WireBtn("act-craft", () => OnCraft?.Invoke());
            WireBtn("act-light", () => OnLight?.Invoke());
            WireBtn("act-build", () => OnBuild?.Invoke());
            WireBtn("act-ward", () => OnWard?.Invoke());
            WireBtn("cb-close", () => OnClose?.Invoke());
            WireBtn("con-close", () => OnClose?.Invoke());
        }

        public void ShowCraftBuild(bool craftBuild)
        {
            var craft = El("craftbuild-sheet");
            var consort = El("consort-sheet");
            if (craft != null)
                craft.style.display = craftBuild ? DisplayStyle.Flex : DisplayStyle.None;
            if (consort != null)
                consort.style.display = craftBuild ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected override void Bind()
        {
            DisableRow("act-trade");
            DisableRow("act-duel");
            DisableRow("act-gambit");
        }

        void DisableRow(string name)
        {
            var btn = Btn(name);
            if (btn == null) return;
            btn.SetEnabled(false);
            btn.AddToClassList("btn--disabled");
        }

        void WireBtn(string name, Action handler)
        {
            var btn = Btn(name);
            if (btn != null)
                btn.clicked += handler;
        }
    }
}
