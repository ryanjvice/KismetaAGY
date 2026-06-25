using System;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class TitleScreenController : ScreenController
    {
        public override string ScreenId => ScreenIds.Title;

        public Action OnNewGame;
        public Action OnResume;
        public Action OnJoin;
        public Action OnHowToPlay;
        public Action OnCodex;

        const float StarChartSpeedDegPerSec = 3f;
        const float ZodiacWheelSpeedDegPerSec = -18f;
        const float MantleRingSpeedDegPerSec = 12f;

        VisualElement? _starChart;
        VisualElement? _zodiacWheel;
        VisualElement? _mantleRing;
        IVisualElementScheduledItem? _rotationTick;
        float _lastTickTime;
        float _starAngle;
        float _zodiacAngle;
        float _mantleAngle;
        VisualElement? _appFelt;

        protected override void Bind()
        {
            var screen = El("title-screen") ?? Root;
            UiArtBindings.ApplyTitleHero(screen);
            SetAppFeltVisible(false);
            StartWheelRotation();
        }

        protected override void Unwire()
        {
            StopWheelRotation();
            SetAppFeltVisible(true);
        }

        protected override void Wire()
        {
            Btn("new-game-btn")!.clicked += () => OnNewGame?.Invoke();
            Btn("resume-btn")!.clicked += () => OnResume?.Invoke();
            Btn("join-btn")!.clicked += () => OnJoin?.Invoke();
            Btn("howto-btn")!.clicked += () => OnHowToPlay?.Invoke();
            Btn("codex-btn")!.clicked += () => OnCodex?.Invoke();
        }

        void StartWheelRotation()
        {
            StopWheelRotation();

            _starChart = El("title-screen-star");
            _zodiacWheel = El("title-wheel-zodiac");
            _mantleRing = El("title-wheel-mantle");

            if (_starChart == null && _zodiacWheel == null && _mantleRing == null)
                return;

            _starAngle = 0f;
            _zodiacAngle = 0f;
            _mantleAngle = 0f;
            _lastTickTime = Time.realtimeSinceStartup;
            _rotationTick = Root!.schedule.Execute(TickWheelRotation);
            _rotationTick.ExecuteLater(16);
        }

        void StopWheelRotation()
        {
            _rotationTick?.Pause();
            _rotationTick = null;
            _starChart = null;
            _zodiacWheel = null;
            _mantleRing = null;
        }

        void TickWheelRotation()
        {
            if (Root == null)
                return;

            float now = Time.realtimeSinceStartup;
            float delta = now - _lastTickTime;
            _lastTickTime = now;

            if (_starChart != null)
            {
                _starAngle += StarChartSpeedDegPerSec * delta;
                _starChart.style.rotate = new Rotate(_starAngle);
            }

            if (_zodiacWheel != null)
            {
                _zodiacAngle += ZodiacWheelSpeedDegPerSec * delta;
                _zodiacWheel.style.rotate = new Rotate(_zodiacAngle);
            }

            if (_mantleRing != null)
            {
                _mantleAngle += MantleRingSpeedDegPerSec * delta;
                _mantleRing.style.rotate = new Rotate(_mantleAngle);
            }

            _rotationTick = Root.schedule.Execute(TickWheelRotation);
            _rotationTick.ExecuteLater(16);
        }

        void SetAppFeltVisible(bool visible)
        {
            _appFelt ??= Root?.parent?.parent?.Q("app-felt");
            if (_appFelt == null)
                return;

            _appFelt.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
