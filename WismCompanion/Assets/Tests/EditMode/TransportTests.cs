using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Companion.Shared.Events;
using WismCompanion.Transport;

namespace WismCompanion.Tests
{
    public sealed class TransportTests
    {
        [TestCase(null, "ws://localhost:5000/gameHub")]
        [TestCase("", "ws://localhost:5000/gameHub")]
        [TestCase("localhost:5000/gameHub", "ws://localhost:5000/gameHub")]
        [TestCase("http://localhost:5000/gameHub", "ws://localhost:5000/gameHub")]
        [TestCase("https://example.test/gameHub", "wss://example.test/gameHub")]
        [TestCase("ws://already/gameHub", "ws://already/gameHub")]
        [TestCase("wss://already/gameHub", "wss://already/gameHub")]
        public void SignalRJsonClient_NormalizesHubUrls(string raw, string expected)
        {
            Assert.That(SignalRJsonClient.NormalizeUrl(raw), Is.EqualTo(expected));
        }

        [Test]
        public void SignalRJsonClient_DecodesMapSnapshotInvocation()
        {
            var client = new SignalRJsonClient("localhost:5000/gameHub");

            Assert.That(client.TryDecodeFrame(Frame("OnMapSnapshot", CompanionStateTests.Map("alpha", 5, 6))), Is.True);

            Assert.That(client.TryDequeue(out var message), Is.True);
            Assert.That(message.Kind, Is.EqualTo(InboundMessage.MessageKind.MapSnapshot));
            Assert.That(message.Map.Width, Is.EqualTo(5));
            Assert.That(message.Map.Telemetry.ChannelId, Is.EqualTo("alpha"));
        }

        [Test]
        public void SignalRJsonClient_DecodesCommandInvocation()
        {
            var client = new SignalRJsonClient("localhost:5000/gameHub");

            Assert.That(client.TryDecodeFrame(Frame("OnCommandExecuted", CompanionStateTests.Command("beta", "Attack"))), Is.True);

            Assert.That(client.TryDequeue(out var message), Is.True);
            Assert.That(message.Kind, Is.EqualTo(InboundMessage.MessageKind.Command));
            Assert.That(message.Command.CommandType, Is.EqualTo("Attack"));
            Assert.That(message.Command.Telemetry.ChannelId, Is.EqualTo("beta"));
        }

        [Test]
        public void SignalRJsonClient_IgnoresMalformedOrUnknownFrames()
        {
            var client = new SignalRJsonClient("localhost:5000/gameHub");

            Assert.That(client.TryDecodeFrame("{not-json"), Is.False);
            Assert.That(client.TryDecodeFrame(Frame("Unknown", CompanionStateTests.Map("alpha", 1, 1))), Is.False);
            Assert.That(client.TryDequeue(out _), Is.False);
        }

        [Test]
        public void NamedPipeTransport_UsesDefaultAndTrimmedPipeNames()
        {
            Assert.That(new NamedPipeTransport(null).Endpoint, Is.EqualTo("pipe://wism-commands"));
            Assert.That(new NamedPipeTransport(" custom-pipe ").Endpoint, Is.EqualTo("pipe://custom-pipe"));
        }

        [Test]
        public void NamedPipeTransport_DecodesMapSnapshotEnvelope()
        {
            var transport = new NamedPipeTransport("test-pipe");

            Assert.That(transport.TryDecodeEnvelope(Envelope(nameof(MapSnapshot), CompanionStateTests.Map("alpha", 2, 3))), Is.True);

            Assert.That(transport.TryDequeue(out var message), Is.True);
            Assert.That(message.Kind, Is.EqualTo(InboundMessage.MessageKind.MapSnapshot));
            Assert.That(message.Map.Height, Is.EqualTo(3));
        }

        [Test]
        public void NamedPipeTransport_DecodesCommandEnvelope()
        {
            var transport = new NamedPipeTransport("test-pipe");

            Assert.That(transport.TryDecodeEnvelope(Envelope(nameof(CommandExecutedEvent), CompanionStateTests.Command("beta", "EndTurn"))), Is.True);

            Assert.That(transport.TryDequeue(out var message), Is.True);
            Assert.That(message.Kind, Is.EqualTo(InboundMessage.MessageKind.Command));
            Assert.That(message.Command.CommandType, Is.EqualTo("EndTurn"));
        }

        [Test]
        public void NamedPipeTransport_IgnoresInvalidOrIncompleteEnvelopes()
        {
            var transport = new NamedPipeTransport("test-pipe");

            Assert.That(transport.TryDecodeEnvelope("{not-json"), Is.False);
            Assert.That(transport.TryDecodeEnvelope(JsonConvert.SerializeObject(new { Type = nameof(MapSnapshot) })), Is.False);
            Assert.That(transport.TryDecodeEnvelope(Envelope("Unknown", CompanionStateTests.Map("alpha", 1, 1))), Is.False);
            Assert.That(transport.TryDequeue(out _), Is.False);
        }

        [Test]
        public void InboundMessage_FactoriesSetExclusivePayloads()
        {
            var map = CompanionStateTests.Map("alpha", 1, 1);
            var command = CompanionStateTests.Command("alpha", "Move");

            var mapMessage = InboundMessage.ForMap(map);
            var commandMessage = InboundMessage.ForCommand(command);

            Assert.That(mapMessage.Kind, Is.EqualTo(InboundMessage.MessageKind.MapSnapshot));
            Assert.That(mapMessage.Map, Is.SameAs(map));
            Assert.That(mapMessage.Command, Is.Null);
            Assert.That(commandMessage.Kind, Is.EqualTo(InboundMessage.MessageKind.Command));
            Assert.That(commandMessage.Command, Is.SameAs(command));
            Assert.That(commandMessage.Map, Is.Null);
        }

        private static string Frame(string target, object payload) =>
            JsonConvert.SerializeObject(new { type = 1, target, arguments = new[] { payload } });

        private static string Envelope(string type, object payload) =>
            JsonConvert.SerializeObject(new { Type = type, Payload = payload });
    }
}
