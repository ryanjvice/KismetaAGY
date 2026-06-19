using System;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
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

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;
        GameLoop? _loop;

        CardTableController? _table;
        CardModalsController? _modals;

        enum ActiveOverlay { None, CardTable, CardModals }
        ActiveOverlay _active = ActiveOverlay.None;
        bool _reopenCardTableAfterInspect;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        public Action<int>? OnDuelFromTable;
        public Action<int>? OnGambitFromTable;
        public Action<int>? OnTradeFromTable;
        public Action? OnOverlayDismissed;

        void Awake() => EnsureControllers();

        public void Configure(VisualTreeAsset cardTable, VisualTreeAsset cardModals)
        {
            _cardTable = cardTable;
            _cardModals = cardModals;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _table ??= GetComponent<CardTableController>();
            _modals ??= GetComponent<CardModalsController>();
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

        public void ShowCardTable()
        {
            EnsureControllers();
            _reopenCardTableAfterInspect = false;
            ShowOverlay(_cardTable, _table, WireTable, ActiveOverlay.CardTable);
        }

        public void ShowInspect(string cardInstanceId)
        {
            EnsureControllers();
            if (_active == ActiveOverlay.CardTable)
                _reopenCardTableAfterInspect = true;

            ShowOverlay(_cardModals, _modals, WireModals, ActiveOverlay.CardModals);
            _modals?.BindInspect(_session!, cardInstanceId);
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

        public void Dismiss()
        {
            _active = ActiveOverlay.None;
            _table?.Detach();
            _modals?.Detach();
            _layout?.DismissOverlay();
        }

        void ShowOverlay<T>(VisualTreeAsset? asset, T? controller, System.Action wire, ActiveOverlay kind)
            where T : OverlayController
        {
            if (_layout == null || asset == null || controller == null) return;
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
            if (_session == null || _bridge == null) return;
            if (_active == ActiveOverlay.CardTable)
                _table?.BindState(_session, _bridge);
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
