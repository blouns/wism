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

        Assert.That(UnityEditor.EditorUtility.audioMasterMute, Is.True);
        yield return null;
        yield return null;
        Assert.That(UnityEditor.EditorUtility.audioMasterMute, Is.True);
#else
        Assert.Ignore("Editor-session policy is not part of player builds.");
        yield break;
#endif
    }
}
