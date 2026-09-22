using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using Assets.Scripts.Telemetry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using Wism.Client.Api.Telemetry;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.Modules;
using Wism.Companion.Shared.Events;

public sealed partial class ArmyUiInputTests
{
    private sealed class SnapshotSink : ITelemetryPublisher
    {
        public MapSnapshot Last;
        public void Publish(object payload) { Last = (MapSnapshot)payload; }
    }

    private static JObject SnapshotState()
    {
        var state = JObject.Parse(JsonConvert.SerializeObject(Game.Current.Snapshot()));
        state.Remove("Timestamp");
        return state;
    }

    [UnityTest]
    public IEnumerator SnapshotDemand_SkipsUnusedOverlayAndPreservesGameState()
    {
        unity.enabled = false;
        var checkpoint = Environment.GetEnvironmentVariable("WISM_SNAPSHOT_BENCHMARK_CHECKPOINT");
        var fixture = Game.Current.Snapshot();
        var fixtureWorld = ModFactory.WorldPath;
        try
        {
            if (!string.IsNullOrEmpty(checkpoint))
            {
                Assert.That(new FileInfo(checkpoint).Length, Is.LessThanOrEqualTo(8 * 1024 * 1024));
                var saved = JObject.Parse(File.ReadAllText(checkpoint));
                var game = (saved["WismGameEntity"] ?? saved).ToObject<GameEntity>();
                ModFactory.WorldPath = game.World.Name;
                ModFactory.ResetCache();
                GameFactory.Load(game);
            }
            VerifySnapshotDemand();
        }
        finally
        {
            if (!string.IsNullOrEmpty(checkpoint))
            {
                ModFactory.WorldPath = fixtureWorld;
                ModFactory.ResetCache();
                GameFactory.Load(fixture);
            }
        }
        yield return null;
    }

    private void VerifySnapshotDemand()
    {
        TestContext.WriteLine($"Snapshot world={World.Current.Name}, cities={World.Current.GetCities().Count}, armies={Game.Current.Players.Sum(p => p.GetArmies().Count)}");
        var sink = new SnapshotSink();
        var emitter = new MapSnapshotEmitter(manager.LoggerFactory, publisher: sink);
        bool demand = false;
        var broadcaster = new UnityMapSnapshotBroadcaster(new MapSnapshotBuilder(), emitter, () => demand);
        var before = SnapshotState();
        broadcaster.TryEmitSnapshot();
        Assert.That(sink.Last, Is.Not.Null);
        Assert.That(sink.Last.Influence, Is.Null);
        var basic = JObject.FromObject(sink.Last);
        basic.Remove("Timestamp"); basic.Remove("Influence");
        demand = true;
        broadcaster.TryEmitSnapshot();
        Assert.That(sink.Last.Influence, Is.Not.Null);
        var full = JObject.FromObject(sink.Last);
        full.Remove("Timestamp"); full.Remove("Influence");
        Assert.That(JToken.DeepEquals(basic, full), Is.True, "Base map must remain identical.");

        // Alternate equal workloads after warmup; timing is evidence, not a flaky pass threshold.
        double withOverlay = 0, withoutOverlay = 0;
        for (int i = 0; i < 20; i++)
        {
            demand = i % 2 == 0;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            broadcaster.TryEmitSnapshot();
            timer.Stop();
            if (demand) withOverlay += timer.Elapsed.TotalMilliseconds;
            else withoutOverlay += timer.Elapsed.TotalMilliseconds;
        }
        TestContext.WriteLine($"Snapshot demand comparison, 10 each: overlay={withOverlay:F3}ms, no-viewer={withoutOverlay:F3}ms");
        demand = false;
        broadcaster.TryEmitSnapshot();
        Assert.That(sink.Last.Influence, Is.Null);
        new UnityMapSnapshotBroadcaster(new MapSnapshotBuilder(), emitter).TryEmitSnapshot();
        Assert.That(sink.Last.Influence, Is.Not.Null, "Legacy callers retain the overlay.");
        Assert.That(JToken.DeepEquals(before, SnapshotState()), Is.True, "Snapshot work must not mutate gameplay or RNG.");
    }

    [UnityTest]
    public IEnumerator SnapshotDemand_TracksSocketJoinDisconnectAndFallback()
    {
        unity.enabled = false;
        using (var publisher = new UnitySocketTelemetryPublisher(manager.LoggerFactory, port: 0))
        using (var client = new ClientWebSocket())
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            Assert.That(publisher.NeedsInfluenceSnapshot, Is.False);
            var listener = (TcpListener)typeof(UnitySocketTelemetryPublisher)
                .GetField("listener", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(publisher);
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var connect = client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/gameHub"), timeout.Token);
            while (!connect.IsCompleted) yield return null;
            connect.GetAwaiter().GetResult();
            var hello = Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e");
            var send = client.SendAsync(new ArraySegment<byte>(hello), WebSocketMessageType.Text, true, timeout.Token);
            while (!send.IsCompleted) yield return null;
            send.GetAwaiter().GetResult();
            while (!publisher.NeedsInfluenceSnapshot && !timeout.IsCancellationRequested) yield return null;
            Assert.That(publisher.NeedsInfluenceSnapshot, Is.True);
            client.Abort();
            while (publisher.NeedsInfluenceSnapshot && !timeout.IsCancellationRequested) yield return null;
            Assert.That(publisher.NeedsInfluenceSnapshot, Is.False);

            // A bound port forces the existing named-pipe fallback, whose demand is unknown.
            using (var fallback = new UnitySocketTelemetryPublisher(manager.LoggerFactory, port: port))
                Assert.That(fallback.NeedsInfluenceSnapshot, Is.True);
            publisher.Dispose();
            Assert.That(publisher.NeedsInfluenceSnapshot, Is.False);
        }
    }
}
