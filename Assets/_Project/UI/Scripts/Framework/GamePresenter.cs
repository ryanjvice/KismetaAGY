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
        private UiSetupConfig _pendingSetupConfig;

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
            WireShellScreens();
            WireAgekeeperContest();
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
            WireShellScreens();
            WireAgekeeperContest();
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
                RouteIfNeeded(ResolveSeasonMainScreen(_session.Phase.CurrentSeason));
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
                    onBegin: BeginAgekeeperContest);
            };
            title.OnResume = () => _router.GoTo(ScreenIds.Resume);
            title.OnJoin = () => _router.GoTo(ScreenIds.Join);
            title.OnHowToPlay = OpenHowToPlay;
            title.OnCodex = OpenCodex;
        }

        private void WireShellScreens()
        {
            var join = _router.GetController<JoinScreenController>(ScreenIds.Join);
            if (join != null)
            {
                join.OnBack = ReturnToTitle;
                join.OnJoin = code =>
                    Debug.Log($"[UI] Join room '{code}' — multiplayer not implemented.");
            }

            var resume = _router.GetController<ResumeScreenController>(ScreenIds.Resume);
            if (resume != null)
            {
                resume.OnBack = ReturnToTitle;
                resume.OnResumeGame = id =>
                    Debug.Log($"[UI] Resume save '{id}' — persistence not implemented.");
                resume.RebuildList();
            }

            var codex = _router.GetController<CodexScreenController>(ScreenIds.Codex);
            if (codex != null)
                codex.OnBack = ReturnToTitle;
        }

        private void OpenCodex()
        {
            _router.GoTo(ScreenIds.Codex);
            _router.GetController<CodexScreenController>(ScreenIds.Codex)?.ShowTab(CodexTabs.Cards);
        }

        private void OpenHowToPlay()
        {
            _router.GoTo(ScreenIds.Codex);
            _router.GetController<CodexScreenController>(ScreenIds.Codex)?.ShowTab(CodexTabs.Terms);
        }

        private void WireAgekeeperContest()
        {
            var contest = _router.GetController<AgekeeperContestController>(ScreenIds.AgekeeperContest);
            if (contest == null) return;

            contest.OnComplete = winnerId =>
            {
                var cfg = _pendingSetupConfig;
                cfg.FirstAgekeeperPlayerId = winnerId;
                NotifySetupBegin(cfg);
            };
        }

        private void BeginAgekeeperContest()
        {
            _pendingSetupConfig = _setupSheetState.Current;
            _setupSheetState.Detach();
            _router.GoTo(ScreenIds.AgekeeperContest);
            _router.GetController<AgekeeperContestController>(ScreenIds.AgekeeperContest)
                ?.BeginContest(_pendingSetupConfig.Players);
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
                RouteIfNeeded(ResolveSeasonMainScreen(_session.Phase.CurrentSeason));
                RefreshActiveScreen();
                return;
            }

            if (_loop!.PendingHumanController == null)
            {
                RouteIfNeeded(ScreenIds.Waiting);
                RefreshActiveScreen();
                return;
            }

            RouteIfNeeded(ResolveSeasonMainScreen(_session.Phase.CurrentSeason));
            RefreshActiveScreen();
        }

        private static string ResolveSeasonMainScreen(Season season) => season switch
        {
            Season.Spring => ScreenIds.SpringHub,
            Season.Summer => ScreenIds.SummerMain,
            Season.Autumn => ScreenIds.AutumnMain,
            Season.Winter => ScreenIds.WinterHub,
            _ => ScreenIds.SpringHub
        };

        private void RouteIfNeeded(string screenId)
        {
            if (_router.CurrentScreenId != screenId)
                _router.GoTo(screenId);
        }

        private void RefreshActiveScreen()
        {
            if (_session == null || _loop == null) return;

            var controller = _router.ActiveController;
            if (controller is SpringHubController spring)
                spring.BindState(_session, _loop, _bridge);
            else if (controller is SummerSceneController summer)
                summer.BindState(_session, _loop, _bridge);
            else if (controller is AutumnSceneController autumn)
                autumn.BindState(_session, _loop, _bridge);
            else if (controller is WinterHubController winter)
                winter.BindState(_session, _loop, _bridge);
            else if (controller is GameplayHudController hud)
                hud.BindState(_session, _loop, _bridge);
            else if (controller is WaitingHudController waiting)
                waiting.BindState(_session, _loop);
            else if (controller is ResumeScreenController resumeCtrl)
                resumeCtrl.RebuildList();
            else
                controller?.Refresh();
        }
    }
}
