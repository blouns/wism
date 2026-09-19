using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.UnityGame.ModKit
{
    public static class UnityWorldSceneCatalog
    {
        public static string Resolve(string world, string explicitScene = null)
        {
            if (!string.IsNullOrWhiteSpace(explicitScene))
                return Application.CanStreamedLevelBeLoaded(explicitScene) ? explicitScene : null;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), world, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
        }
    }
}
