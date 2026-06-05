using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Api.Telemetry;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Telemetry;

namespace Wism.Client.Test.Telemetry;

[TestFixture]
public class MultiChannelTelemetryTests
{
    [Test]
    public void MapSnapshot_WithoutTelemetry_DeserializesAsDefaultLocalChannel()
    {
        var snapshot = JsonConvert.DeserializeObject<MapSnapshot>(
            """{"Width":3,"Height":2,"Tiles":[],"Armies":[],"Cities":[],"Items":[],"Locations":[]}""");

        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Telemetry, Is.Null);
        Assert.That(TelemetryContext.ChannelIdOrDefault(snapshot.Telemetry), Is.EqualTo("default"));
        Assert.That(TelemetryContext.SessionIdOrDefault(snapshot.Telemetry), Is.EqualTo("local"));
    }

    [Test]
    public void CommandPublisher_CanUseInMemoryPublisherWithoutCompanion()
    {
        var telemetry = new TelemetryContext
        {
            ChannelId = "test-channel",
            SessionId = "test-session",
            SourceKind = "Test",
            SourceName = "Unit",
            StartedAtUtc = DateTime.UtcNow
        };
        var publisher = new RecordingTelemetryPublisher(telemetry);
        var processor = new StandardProcessor(
            new WismLoggerFactory(),
            new CommandIpcPublisher(new WismLoggerFactory(), telemetry, publisher));

        var result = processor.Execute(new TestCommand());

        Assert.That(result, Is.EqualTo(ActionState.Succeeded));
        Assert.That(publisher.Payloads, Has.Count.EqualTo(1));
        var evt = (CommandExecutedEvent)publisher.Payloads[0];
        Assert.That(evt.Telemetry?.ChannelId, Is.EqualTo("test-channel"));
    }

    [Test]
    public void TelemetryChannelRegistry_PartitionsInterleavedSnapshots()
    {
        var registry = new TelemetryChannelRegistry();
        var first = Snapshot("playground:a", DateTime.UtcNow.AddSeconds(-1));
        var second = Snapshot("unity:b", DateTime.UtcNow);

        Assert.That(registry.Register(first), Is.EqualTo("playground:a"));
        Assert.That(registry.Register(second), Is.EqualTo("unity:b"));

        Assert.That(registry.Channels, Is.EquivalentTo(new[] { "playground:a", "unity:b" }));
        Assert.That(registry.GetLatestMap("playground:a"), Is.SameAs(first));
        Assert.That(registry.GetLatestMap("unity:b"), Is.SameAs(second));
        Assert.That(registry.GetLatestMap(null), Is.Null);
    }

    private static MapSnapshot Snapshot(string channel, DateTime timestamp)
    {
        return new MapSnapshot
        {
            Width = 1,
            Height = 1,
            Timestamp = timestamp,
            Telemetry = new TelemetryContext
            {
                ChannelId = channel,
                SessionId = $"{channel}:session",
                SourceKind = "Test",
                SourceName = channel,
                StartedAtUtc = timestamp
            }
        };
    }

    private sealed class RecordingTelemetryPublisher : ITelemetryPublisher
    {
        private readonly TelemetryContext telemetryContext;

        public RecordingTelemetryPublisher(TelemetryContext telemetryContext)
        {
            this.telemetryContext = telemetryContext;
        }

        public List<object> Payloads { get; } = new();

        public void Publish(object payload)
        {
            if (payload is CommandExecutedEvent command && command.Telemetry is null)
            {
                command.Telemetry = telemetryContext;
            }

            Payloads.Add(payload);
        }
    }

    private sealed class TestCommand : ICommandAction
    {
        public ActionState Execute()
        {
            return ActionState.Succeeded;
        }
    }
}
