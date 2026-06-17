using System.IO;
using NUnit.Framework;
using WismCompanion.App;

namespace WismCompanion.Tests
{
    public sealed class SignalRHostLauncherTests
    {
        [TestCase("signalr://localhost:5000/gameHub", true, "localhost", 5000)]
        [TestCase("signalr+ws://127.0.0.1:5000/gameHub", true, "127.0.0.1", 5000)]
        [TestCase("ws://localhost:5000/gameHub", false, null, 0)]
        [TestCase("localhost:5000/gameHub", false, null, 0)]
        [TestCase("http://127.0.0.1:5000/gameHub", false, null, 0)]
        [TestCase("wss://example.test/gameHub", false, null, 0)]
        [TestCase("pipe://wism-commands", false, null, 0)]
        public void TryGetBridgeEndpoint_OnlyAcceptsExplicitLocalSignalRBridge(
            string endpoint,
            bool expected,
            string expectedHost,
            int expectedPort)
        {
            var actual = SignalRHostLauncher.TryGetBridgeEndpoint(endpoint, out var host, out var port);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(host, Is.EqualTo(expectedHost));
            Assert.That(port, Is.EqualTo(expectedPort));
        }

        [TestCase("signalr://localhost:5000/gameHub", "ws://localhost:5000/gameHub")]
        [TestCase("signalr+ws://localhost:5000/gameHub", "ws://localhost:5000/gameHub")]
        [TestCase("signalr+wss://example.test/gameHub", "wss://example.test/gameHub")]
        [TestCase("ws://localhost:5000/gameHub", null)]
        public void TryNormalizeBridgeUrl_OnlyNormalizesSignalRBridgeScheme(string endpoint, string expected)
        {
            var actual = SignalRHostLauncher.TryNormalizeBridgeUrl(endpoint, out var normalized);

            Assert.That(actual, Is.EqualTo(expected != null));
            Assert.That(normalized, Is.EqualTo(expected));
        }

        [Test]
        public void FindRepoRoot_WalksUpToSignalRHostProject()
        {
            var root = Path.Combine(Path.GetTempPath(), "wism-companion-launcher-test-" + Path.GetRandomFileName());
            var start = Path.Combine(root, "WismCompanion", "Build", "Win64", "WismCompanion_Data");
            Directory.CreateDirectory(start);
            Directory.CreateDirectory(Path.Combine(root, "WismClient", "Wism.SignalR.Host"));

            try
            {
                Assert.That(SignalRHostLauncher.FindRepoRoot(start), Is.EqualTo(root));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
