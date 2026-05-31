using NUnit.Framework;
using Wism.Client.Test.AgentPlayground;

namespace Wism.Client.Test.AgentPlayground.Captures.CapturedAsciiWin;

[TestFixture]
public sealed class CapturedAsciiWinTests
{
    [Test]
    public void CapturedAsciiWin_MatchesRecordedOutcome()
    {
        var result = CaptureTestRunner.Verify("CapturedAsciiWin");
        Assert.That(result.Passed, Is.True, result.Message);
    }
}