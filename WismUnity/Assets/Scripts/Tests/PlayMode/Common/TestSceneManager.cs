using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Tests.PlayMode
{
    public static class TestSceneBuildManager
    {

        /// Add all scenes to the build settings that are in the test scene folder.
        public static void AddTestScenesToBuildSettings(string testSceneFolder)
        {
#if UNITY_EDITOR
            var scenes = new List<EditorBuildSettingsScene>();
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { testSceneFolder });
            if (guids != null)
            {
                foreach (string guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        var scene = new EditorBuildSettingsScene(path, true);
                        scenes.Add(scene);
                    }
                }
            }

            Debug.Log("Adding test scenes to build settings:\n" + string.Join("\n", scenes.Select(scene => scene.path)));
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Union(scenes).ToArray();
#endif
        }

        /// Remove all scenes from the build settings that are in the test scene folder.
        public static void RemoveTestScenesFromBuildSettings(string testSceneFolder)
        {
#if UNITY_EDITOR
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(scene => !scene.path.StartsWith(testSceneFolder)).ToArray();
#endif
        }
    }
}
