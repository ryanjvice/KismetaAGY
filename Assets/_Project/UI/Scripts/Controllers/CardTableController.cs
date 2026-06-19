using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CardTableController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        CardTableBindings.SortMode _sort = CardTableBindings.SortMode.Threat;

        public Action? OnClose;
        public Action<int>? OnDuel;
        public Action<int>? OnGambit;
        public Action<int>? OnTrade;
        public Action<string>? OnInspect;

        protected override void Wire()
        {
            Btn("close-btn")!.clicked += () => OnClose?.Invoke();
            HookSort("sort-threat", CardTableBindings.SortMode.Threat);
            HookSort("sort-turn", CardTableBindings.SortMode.Turn);
            HookSort("sort-arcanum", CardTableBindings.SortMode.Arcanum);
        }

        void HookSort(string btnName, CardTableBindings.SortMode mode)
        {
            Btn(btnName)?.RegisterCallback<ClickEvent>(_ =>
            {
                _sort = mode;
                foreach (var n in new[] { "sort-threat", "sort-turn", "sort-arcanum" })
                    Btn(n)?.EnableInClassList("table-action--active", n == btnName);
                RefreshTable();
            });
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            Btn("sort-threat")?.EnableInClassList("table-action--active", _sort == CardTableBindings.SortMode.Threat);
            RefreshTable();
        }

        void RefreshTable()
        {
            if (Root == null || _session == null || _bridge == null) return;
            int localId = SummerActionBindings.ResolvePlayerId(_session, _bridge);
            CardTableBindings.Populate(
                Root, _session, localId, _session.Phase.CurrentSeason, _sort,
                OnInspect, OnDuel, OnGambit, OnTrade);
        }
    }
}
