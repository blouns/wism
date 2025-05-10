#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace BuildScripts
{
    public static class CIBuilder
    {
        public static void Build()
        {
            string buildPath = "Builds/Windows/WISM.exe";
            string[] scenes = new[] { "Assets/Scenes/mini-illuria.unity" };

            if (Directory.Exists("Builds/Windows"))
            {
                Directory.Delete("Builds/Windows", recursive: true);
                UnityEngine.Debug.Log("Cleaned previous build directory.");
            }


            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log($"Build succeeded. Output: {summary.outputPath}");
            }
            else
            {
                UnityEngine.Debug.LogError($"Build failed: {summary.result}");
                EditorApplication.Exit(1); // Fail the process for CI
            }
        }
    }
}
#endif
