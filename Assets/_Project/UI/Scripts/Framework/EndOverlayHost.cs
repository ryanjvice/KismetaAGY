using System;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Manages Card Table and Card Modals overlays (Batch 7).
    /// </summary>
    public sealed class EndOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _cardTable;
        VisualTreeAsset? _cardModals;
        VisualTreeAsset? _activeEffects;
        VisualTreeAsset? _crucibleCardDetail;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;
        GameLoop? _loop;

        CardTableController? _table;
        CardModalsController? _modals;
        ActiveEffectsController? _activeEffectsCtrl;
        CrucibleCardDetailController? _crucibleDetail;

        enum ActiveOverlay { None, CardTable, CardModals, ActiveEffects, CrucibleDetail }
        ActiveOverlay _active = ActiveOverlay.None;
        bool _reopenCardTableAfterInspect;
        int _cardTableFocusPlayerId = -1;
        int _activeEffectsFocusPlayerId = -1;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        public Action<int>? OnDuelFromTable;
        public Action<int>? OnGambitFromTable;
        public Action<int>? OnTradeFromTable;
        public Action? OnOverlayDismissed;

        void Awake() => EnsureControllers();

        public void Configure(
            VisualTreeAsset cardTable,
            VisualTreeAsset cardModals,
            VisualTreeAsset? activeEffects = null,
            VisualTreeAsset? crucibleCardDetail = null)
        {
            _cardTable = cardTable;
            _cardModals = cardModals;
            _activeEffects = activeEffects;
            _crucibleCardDetail = crucibleCardDetail;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _table ??= GetComponent<CardTableController>();
            _modals ??= GetComponent<CardModalsController>();
            _activeEffectsCtrl ??= GetComponent<ActiveEffectsController>();
            _crucibleDetail ??= GetComponent<CrucibleCardDetailController>();
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
        }

        public void DismissIfNotHumanTurn()
        {
            if (_bridge != null && _bridge.CanSubmit) return;
            Dismiss();
        }

        public void ShowActiveEffects(int focusPlayerId = -1)
        {
            EnsureControllers();
            if (_activeEffects == null)
            {
                Debug.LogWarning("[EndOverlayHost] ActiveEffects UXML not assigned — run Kismeta → UI → Wire Bootstrap UI References.");
                return;
            }

            _reopenCardTableAfterInspect = false;
            _activeEffectsFocusPlayerId = focusPlayerId;
            ShowOverlay(_activeEffects, _activeEffectsCtrl, WireActiveEffects, ActiveOverlay.ActiveEffects);
        }

        public void ShowCardTable(int focusPlayerId = -1)
        {
            EnsureControllers();
            if (_cardTable == null)
            {
                Debug.LogWarning("[EndOverlayHost] CardTable UXML not assigned — run Kismeta → UI → Wire Bootstrap UI References.");
                return;
            }

            // Defer one frame so the rival click finishes before the modal layer appears.
            var scheduleRoot = _layout?.Root ?? _layout?.ContentScreenRoot;
            if (scheduleRoot != null)
            {
                scheduleRoot.schedule.Execute(() => OpenCardTable(focusPlayerId)).ExecuteLater(0);
                return;
            }

            OpenCardTable(focusPlayerId);
        }

        void OpenCardTable(int focusPlayerId)
        {
            if (focusPlayerId >= 0)
            {
                var screenRoot = _layout?.ContentScreenRoot;
                if (screenRoot != null)
                    HeaderOverlayBindings.SetExpanded(screenRoot, false, animate: false);
            }

            _reopenCardTableAfterInspect = false;
            _cardTableFocusPlayerId = focusPlayerId;
            ShowOverlay(_cardTable, _table, WireTable, ActiveOverlay.CardTable);
        }

        public void ShowInspect(string cardInstanceId)
        {
            EnsureControllers();
            if (_session != null)
            {
                var inst = _session.GetCard(cardInstanceId);
                var db = _session.Rules?.CardDatabase;
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.IsCrucible == true)
                {
                    ShowCrucibleDetail(cardInstanceId);
                    return;
                }
            }

            if (_active == ActiveOverlay.CardTable)
                _reopenCardTableAfterInspect = true;

            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            _modals?.BindInspect(_session!, cardInstanceId);
        }

        public void ShowCrucibleDetail(string cardInstanceId)
        {
            EnsureControllers();
            if (_crucibleCardDetail == null)
            {
                Debug.LogWarning("[EndOverlayHost] CrucibleCardDetail UXML not assigned — run Kismeta → UI → Wire Bootstrap UI References.");
                return;
            }

            if (_active == ActiveOverlay.CardTable)
                _reopenCardTableAfterInspect = true;

            if (_crucibleDetail != null)
            {
                _crucibleDetail.CardInstanceId = cardInstanceId;
                _crucibleDetail.SlotIndex = null;
            }

            ShowOverlay(_crucibleCardDetail, _crucibleDetail, WireCrucibleDetail, ActiveOverlay.CrucibleDetail);
        }

        public void ShowAdept(string adeptInstanceId)
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            if (_session != null && _bridge != null)
                _modals?.BindAdept(_session, _bridge, adeptInstanceId);
        }

        public void ShowFate(string fateInstanceId, int arcanaNum)
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            if (_session != null)
                _modals?.BindFate(_session, fateInstanceId, arcanaNum);
        }

        public void ShowMoonDecision()
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            if (_session != null && _bridge != null)
                _modals?.BindMoonDecision(_session, _bridge);
        }

        public void ShowFateReagentChoice(string? fateInstanceId = null)
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            if (_session != null && _bridge != null)
                _modals?.BindReagentChoice(_session, _bridge, fateInstanceId);
        }

        public void ShowLoversTargetPick()
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            if (_session != null && _bridge != null)
                _modals?.BindLoversTarget(_session, _bridge);
        }

        public void ShowLoversChoice(int drawerId)
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            if (_session != null && _bridge != null)
                _modals?.BindLoversChoice(_session, _bridge, drawerId);
        }

        public void Dismiss()
        {
            _active = ActiveOverlay.None;
            _table?.Detach();
            _modals?.Detach();
            _activeEffectsCtrl?.Detach();
            _crucibleDetail?.Detach();
            _layout?.DismissOverlay();
        }

        void ShowOverlay<T>(VisualTreeAsset? asset, T? controller, System.Action wire, ActiveOverlay kind)
            where T : OverlayController
        {
            if (_layout == null)
            {
                Debug.LogWarning("[EndOverlayHost] ViewportLayout missing — cannot show overlay.");
                return;
            }
            if (asset == null)
            {
                Debug.LogWarning("[EndOverlayHost] Overlay UXML not assigned.");
                return;
            }
            if (controller == null)
            {
                Debug.LogWarning($"[EndOverlayHost] {typeof(T).Name} missing on bootstrap object.");
                return;
            }
            _layout.ShowModal(asset);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;
            controller.AttachTo(root);
            _active = kind;
            wire();
            RefreshOpenOverlay();
        }

        void RefreshOpenOverlay()
        {
            if (_session == null || _bridge == null)
            {
                Debug.LogWarning("[EndOverlayHost] Session not bound — overlay content will be empty.");
                return;
            }
            if (_active == ActiveOverlay.CardTable)
            {
                _table?.SetFocus(_cardTableFocusPlayerId);
                _cardTableFocusPlayerId = -1;
                _table?.BindState(_session, _bridge);
            }
            else if (_active == ActiveOverlay.ActiveEffects)
            {
                _activeEffectsCtrl?.SetFocus(_activeEffectsFocusPlayerId);
                _activeEffectsFocusPlayerId = -1;
                _activeEffectsCtrl?.BindState(_session, _loop, _bridge!);
            }
            else if (_active == ActiveOverlay.CrucibleDetail)
            {
                _crucibleDetail?.BindState(_session, _bridge);
            }
        }

        void WireActiveEffects()
        {
            if (_activeEffectsCtrl == null) return;
            _activeEffectsCtrl.OnClose = Dismiss;
        }

        void WireTable()
        {
            if (_table == null) return;
            _table.OnClose = Dismiss;
            _table.OnInspect = ShowInspect;
            _table.OnDuel = id => { Dismiss(); OnDuelFromTable?.Invoke(id); };
            _table.OnGambit = id => { Dismiss(); OnGambitFromTable?.Invoke(id); };
            _table.OnTrade = id => { Dismiss(); OnTradeFromTable?.Invoke(id); };
        }

        void WireModals()
        {
            if (_modals == null) return;
            _modals.OnInspectDone = OnModalDone;
            _modals.OnAdeptCompleted = OnModalDone;
            _modals.OnFateAccept = OnModalDone;
            _modals.OnFateDecisionCompleted = OnModalDone;
        }

        void WireCrucibleDetail()
        {
            if (_crucibleDetail == null) return;
            _crucibleDetail.OnClose = OnModalDone;
        }

        void OnModalDone()
        {
            OnOverlayDismissed?.Invoke();
            if (_reopenCardTableAfterInspect)
            {
                _reopenCardTableAfterInspect = false;
                ShowCardTable();
                return;
            }
            Dismiss();
        }
    }
}
