using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BackgroundAudioPlayModeTests
{
    [UnityTest]
    public IEnumerator BackgroundPlayModeRemainsMutedAcrossFrames()
    {
#if UNITY_EDITOR
        if (!Application.isBatchMode)
            Assert.Ignore("This assertion requires a background editor session.");

        var editorMute = UnityEditor.EditorUtility.audioMasterMute;
        Assert.That(AudioListener.volume, Is.Zero);
        yield return null;
        yield return null;
        Assert.That(AudioListener.volume, Is.Zero);
        Assert.That(UnityEditor.EditorUtility.audioMasterMute, Is.EqualTo(editorMute),
            "Automated playback must not modify the Editor's mute preference.");
#else
        Assert.Ignore("Editor-session policy is not part of player builds.");
        yield break;
#endif
    }
}
