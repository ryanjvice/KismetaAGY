using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kismeta.UI
{
    /// <summary>
    /// Controls the game splash screen with layered wheel art, continuous rotation,
    /// fade animation, and automatic transition to the next scene.
    /// </summary>
    public class GameSplashController : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 3.5f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("References")]
        [SerializeField] private CanvasGroup splashCanvasGroup;
        [SerializeField] private RectTransform starChart;
        [SerializeField] private RectTransform zodiacWheel;
        [SerializeField] private RectTransform mantleRing;

        [Header("Rotation")]
        [SerializeField] private float starChartSpeedDegPerSec = 3f;
        [SerializeField] private float zodiacWheelSpeedDegPerSec = 20f;
        [SerializeField] private float mantleRingSpeedDegPerSec = 25f;

        [Header("Star Chart")]
        [SerializeField] [Range(0f, 1f)] private float starChartOpacity = 0.5f;

        [Header("Next Scene")]
        [SerializeField] private string nextSceneName = "Bootstrap";

        [Header("Skip Settings")]
        [SerializeField] private bool allowSkip = true;

        private bool isTransitioning;
        private bool isRotating;

        private void Start()
        {
            if (splashCanvasGroup != null)
                splashCanvasGroup.alpha = 0f;

            TryFindStarChart();
            ApplyStarChartOpacity();
            StartCoroutine(SplashSequence());
        }

        private void Update()
        {
            if (isRotating)
            {
                var delta = Time.unscaledDeltaTime;
                if (starChart != null)
                    starChart.Rotate(0f, 0f, starChartSpeedDegPerSec * delta);
                if (zodiacWheel != null)
                    zodiacWheel.Rotate(0f, 0f, -zodiacWheelSpeedDegPerSec * delta);
                if (mantleRing != null)
                    mantleRing.Rotate(0f, 0f, mantleRingSpeedDegPerSec * delta);
            }

            if (allowSkip && !isTransitioning && WasSkipPressed())
                SkipSplash();
        }

        private static bool WasSkipPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
#else
            return Input.GetMouseButtonDown(0) || Input.touchCount > 0;
#endif
        }

        private IEnumerator SplashSequence()
        {
            yield return FadeCanvasGroup(0f, 1f, fadeInDuration);

            isRotating = true;
            yield return new WaitForSecondsRealtime(displayDuration);
            isRotating = false;

            yield return FadeCanvasGroup(1f, 0f, fadeOutDuration);
            LoadNextScene();
        }

        private void TryFindStarChart()
        {
            if (starChart != null || splashCanvasGroup == null) return;

            var chart = splashCanvasGroup.transform.Find("WheelStack/StarChart");
            if (chart != null)
                starChart = chart as RectTransform;
        }

        private void ApplyStarChartOpacity()
        {
            if (starChart == null) return;

            var image = starChart.GetComponent<Image>();
            if (image == null) return;

            var color = image.color;
            color.a = starChartOpacity;
            image.color = color;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TryFindStarChart();
            ApplyStarChartOpacity();
        }
#endif

        private IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            if (splashCanvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                splashCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            splashCanvasGroup.alpha = to;
        }

        private void SkipSplash()
        {
            isRotating = false;
            StopAllCoroutines();
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            if (isTransitioning) return;

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[GameSplashController] Scene '{nextSceneName}' is not in Build Settings.");
                return;
            }

            isTransitioning = true;
            Debug.Log($"[GameSplashController] Loading next scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
