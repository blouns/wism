using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WismCompanion.EditorTools
{
    /// <summary>
    /// Builds the standalone Windows player for WismCompanion. Usable three ways:
    /// <list type="bullet">
    /// <item>Editor menu <b>WISM &gt; Build Windows Player</b> (builds while the Editor is open).</item>
    /// <item>Editor menu <b>WISM &gt; Build and Launch</b> (build then run the exe).</item>
    /// <item>Headless via <c>build-companion.ps1</c> → <c>-executeMethod WismCompanion.EditorTools.CompanionBuild.BuildWindows</c>.</item>
    /// </list>
    /// </summary>
    public static class CompanionBuild
    {
        private const string ProductName = "WISM Companion";
        private const string ExeName = "WismCompanion.exe";

        /// <summary>Batchmode entry point. Reads <c>-buildOutput &lt;dir&gt;</c> if provided.</summary>
        public static void BuildWindows()
        {
            var outDir = GetArg("-buildOutput") ?? DefaultOutputDir();
            BuildPlayerInternal(outDir, out var ok);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        [MenuItem("WISM/Build Windows Player")]
        public static void BuildWindowsMenu()
        {
            var exe = BuildPlayerInternal(DefaultOutputDir(), out var ok);
            if (ok)
            {
                EditorUtility.RevealInFinder(exe);
            }
        }

        [MenuItem("WISM/Build and Launch")]
        public static void BuildAndLaunchMenu()
        {
            var exe = BuildPlayerInternal(DefaultOutputDir(), out var ok);
            if (ok && File.Exists(exe))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe)
                {
                    UseShellExecute = true
                });
            }
        }

        private static string BuildPlayerInternal(string outputDir, out bool ok)
        {
            Directory.CreateDirectory(outputDir);
            var exePath = Path.Combine(outputDir, ExeName);

            // Ensure the runtime UI assets (theme/PanelSettings/text settings) exist and the asset DB
            // is settled before building — done synchronously here so it can't race the build.
            CompanionSetup.EnsureAssets(false);

            // Ship as a resizable window (not fullscreen) and keep updating in the background so the
            // stream stays live when the companion isn't focused.
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "WISM";
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            ok = report.summary.result == BuildResult.Succeeded;
            if (ok)
            {
                Debug.Log($"[WismCompanion] Build succeeded → {exePath} ({report.summary.totalSize} bytes)");
            }
            else
            {
                Debug.LogError($"[WismCompanion] Build {report.summary.result}: {report.summary.totalErrors} error(s). See the build log.");
            }

            return exePath;
        }

        private static string DefaultOutputDir()
        {
            // Application.dataPath is "<project>/Assets"; place the build under "<project>/Build/Win64".
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, "Build", "Win64");
        }

        private static string[] GetScenes()
        {
            var scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            if (scenes.Count == 0)
            {
                const string sample = "Assets/Scenes/SampleScene.unity";
                if (File.Exists(sample))
                {
                    scenes.Add(sample);
                }
            }

            return scenes.ToArray();
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
