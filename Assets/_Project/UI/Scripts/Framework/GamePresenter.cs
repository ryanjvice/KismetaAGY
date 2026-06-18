using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Controllers;
using Kismeta.UI.Setup;
using UnityEngine;

namespace Kismeta.UI
{
    /// <summary>
    /// Connects <see cref="GameLoop"/> state to <see cref="ScreenRouter"/> navigation
    /// and refreshes active screen controllers when the session changes.
    /// </summary>
    [RequireComponent(typeof(ScreenRouter))]
    public sealed class GamePresenter : MonoBehaviour
    {
        [SerializeField] private bool _startAtTitle = true;

        private readonly SetupSheetState _setupSheetState = new();

        private ScreenRouter _router;
        private ViewportLayout _layout;
        private readonly CommandBridge _bridge = new();
        private GameSession? _session;
        private GameLoop? _loop;

        private bool _inGame;
        private HotSeatController? _lastHuman;
        private ActionHint _lastHint = ActionHint.None;
        private Season _lastSeason = Season.Spring;

        public CommandBridge Bridge => _bridge;
        public bool IsInGame => _inGame;

        public event Action<UiSetupConfig>? SetupBeginRequested;

        private void Awake()
        {
            _router = GetComponent<ScreenRouter>();
            _layout = GetComponent<ViewportLayout>();
        }

        /// <summary>Wire title menu and show the title screen before a session exists.</summary>
        public void InitializeForTitle()
        {
            WireTitleScreen();
            if (_router.CurrentScreenId != ScreenIds.Title)
                _router.GoTo(ScreenIds.Title);
        }

        public void Bind(GameSession session, GameLoop loop)
        {
            Unbind();

            _session = session;
            _loop = loop;
            _bridge.Bind(loop);

            session.OnEvent += OnSessionEvent;
            loop.OnLog += OnLoopLog;

            WireTitleScreen();
        }

        public void Unbind()
        {
            if (_session != null)
                _session.OnEvent -= OnSessionEvent;
            if (_loop != null)
                _loop.OnLog -= OnLoopLog;
            _session = null;
            _loop = null;
            _inGame = false;
        }

        private void Update()
        {
            if (_loop == null || _session == null || !_inGame)
                return;

            var hs = _loop.PendingHumanController;
            var hint = _loop.PendingHint;
            var season = _session.Phase.CurrentSeason;

            if (_session.IsOver)
            {
                RouteIfNeeded(ScreenIds.GameplayHud);
                RefreshActiveScreen();
                return;
            }

            if (hs != _lastHuman || hint != _lastHint || season != _lastSeason)
            {
                _lastHuman = hs;
                _lastHint = hint;
                _lastSeason = season;
                RouteGameplay();
            }
        }

        private void OnSessionEvent(IGameEvent _) => RefreshActiveScreen();
        private void OnLoopLog(string _) => RefreshActiveScreen();

        private void WireTitleScreen()
        {
            var title = _router.GetController<TitleScreenController>(ScreenIds.Title);
            if (title == null) return;

            title.OnNewGame = () =>
            {
                _router.ShowSetupSheet(
                    onOpened: sheet => _setupSheetState.Attach(sheet),
                    onBegin: () => NotifySetupBegin(_setupSheetState.Current));
            };
            title.OnResume = () => Debug.Log("[UI] Resume not implemented.");
            title.OnJoin = () => Debug.Log("[UI] Join not implemented.");
            title.OnHowToPlay = () => Debug.Log("[UI] How to play not implemented.");
            title.OnCodex = () => Debug.Log("[UI] Codex not implemented.");
        }

        public void NotifySetupBegin(UiSetupConfig config)
        {
            _router.DismissOverlay();
            SetupBeginRequested?.Invoke(config);
            _inGame = true;
            _lastHuman = null;
            _lastHint = ActionHint.None;
            RouteGameplay();
        }

        public void ReturnToTitle()
        {
            _inGame = false;
            _setupSheetState.Detach();
            _router.GoTo(ScreenIds.Title);
        }

        private void RouteGameplay()
        {
            if (_session!.IsOver)
            {
                RouteIfNeeded(ScreenIds.GameplayHud);
                RefreshActiveScreen();
                return;
            }

            if (_loop!.PendingHumanController == null)
            {
                RouteIfNeeded(ScreenIds.Waiting);
                RefreshActiveScreen();
                return;
            }

            RouteIfNeeded(ScreenIds.GameplayHud);
            RefreshActiveScreen();
        }

        private void RouteIfNeeded(string screenId)
        {
            if (_router.CurrentScreenId != screenId)
                _router.GoTo(screenId);
        }

        private void RefreshActiveScreen()
        {
            var controller = _router.ActiveController;
            if (controller is GameplayHudController hud && _session != null && _loop != null)
                hud.BindState(_session, _loop, _bridge);
            else if (controller is WaitingHudController waiting && _session != null && _loop != null)
                waiting.BindState(_session, _loop);
            else
                controller?.Refresh();
        }
    }
}
