using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class TradeController : ContestController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _rivalId = -1;
        int? _preselectedRival;
        readonly HashSet<string> _give = new();
        readonly HashSet<string> _get = new();

        public System.Action? OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("propose-btn")!.clicked += OnCompleteTrade;
        }

        public void SetPreselectedRival(int? rivalId) => _preselectedRival = rivalId;

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            _rivalId = _preselectedRival ?? -1;
            _preselectedRival = null;
            _give.Clear();
            _get.Clear();

            if (Root == null || _playerId < 0) return;

            RefreshAll();
        }

        void RefreshAll()
        {
            if (_session == null || Root == null || _playerId < 0) return;

            ContestBindings.SetContestTitle(Root, "Trade", _rivalId, _session);
            ContestBindings.BuildRivalChips(El("rival-strip"), _session, _playerId,
                ContestBindings.RivalEligibleForSummerContest, _rivalId, id =>
                {
                    _rivalId = id;
                    _get.Clear();
                    RefreshAll();
                });

            var player = _session.Players[_playerId];
            var giveCards = ContestBindings.MinorSpreadCards(_session, player);
            ContestBindings.BuildCardChips(El("give-cards"), _session, giveCards, _give, true, _ => RefreshTrays());

            var getCards = _rivalId >= 0
                ? ContestBindings.PublicRivalSpread(_session, _rivalId)
                : new List<string>();
            ContestBindings.BuildCardChips(El("get-cards"), _session, getCards, _get, true, _ => RefreshTrays());

            if (Lbl("ratio-line") != null)
                Lbl("ratio-line")!.text = ContestBindings.TradeRatioHint(_session);

            RefreshTrays();
        }

        void RefreshTrays()
        {
            if (Lbl("give-count") != null)
                Lbl("give-count")!.text = _give.Count == 1 ? "1 card" : $"{_give.Count} cards";
            if (Lbl("get-count") != null)
                Lbl("get-count")!.text = _get.Count == 1 ? "1 card" : $"{_get.Count} cards";

            var btn = Btn("propose-btn");
            if (btn != null)
            {
                btn.text = "Complete trade";
                bool valid = _rivalId >= 0 && (_give.Count > 0 || _get.Count > 0);
                btn.SetEnabled(valid);
            }
        }

        void OnCompleteTrade()
        {
            if (_session == null || _bridge == null || _playerId < 0 || _rivalId < 0) return;
            if (_give.Count == 0 && _get.Count == 0) return;

            var offer = new List<string>(_give);
            var request = new List<string>(_get);
            if (_bridge.TrySubmit(new DirectTradeCommand(_playerId, _rivalId, offer, request)))
                OnCompleted?.Invoke();
        }
    }
}
