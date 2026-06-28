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
        public Action OnWard;
        public Action OnTrade;
        public Action OnDuel;
        public Action OnGambit;
        public Action OnClose;

        GameSession? _session;
        CommandBridge? _bridge;

        Action? _onCraft;
        Action? _onWard;
        Action? _onTrade;
        Action? _onDuel;
        Action? _onGambit;
        Action? _onCbClose;
        Action? _onConClose;

        protected override void Wire()
        {
            _onCraft = () => OnCraft?.Invoke();
            _onWard = () => OnWard?.Invoke();
            _onTrade = () => TryInvokeConsortRow("act-trade", OnTrade);
            _onDuel = () => TryInvokeConsortRow("act-duel", OnDuel);
            _onGambit = () => TryInvokeConsortRow("act-gambit", OnGambit);
            _onCbClose = () => OnClose?.Invoke();
            _onConClose = () => OnClose?.Invoke();

            WireBtn("act-craft", _onCraft);
            WireBtn("act-ward", _onWard);
            WireBtn("act-trade", _onTrade);
            WireBtn("act-duel", _onDuel);
            WireBtn("act-gambit", _onGambit);
            WireBtn("cb-close", _onCbClose);
            WireBtn("con-close", _onConClose);
        }

        protected override void Unwire()
        {
            UnwireBtn("act-craft", _onCraft);
            UnwireBtn("act-ward", _onWard);
            UnwireBtn("act-trade", _onTrade);
            UnwireBtn("act-duel", _onDuel);
            UnwireBtn("act-gambit", _onGambit);
            UnwireBtn("cb-close", _onCbClose);
            UnwireBtn("con-close", _onConClose);
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
            btn.pickingMode = PickingMode.Position;
            if (enabled)
                btn.RemoveFromClassList("btn--disabled");
            else
                btn.AddToClassList("btn--disabled");
        }

        void TryInvokeConsortRow(string name, Action? action)
        {
            var btn = Btn(name);
            if (btn != null && !btn.enabledSelf)
                return;
            action?.Invoke();
        }

        void WireBtn(string name, Action? handler)
        {
            var btn = Btn(name);
            if (btn != null && handler != null)
                btn.clicked += handler;
        }

        void UnwireBtn(string name, Action? handler)
        {
            var btn = Btn(name);
            if (btn != null && handler != null)
                btn.clicked -= handler;
        }
    }
}
