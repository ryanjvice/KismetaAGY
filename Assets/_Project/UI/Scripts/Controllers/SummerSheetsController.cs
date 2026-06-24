using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerSheetsController : OverlayController
    {
        public Action OnCraft;
        public Action OnBuild;
        public Action OnWard;
        public Action OnTrade;
        public Action OnDuel;
        public Action OnGambit;
        public Action OnClose;

        GameSession? _session;
        CommandBridge? _bridge;

        protected override void Wire()
        {
            WireBtn("act-craft", () => OnCraft?.Invoke());
            WireBtn("act-build", () => OnBuild?.Invoke());
            WireBtn("act-ward", () => OnWard?.Invoke());
            WireBtn("act-trade", () => OnTrade?.Invoke());
            WireBtn("act-duel", () => OnDuel?.Invoke());
            WireBtn("act-gambit", () => OnGambit?.Invoke());
            WireBtn("cb-close", () => OnClose?.Invoke());
            WireBtn("con-close", () => OnClose?.Invoke());
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            RefreshConsortRows();
        }

        public void ShowCraftBuild(bool craftBuild)
        {
            var craft = El("craftbuild-sheet");
            var consort = El("consort-sheet");
            if (craft != null)
                craft.style.display = craftBuild ? DisplayStyle.Flex : DisplayStyle.None;
            if (consort != null)
                consort.style.display = craftBuild ? DisplayStyle.None : DisplayStyle.Flex;
            if (!craftBuild)
                RefreshConsortRows();
        }

        void RefreshConsortRows()
        {
            bool enabled = _bridge != null && _bridge.CanSubmit
                && _bridge.PendingHint == ActionHint.SummerAction;
            SetRowEnabled("act-trade", enabled);
            SetRowEnabled("act-duel", enabled);
            SetRowEnabled("act-gambit", enabled);
        }

        void SetRowEnabled(string name, bool enabled)
        {
            var btn = Btn(name);
            if (btn == null) return;
            btn.SetEnabled(enabled);
            if (enabled)
                btn.RemoveFromClassList("btn--disabled");
            else
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
