using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kismeta.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Kismeta.UI.Editor
{
    public static class GameSplashSceneBuilder
    {
        public const string GameSplashScenePath = "Assets/_Project/Scenes/GameSplashScreen.unity";

        const string ArtRoot = "Assets/_Project/Art/GameSplash";
        const int UiLayer = 5;
        static readonly Vector2 ReferenceResolution = new(1080f, 1620f);
        static readonly Vector2 WheelStackSize = new(1980f, 1984f);

        [MenuItem("Kismeta/Create Game Splash Scene")]
        public static void CreateGameSplashScene()
        {
            CreateScene();
            EnsureGameSplashInBuildSettings();
            Debug.Log($"[Kismeta] Game splash scene created at {GameSplashScenePath} and added to Build Settings.");
        }

        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateEventSystem();
            var canvas = CreateCanvas();
            var splashPanel = CreateSplashPanel(canvas);
            CreateBackground(splashPanel);
            var wheelStack = CreateWheelStack(splashPanel);
            var starChart = CreateWheelLayer(wheelStack, "StarChart", LoadSprite($"{ArtRoot}/starChart.png"), alpha: 0.5f);
            var zodiacWheel = CreateWheelLayer(wheelStack, "ZodiacWheel", LoadSprite($"{ArtRoot}/zodiacWheel.png"));
            var mantleRing = CreateWheelLayer(wheelStack, "MantleRing", LoadSprite($"{ArtRoot}/mantleRing.png"));
            CreateWheelLayer(wheelStack, "CrucibleForge", LoadSprite($"{ArtRoot}/crucibleForge.png"));

            var controllerGo = new GameObject("GameSplashController");
            var controller = controllerGo.AddComponent<GameSplashController>();
            WireController(controller, splashPanel.GetComponent<CanvasGroup>(), starChart, zodiacWheel, mantleRing);

            Directory.CreateDirectory(Path.GetDirectoryName(GameSplashScenePath)!);
            EditorSceneManager.SaveScene(scene, GameSplashScenePath);
        }

        static void EnsureGameSplashInBuildSettings()
        {
            const string studioPath = "Assets/_Project/Scenes/StudioSplashScreen.unity";
            const string bootstrapPath = "Assets/_Project/Scenes/Bootstrap.unity";

            var scenes = EditorBuildSettings.scenes.ToList();
            EnsureScene(scenes, studioPath, enabled: true);
            EnsureScene(scenes, GameSplashScenePath, enabled: true);
            EnsureScene(scenes, bootstrapPath, enabled: true);

            EditorBuildSettings.scenes = scenes
                .OrderBy(scene => scene.path == studioPath ? 0 :
                    scene.path == GameSplashScenePath ? 1 :
                    scene.path == bootstrapPath ? 2 : 3)
                .ToArray();
        }

        static void EnsureScene(System.Collections.Generic.List<EditorBuildSettingsScene> scenes, string scenePath, bool enabled)
        {
            var index = scenes.FindIndex(scene => scene.path == scenePath);
            if (index < 0)
                scenes.Add(new EditorBuildSettingsScene(scenePath, enabled));
            else
                scenes[index] = new EditorBuildSettingsScene(scenePath, enabled);
        }

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        static Transform CreateCanvas()
        {
            var canvasGo = new GameObject("SplashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = UiLayer;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            return canvasGo.transform;
        }

        static Transform CreateSplashPanel(Transform canvas)
        {
            var go = new GameObject("SplashPanel", typeof(RectTransform), typeof(CanvasGroup));
            go.layer = UiLayer;

            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            StretchFull(rt);

            return go.transform;
        }

        static void CreateBackground(Transform splashPanel)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = UiLayer;

            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(splashPanel, false);
            StretchFull(rt);

            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite($"{ArtRoot}/splashBackground.png");
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        static Transform CreateWheelStack(Transform splashPanel)
        {
            var go = new GameObject("WheelStack", typeof(RectTransform));
            go.layer = UiLayer;

            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(splashPanel, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = WheelStackSize;

            return go.transform;
        }

        static RectTransform CreateWheelLayer(Transform wheelStack, string name, Sprite sprite, float alpha = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = UiLayer;

            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(wheelStack, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = WheelStackSize;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var color = image.color;
            color.a = alpha;
            image.color = color;

            return rt;
        }

        static void WireController(
            GameSplashController controller,
            CanvasGroup canvasGroup,
            RectTransform starChart,
            RectTransform zodiacWheel,
            RectTransform mantleRing)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("splashCanvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("starChart").objectReferenceValue = starChart;
            so.FindProperty("zodiacWheel").objectReferenceValue = zodiacWheel;
            so.FindProperty("mantleRing").objectReferenceValue = mantleRing;
            so.FindProperty("starChartOpacity").floatValue = 0.5f;
            so.FindProperty("starChartSpeedDegPerSec").floatValue = 3f;
            so.FindProperty("nextSceneName").stringValue = "Bootstrap";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        }
    }
}
