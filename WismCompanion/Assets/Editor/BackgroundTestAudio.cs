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
            if (!ShouldMute(Application.isBatchMode, Environment.GetCommandLineArgs()))
                return;

            // Process-local master mute preserves audio timing and normal player settings.
            EnforceMute();
            EditorApplication.update += EnforceMute;
            EditorApplication.playModeStateChanged += _ => EnforceMute();
        }

        public static bool ShouldMute(bool batchMode, string[] arguments)
        {
            return batchMode || Array.Exists(arguments, argument =>
                string.Equals(argument, MuteArgument, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(argument, "-runTests", StringComparison.OrdinalIgnoreCase));
        }

        private static void EnforceMute()
        {
            EditorUtility.audioMasterMute = true;
        }
    }
}
