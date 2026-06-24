using System;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Manages shared contest overlays (Trade, Duel, Gambit, Opposition) as full-screen modals.
    /// </summary>
    public sealed class ContestOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _trade;
        VisualTreeAsset? _duel;
        VisualTreeAsset? _gambit;
        VisualTreeAsset? _opposition;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;

        TradeController? _tradeCtrl;
        DuelController? _duelCtrl;
        GambitController? _gambitCtrl;
        OppositionController? _oppositionCtrl;

        int? _pendingDuelRival;
        int? _pendingGambitRival;
        int? _pendingTradeRival;

        enum ActiveContest { None, Trade, Duel, Gambit, Opposition }
        ActiveContest _active = ActiveContest.None;

        public bool IsOpen => _active != ActiveContest.None
            && _layout != null && _layout.IsOverlayVisible;

        public int? ActiveSummerGroupIndex => _active is ActiveContest.Trade or ActiveContest.Duel or ActiveContest.Gambit
            ? 1
            : null;

        public int? ActiveAutumnGroupIndex => _active == ActiveContest.Opposition ? 2 : null;

        public Action? OverlayChanged;

        void Awake() => EnsureControllers();

        public void Configure(
            VisualTreeAsset trade,
            VisualTreeAsset duel,
            VisualTreeAsset gambit,
            VisualTreeAsset opposition)
        {
            _trade = trade;
            _duel = duel;
            _gambit = gambit;
            _opposition = opposition;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _tradeCtrl ??= GetComponent<TradeController>();
            _duelCtrl ??= GetComponent<DuelController>();
            _gambitCtrl ??= GetComponent<GambitController>();
            _oppositionCtrl ??= GetComponent<OppositionController>();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
        }

        public void DismissIfNotHumanTurn()
        {
            if (_bridge != null && _bridge.CanSubmit) return;
            Dismiss();
        }

        public void ShowTrade(int? rivalId = null)
        {
            EnsureControllers();
            _pendingTradeRival = rivalId;
            ShowContest(_trade, _tradeCtrl, WireTrade, ActiveContest.Trade);
        }

        public void ShowDuel(int? rivalId = null)
        {
            EnsureControllers();
            _pendingDuelRival = rivalId;
            ShowContest(_duel, _duelCtrl, WireDuel, ActiveContest.Duel);
        }

        public void ShowGambit(int? rivalId = null)
        {
            EnsureControllers();
            _pendingGambitRival = rivalId;
            ShowContest(_gambit, _gambitCtrl, WireGambit, ActiveContest.Gambit);
        }

        public void ShowOpposition()
        {
            EnsureControllers();
            ShowContest(_opposition, _oppositionCtrl, WireOpposition, ActiveContest.Opposition);
        }

        public void Dismiss()
        {
            _active = ActiveContest.None;
            _tradeCtrl?.Detach();
            _duelCtrl?.Detach();
            _gambitCtrl?.Detach();
            _oppositionCtrl?.Detach();
            _layout?.DismissOverlay();
            NotifyOverlayChanged();
        }

        void ShowContest<T>(VisualTreeAsset? asset, T? controller, System.Action wire, ActiveContest kind)
            where T : OverlayController
        {
            EnsureControllers();
            if (_layout == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: ViewportLayout missing.");
                return;
            }
            if (asset == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: UXML asset not assigned.");
                return;
            }
            if (controller == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: controller missing on bootstrap.");
                return;
            }
            _layout.ShowModal(asset);
            var root = _layout.OverlayContentRoot;
            if (root == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: overlay content root missing.");
                return;
            }
            controller.AttachTo(root);
            _active = kind;
            wire();
            RefreshOpenOverlay();
            NotifyOverlayChanged();
        }

        void NotifyOverlayChanged() => OverlayChanged?.Invoke();

        void RefreshOpenOverlay()
        {
            if (_session == null || _bridge == null) return;
            switch (_active)
            {
                case ActiveContest.Trade:
                    _tradeCtrl?.SetPreselectedRival(_pendingTradeRival);
                    _pendingTradeRival = null;
                    _tradeCtrl?.BindState(_session, _bridge);
                    break;
                case ActiveContest.Duel:
                    _duelCtrl?.SetPreselectedRival(_pendingDuelRival);
                    _pendingDuelRival = null;
                    _duelCtrl?.BindState(_session, _bridge);
                    break;
                case ActiveContest.Gambit:
                    _gambitCtrl?.SetPreselectedRival(_pendingGambitRival);
                    _pendingGambitRival = null;
                    _gambitCtrl?.BindState(_session, _bridge);
                    break;
                case ActiveContest.Opposition: _oppositionCtrl?.BindState(_session, _bridge); break;
            }
        }

        void WireTrade()
        {
            if (_tradeCtrl == null) return;
            _tradeCtrl.OnBack = Dismiss;
            _tradeCtrl.OnCompleted = Dismiss;
        }

        void WireDuel()
        {
            if (_duelCtrl == null) return;
            _duelCtrl.OnBack = Dismiss;
            _duelCtrl.OnCompleted = Dismiss;
        }

        void WireGambit()
        {
            if (_gambitCtrl == null) return;
            _gambitCtrl.OnBack = Dismiss;
            _gambitCtrl.OnCompleted = Dismiss;
        }

        void WireOpposition()
        {
            if (_oppositionCtrl == null) return;
            _oppositionCtrl.OnBack = Dismiss;
            _oppositionCtrl.OnCompleted = Dismiss;
        }
    }
}
