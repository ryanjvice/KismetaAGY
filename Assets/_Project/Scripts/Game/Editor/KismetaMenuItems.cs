using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kismeta.Game.Bootstrap;

namespace Kismeta.Game.Editor
{
    /// <summary>
    /// Editor utilities for the Kismeta project accessible via the Kismeta menu in the Unity toolbar.
    /// </summary>
    public static class KismetaMenuItems
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        [MenuItem("Kismeta/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            // Create or overwrite the Bootstrap scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove the default directional light (not needed for a logic-only scene).
            var light = GameObject.Find("Directional Light");
            if (light != null) Object.DestroyImmediate(light);

            // Create the bootstrap host GameObject.
            var host = new GameObject("GameBootstrap");
            host.AddComponent<GameBootstrap>();

            // Save the scene.
            Directory.CreateDirectory(Path.GetDirectoryName(BootstrapScenePath)!);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);

            // Add to build settings at index 0.
            AddSceneToBuildSettings(BootstrapScenePath);

            Debug.Log($"[Kismeta] Bootstrap scene created at {BootstrapScenePath} and added to Build Settings.");
        }

        [MenuItem("Kismeta/Run data:sync (npm)")]
        public static void RunDataSync()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName  = "npm";
            proc.StartInfo.Arguments = "run data:sync";
            proc.StartInfo.WorkingDirectory = projectRoot;
            proc.StartInfo.UseShellExecute  = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError  = true;
            proc.Start();
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(stdout)) Debug.Log($"[data:sync]\n{stdout}");
            if (!string.IsNullOrEmpty(stderr)) Debug.LogWarning($"[data:sync stderr]\n{stderr}");

            AssetDatabase.Refresh();
            Debug.Log("[Kismeta] data:sync complete. Assets refreshed.");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var current = EditorBuildSettings.scenes;
            foreach (var s in current)
                if (s.path == scenePath) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(current)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
