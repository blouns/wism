using NUnit.Framework;
using UnityEditor;
using Wism.EditorTesting;

public class BackgroundTestAudioTests
{
    [TestCase(false, "", false)]
    [TestCase(false, "-projectPath", false)]
    [TestCase(false, "contains-runTests-text", false)]
    [TestCase(false, "-runTests", true)]
    [TestCase(false, "-RUNTESTS", true)]
    [TestCase(false, "-wism-mute-audio", true)]
    [TestCase(true, "", true)]
    public void OnlyAutomatedSessionsRequireMute(bool batchMode, string argument, bool expected)
    {
        Assert.That(BackgroundTestAudio.ShouldMute(batchMode, new[] { argument }), Is.EqualTo(expected));
    }

    [Test]
    public void ImportWorkersDoNotChangeAudioPolicy()
    {
        Assert.That(BackgroundTestAudio.ShouldMute(true, new[] { "-runTests" }, true), Is.False);
        Assert.That(BackgroundTestAudio.ShouldMute(true, new string[0], true), Is.False);
    }

    [Test]
    public void BackgroundEditorHasProcessLocalMuteEnabled()
    {
        if (!BackgroundTestAudio.ShouldMute(UnityEngine.Application.isBatchMode, System.Environment.GetCommandLineArgs()))
            Assert.Ignore("This assertion requires an automated editor session.");

        Assert.That(UnityEngine.AudioListener.volume, Is.Zero);
    }
}
