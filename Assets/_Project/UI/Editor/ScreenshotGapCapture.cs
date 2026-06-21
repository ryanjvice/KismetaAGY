using System.Collections.Generic;
using System.IO;
using Kismeta.UI;
using Kismeta.UI.Controllers;
using UnityEditor;
using UnityEngine;

namespace Kismeta.UI.Editor
{
    /// <summary>
    /// Captures missing UI screenshots during Play Mode (Bootstrap scene, active game session).
    /// Output: Docs/_images/screenshots/gaps/
    /// Menu: Kismeta → UI → Capture Missing Screenshots
    /// </summary>
    public static class ScreenshotGapCapture
    {
        const string OutputDir = "Docs/_images/screenshots/gaps";
        const int FrameDelay = 12;
        const int PostCaptureDelay = 4;

        static List<(string filename, System.Action capture)> _queue;
        static int _index;
        static string _pendingFile = "";
        static int _waitFrames;
        static bool _running;

        static int _phase; // 0=navigate, 1=wait before shot, 2=wait after shot

        static readonly (string filename, System.Action capture)[] Captures =
        {
            ("spring_hub_roll.png", () => CaptureHelpers.Go(ScreenIds.SpringHub)),
            ("spring_harvest.png", () => CaptureHelpers.Go(ScreenIds.SpringHarvest)),
            ("summer_main_default.png", () => CaptureHelpers.Go(ScreenIds.SummerMain)),
            ("summer_consortSheet.png", () => { CaptureHelpers.Go(ScreenIds.SummerMain); CaptureHelpers.Overlay<SummerOverlayHost>(h => h.ShowConsortSheet()); }),
            ("summer_craftReagent_result.png", () => { CaptureHelpers.Go(ScreenIds.SummerMain); CaptureHelpers.Overlay<SummerOverlayHost>(h => h.ShowCraftReagent()); }),
            ("summer_duel_result.png", () => { CaptureHelpers.Go(ScreenIds.SummerMain); CaptureHelpers.Overlay<ContestOverlayHost>(h => h.ShowDuel()); }),
            ("summer_gambit_ante.png", () => { CaptureHelpers.Go(ScreenIds.SummerMain); CaptureHelpers.Overlay<ContestOverlayHost>(h => h.ShowGambit(1)); }),
            ("autumn_fireStone.png", () => { CaptureHelpers.Go(ScreenIds.AutumnMain); CaptureHelpers.Overlay<AutumnOverlayHost>(h => h.ShowFire()); }),
            ("autumn_temperStone.png", () => { CaptureHelpers.Go(ScreenIds.AutumnMain); CaptureHelpers.Overlay<AutumnOverlayHost>(h => h.ShowTemper()); }),
            ("autumn_opposition_start.png", () => { CaptureHelpers.Go(ScreenIds.AutumnMain); CaptureHelpers.Overlay<ContestOverlayHost>(h => h.ShowOpposition()); }),
            ("autumn_leaveStasis.png", () => { CaptureHelpers.Go(ScreenIds.AutumnMain); CaptureHelpers.Overlay<AutumnOverlayHost>(h => h.ShowLeaveStasis()); }),
            ("winter_cardLimits.png", () => CaptureHelpers.Go(ScreenIds.CardLimits)),
            ("winter_ageClosing.png", () => CaptureHelpers.Go(ScreenIds.AgeClosing)),
            ("end_victory.png", () => CaptureHelpers.Go(ScreenIds.Victory)),
            ("end_chronicle.png", () => CaptureHelpers.Go(ScreenIds.Chronicle)),
        };

        [MenuItem("Kismeta/UI/Capture Missing Screenshots")]
        public static void StartCapture()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Kismeta.UI] Enter Play Mode on Bootstrap scene with an active game, then run again.");
                return;
            }

            if (Object.FindFirstObjectByType<ScreenRouter>() == null)
            {
                Debug.LogWarning("[Kismeta.UI] No ScreenRouter found in scene.");
                return;
            }

            StopCapture();
            _queue = new List<(string, System.Action)>(Captures);
            _index = 0;
            _pendingFile = "";
            _waitFrames = 0;
            _phase = 0;
            _running = true;
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", OutputDir));
            EditorApplication.update += OnEditorUpdate;
            Debug.Log($"[Kismeta.UI] Capturing {_queue.Count} gap screenshots → {OutputDir}/");
        }

        static void StopCapture()
        {
            _running = false;
            EditorApplication.update -= OnEditorUpdate;
            DismissOverlays();
        }

        static void OnEditorUpdate()
        {
            if (!_running || !EditorApplication.isPlaying)
            {
                StopCapture();
                return;
            }

            if (_waitFrames > 0)
            {
                _waitFrames--;
                if (_waitFrames == 0)
                {
                    if (_phase == 1 && _pendingFile != "")
                    {
                        var path = Path.Combine(Application.dataPath, "..", OutputDir, _pendingFile);
                        ScreenCapture.CaptureScreenshot(path);
                        Debug.Log($"[Kismeta.UI] Saved {OutputDir}/{_pendingFile}");
                        _phase = 2;
                        _waitFrames = PostCaptureDelay;
                        return;
                    }

                    if (_phase == 2)
                    {
                        _pendingFile = "";
                        _index++;
                        _phase = 0;
                    }
                }
                return;
            }

            if (_index >= _queue.Count)
            {
                StopCapture();
                Debug.Log("[Kismeta.UI] Gap screenshot capture complete.");
                return;
            }

            DismissOverlays();
            var (filename, capture) = _queue[_index];
            capture();
            _pendingFile = filename;
            _phase = 1;
            _waitFrames = FrameDelay;
        }

        static void DismissOverlays()
        {
            var layout = Object.FindFirstObjectByType<ViewportLayout>();
            layout?.DismissOverlay();
        }
    }

    static class CaptureHelpers
    {
        internal static void Go(string screenId)
        {
            var router = Object.FindFirstObjectByType<ScreenRouter>();
            router?.GoTo(screenId);
        }

        internal static void Overlay<T>(System.Action<T> show) where T : Component
        {
            var host = Object.FindFirstObjectByType<T>();
            if (host != null)
                show(host);
            else
                Debug.LogWarning($"[Kismeta.UI] Missing {typeof(T).Name} for overlay capture.");
        }
    }
}
