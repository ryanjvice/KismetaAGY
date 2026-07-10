using System;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
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
        VisualTreeAsset? _contestResponse;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;

        TradeController? _tradeCtrl;
        DuelController? _duelCtrl;
        GambitController? _gambitCtrl;
        OppositionController? _oppositionCtrl;
        ContestResponseController? _responseCtrl;

        int? _pendingDuelRival;
        int? _pendingGambitRival;
        int? _pendingTradeRival;
        GameLoop? _pendingResponseLoop;

        enum ActiveContest { None, Trade, Duel, Gambit, Opposition, Response }
        ActiveContest _active = ActiveContest.None;

        public bool IsOpen => _active != ActiveContest.None
            && _layout != null && _layout.IsOverlayVisible;

        public bool IsResponseActive => _active == ActiveContest.Response;

        public bool IsDefenderRollInProgress =>
            IsResponseActive && (_responseCtrl?.IsDuelRollInProgress ?? false);

        public bool IsAwaitingDuelContinue =>
            IsResponseActive && (_responseCtrl?.IsAwaitingDuelContinue ?? false);

        public bool IsDefenderDuelUiPending => IsDefenderRollInProgress || IsAwaitingDuelContinue;

        public bool IsAttackerDuelUiPending =>
            _active == ActiveContest.Duel
            && (_duelCtrl != null && (_duelCtrl.IsDuelRollInProgress || _duelCtrl.IsAwaitingDuelContinue));

        public bool IsContestDuelUiPending => IsDefenderDuelUiPending || IsAttackerDuelUiPending;

        public System.Action? OnResponseCompleted;

        public int? ActiveSummerGroupIndex => _active switch
        {
            ActiveContest.Trade => 0,
            ActiveContest.Duel => 1,
            ActiveContest.Gambit => 2,
            _ => null
        };

        public int? ActiveAutumnGroupIndex => _active switch
        {
            ActiveContest.Opposition => 3,
            _ => null
        };

        public string? ActiveNarrativeStepId => _active switch
        {
            ActiveContest.Trade => "summer.trade",
            ActiveContest.Duel => "summer.duel",
            ActiveContest.Gambit => "summer.gambit",
            ActiveContest.Opposition => "autumn.opposition",
            _ => null
        };

        public Action? OverlayChanged;

        void Awake() => EnsureControllers();

        public void Configure(
            VisualTreeAsset trade,
            VisualTreeAsset duel,
            VisualTreeAsset gambit,
            VisualTreeAsset opposition,
            VisualTreeAsset contestResponse)
        {
            _trade = trade;
            _duel = duel;
            _gambit = gambit;
            _opposition = opposition;
            _contestResponse = contestResponse;
            EnsureControllers();

            if (_contestResponse != null
                && !_contestResponse.name.StartsWith("ContestResponse"))
            {
                Debug.LogWarning(
                    $"[ContestOverlayHost] Expected ContestResponse.uxml but got '{_contestResponse.name}'. " +
                    "Run Kismeta → UI → Wire Bootstrap UI.");
            }
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _tradeCtrl ??= GetComponent<TradeController>();
            _duelCtrl ??= GetComponent<DuelController>();
            _gambitCtrl ??= GetComponent<GambitController>();
            _oppositionCtrl ??= GetComponent<OppositionController>();
            _responseCtrl ??= GetComponent<ContestResponseController>();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
        }

        public void DismissIfNotHumanTurn()
        {
            if (_bridge != null && _bridge.CanSubmit) return;
            if (IsContestDuelUiPending) return;
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

        public bool ShowContestResponse(GameLoop loop)
        {
            EnsureControllers();
            var asset = _contestResponse ?? _responseCtrl?.UxmlAsset;
            _pendingResponseLoop = loop;
            if (!ShowContest(asset, _responseCtrl, WireResponse, ActiveContest.Response))
            {
                _pendingResponseLoop = null;
                return false;
            }

            return true;
        }

        public void Dismiss()
        {
            _active = ActiveContest.None;
            _tradeCtrl?.Detach();
            _duelCtrl?.Detach();
            _gambitCtrl?.Detach();
            _oppositionCtrl?.Detach();
            _responseCtrl?.Detach();
            _layout?.DismissOverlay();
            NotifyOverlayChanged();
        }

        /// <summary>Clears contest active state when another overlay replaced ours on the shared layer.</summary>
        public void ReleaseStaleActiveState()
        {
            if (_active == ActiveContest.None)
                return;
            _active = ActiveContest.None;
            _pendingResponseLoop = null;
            _tradeCtrl?.Detach();
            _duelCtrl?.Detach();
            _gambitCtrl?.Detach();
            _oppositionCtrl?.Detach();
            _responseCtrl?.Detach();
            NotifyOverlayChanged();
        }

        bool ShowContest<T>(VisualTreeAsset? asset, T? controller, System.Action wire, ActiveContest kind)
            where T : OverlayController
        {
            EnsureControllers();
            if (_layout == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: ViewportLayout missing.");
                return false;
            }
            if (asset == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: UXML asset not assigned.");
                return false;
            }
            if (controller == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: controller missing on bootstrap.");
                return false;
            }
            _layout.ShowModal(asset);
            var root = ResolveAttachRoot(_layout, kind);
            if (root == null)
            {
                Debug.LogWarning($"[ContestOverlayHost] Cannot show {kind}: overlay content root missing.");
                _layout.DismissOverlay();
                return false;
            }

            if (kind == ActiveContest.Response
                && root.Q<Button>("accept-btn") == null
                && root.Q<Button>("roll-btn") == null)
            {
                Debug.LogError(
                    $"[ContestOverlayHost] ContestResponse clone is missing #accept-btn and #roll-btn. " +
                    $"Asset='{asset.name}', attachRoot='{root.name}', cloneChildren={root.childCount}. " +
                    "If cloneChildren is 0, check the Unity console for UXML import errors on ContestResponse.uxml.");
                _layout.DismissOverlay();
                return false;
            }

            controller.AttachTo(root);
            _active = kind;
            if (kind == ActiveContest.Response)
                _layout.ApplyBoundedOverlaySheet();
            wire();
            RefreshOpenOverlay();
            NotifyOverlayChanged();
            return true;
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
                case ActiveContest.Response:
                    if (_pendingResponseLoop != null)
                        _responseCtrl?.BindState(_session, _bridge, _pendingResponseLoop);
                    _pendingResponseLoop = null;
                    break;
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
            _duelCtrl.OnCompleted = () =>
            {
                ReleaseStaleActiveState();
                OnResponseCompleted?.Invoke();
            };
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

        void WireResponse()
        {
            if (_responseCtrl == null) return;
            _responseCtrl.OnCompleted = () =>
            {
                ReleaseStaleActiveState();
                OnResponseCompleted?.Invoke();
            };
        }

        static VisualElement? ResolveAttachRoot(ViewportLayout layout, ActiveContest kind)
        {
            var host = layout.OverlayCloneHost ?? layout.OverlayContentRoot?.parent;
            var root = layout.OverlayContentRoot;

            if (kind == ActiveContest.Response)
            {
                var response = FindNamedDescendant(host, "contest-response")
                    ?? FindNamedDescendant(root, "contest-response");
                if (response != null) return response;
            }

            if (root == null) return null;

            if (root.name == "contest-response" || root.ClassListContains("contest-response"))
                return root;

            if (root.ClassListContains("screen") || root.ClassListContains("sheet"))
                return root;

            return root.Q<VisualElement>(className: "sheet")
                ?? root.Q<VisualElement>(className: "screen")
                ?? root;
        }

        static VisualElement? FindNamedDescendant(VisualElement? parent, string elementName)
        {
            if (parent == null) return null;
            if (parent.name == elementName) return parent;
            return parent.Q<VisualElement>(elementName);
        }
    }
}
