using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kismeta.UI
{
    /// <summary>
    /// Controls the studio splash screen display with fade animation and audio support.
    /// Automatically transitions to the next scene after display duration.
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 3.5f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("References")]
        [SerializeField] private CanvasGroup splashCanvasGroup;
        [SerializeField] private Image logoImage;
        [SerializeField] private TextMeshProUGUI studioNameText;
        [SerializeField] private RectTransform goodImage;
        [SerializeField] private RectTransform magikImage;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip splashSound;

        [Header("Next Scene")]
        [SerializeField] private string nextSceneName = "Bootstrap";

        [Header("Skip Settings")]
        [SerializeField] private bool allowSkip = true;

        [Header("Animation Settings")]
        [SerializeField] private float rotationDuration = 2f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region Private Fields

        private bool isTransitioning = false;
        private Coroutine goodImageRotationCoroutine;
        private Coroutine magikImageRotationCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Start with canvas invisible
            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 0f;
            }

            // Hide MagikImage by setting scale to 0
            if (magikImage != null)
            {
                magikImage.localScale = Vector3.zero;
            }

            StartCoroutine(SplashSequence());
        }

        private void Update()
        {
            if (allowSkip && !isTransitioning && WasSkipPressed())
            {
                SkipSplash();
            }
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

        #endregion

        #region Splash Sequence

        /// <summary>
        /// Main splash sequence coroutine
        /// </summary>
        private IEnumerator SplashSequence()
        {
            // Play audio if available
            if (audioSource != null && splashSound != null)
            {
                audioSource.PlayOneShot(splashSound);
            }

            // Fade in
            yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));

            // Start rotation animations if images are assigned
            if (goodImage != null)
            {
                goodImageRotationCoroutine = StartCoroutine(RotateGoodImage());
            }

            if (magikImage != null)
            {
                magikImageRotationCoroutine = StartCoroutine(RotateMagikImage());
            }

            // Display duration
            yield return new WaitForSecondsRealtime(displayDuration);

            // Stop rotation animations
            if (goodImageRotationCoroutine != null)
            {
                StopCoroutine(goodImageRotationCoroutine);
                goodImageRotationCoroutine = null;
            }

            if (magikImageRotationCoroutine != null)
            {
                StopCoroutine(magikImageRotationCoroutine);
                magikImageRotationCoroutine = null;
            }

            // Fade out and transition
            yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));

            LoadNextScene();
        }

        /// <summary>
        /// Fade the canvas group alpha over time
        /// </summary>
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

        /// <summary>
        /// Rotate GoodImage 180 degrees on the y-axis continuously
        /// </summary>
        private IEnumerator RotateGoodImage()
        {
            if (goodImage == null) yield break;

            float elapsed = 0f;
            float startRotation = 0f;
            float targetRotation = 180f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / rotationDuration;
                
                // Rotate from 0 to 180 degrees with curve easing
                float t = Mathf.Clamp01(progress);
                float curveT = animationCurve.Evaluate(t);
                float currentRotation = Mathf.Lerp(startRotation, targetRotation, curveT);
                goodImage.localRotation = Quaternion.Euler(0f, currentRotation, 0f);

                // Reset and loop when reaching 180 degrees
                if (progress >= 1f)
                {
                    elapsed = 0f;
                    // Flip start and target to rotate back and forth
                    float temp = startRotation;
                    startRotation = targetRotation;
                    targetRotation = temp;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Rotate MagikImage 180 degrees on the z-axis continuously and scale from 0 to 1
        /// </summary>
        private IEnumerator RotateMagikImage()
        {
            if (magikImage == null) yield break;

            // Start with scale at 0
            magikImage.localScale = Vector3.zero;

            float elapsed = 0f;
            float startRotation = 0f;
            float targetRotation = 180f;
            bool hasScaledUp = false;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / rotationDuration;
                
                // Scale from 0 to 1 during the first rotation cycle with curve easing
                if (!hasScaledUp)
                {
                    float t = Mathf.Clamp01(progress);
                    float curveT = animationCurve.Evaluate(t);
                    magikImage.localScale = Vector3.one * curveT;
                    
                    if (curveT >= 1f)
                    {
                        hasScaledUp = true;
                        magikImage.localScale = Vector3.one;
                    }
                }
                
                // Rotate from 0 to 180 degrees with curve easing
                float rotationT = Mathf.Clamp01(progress);
                float rotationCurveT = animationCurve.Evaluate(rotationT);
                float currentRotation = Mathf.Lerp(startRotation, targetRotation, rotationCurveT);
                magikImage.localRotation = Quaternion.Euler(0f, 0f, currentRotation);

                // Reset and loop when reaching 180 degrees
                if (progress >= 1f)
                {
                    elapsed = 0f;
                    // Flip start and target to rotate back and forth
                    float temp = startRotation;
                    startRotation = targetRotation;
                    targetRotation = temp;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Skip the splash screen and load the next scene immediately
        /// </summary>
        private void SkipSplash()
        {
            if (goodImageRotationCoroutine != null)
            {
                StopCoroutine(goodImageRotationCoroutine);
                goodImageRotationCoroutine = null;
            }

            if (magikImageRotationCoroutine != null)
            {
                StopCoroutine(magikImageRotationCoroutine);
                magikImageRotationCoroutine = null;
            }

            StopAllCoroutines();
            LoadNextScene();
        }

        /// <summary>
        /// Load the next scene
        /// </summary>
        private void LoadNextScene()
        {
            if (isTransitioning) return;

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[SplashController] Scene '{nextSceneName}' is not in Build Settings.");
                return;
            }

            isTransitioning = true;

            Debug.Log($"[SplashController] Loading next scene: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }

        #endregion
    }
}
