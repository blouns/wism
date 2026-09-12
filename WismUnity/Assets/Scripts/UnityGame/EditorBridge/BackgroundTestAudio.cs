using System;
using UnityEditor;
using UnityEngine;

namespace Wism.EditorTesting
{
    [InitializeOnLoad]
    public static class BackgroundTestAudio
    {
        public const string MuteArgument = "-wism-mute-audio";

        static BackgroundTestAudio()
        {
            if (!ShouldMute(Application.isBatchMode, Environment.GetCommandLineArgs(), AssetDatabase.IsAssetImportWorkerProcess()))
                return;

            // Listener volume is process-local. Never change the Editor's persisted mute preference.
            EnforceMute();
            EditorApplication.update += EnforceMute;
            EditorApplication.playModeStateChanged += _ => EnforceMute();
        }

        public static bool ShouldMute(bool batchMode, string[] arguments, bool assetImportWorker = false)
        {
            return !assetImportWorker && (batchMode || Array.Exists(arguments, argument =>
                string.Equals(argument, MuteArgument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(argument, "-runTests", StringComparison.OrdinalIgnoreCase)));
        }

        private static void EnforceMute()
        {
            AudioListener.volume = 0f;
        }
    }
}
