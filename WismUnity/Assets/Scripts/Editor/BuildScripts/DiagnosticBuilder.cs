#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildScripts
{
    public static class DiagnosticBuilder
    {
        public static void Build()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-wism-build-output");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("-wism-build-output is required.");
            var directory = Path.GetFullPath(args[index + 1]);
            if (Directory.Exists(directory)) throw new IOException("Build output must be a new directory.");
            Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(directory, "WISM.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Diagnostic player build failed: " + report.summary.result);
            // The runtime mod resolver uses the same neutral relative layout as the editor.
            var source = Path.GetFullPath("Assets/Plugins/WismClient/Mods");
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories).Where(p => !p.EndsWith(".meta")))
            {
                var destination = Path.Combine(directory, "Assets/Plugins/WismClient/Mods", file.Substring(source.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination);
            }
        }
    }
}
#endif
