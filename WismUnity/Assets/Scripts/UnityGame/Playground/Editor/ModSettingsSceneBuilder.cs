using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WismUnity.Playground
{
    public static class ModSettingsSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/ModSettings.unity";

        [MenuItem("WISM/Mod Kit/Rebuild Mod Settings Scene")]
        public static void Rebuild()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
            canvas.AddComponent<ModSettingsPanel>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings(ScenePath);
            Debug.Log("Rebuilt WISM Mod Settings scene at " + ScenePath);
        }

        static void EnsureBuildSettings(string modSettingsScenePath)
        {
            var desired = new[]
            {
                "Assets/Scenes/SplashScreen.unity",
                modSettingsScenePath,
                "Assets/Scenes/GameSetup.unity",
                "Assets/Scenes/Mini-Illuria.unity",
                "Assets/Scenes/Test/TestWorld.unity"
            };
            var existing = EditorBuildSettings.scenes
                .Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();

            foreach (var path in desired)
            {
                if (!existing.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    existing.Add(path);
                }
            }

            EditorBuildSettings.scenes = existing
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }
    }
}
