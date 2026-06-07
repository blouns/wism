using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Companion.Shared.Events;

namespace WismCompanion.Transport
{
    /// <summary>
    /// Minimal SignalR "json" hub-protocol client over a raw WebSocket.
    ///
    /// The WISM SignalR host (Wism.SignalR.Host) configures the hub with
    /// <c>AddNewtonsoftJsonProtocol</c> and <c>TypeNameHandling.Auto</c>, so this client
    /// deserializes invocation arguments with Newtonsoft using the same setting. Using a raw
    /// WebSocket avoids dragging the full Microsoft.AspNetCore.SignalR.Client dependency tree
    /// (and its IL2CPP stripping risk) into the companion.
    ///
    /// Inbound hub invocations are pushed onto a thread-safe queue; the Unity main thread drains
    /// it via <see cref="TryDequeue"/>. No UnityEngine APIs are touched off the main thread.
    /// </summary>
    public sealed class SignalRJsonClient : ICompanionTransport
    {
        private const byte RecordSeparator = 0x1e;

        private readonly string url;
        private readonly ConcurrentQueue<InboundMessage> inbound = new();
        private readonly JsonSerializer serializer;

        private CancellationTokenSource cts;
        private Task runTask;

        public SignalRJsonClient(string hubUrl)
        {
            url = NormalizeUrl(hubUrl);
            serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        public CompanionConnectionStatus Status { get; private set; } = CompanionConnectionStatus.Disconnected;

        public string StatusDetail { get; private set; } = "Idle";

        public string Endpoint => url;

        public bool TryDequeue(out InboundMessage message) => inbound.TryDequeue(out message);

        public void Start()
        {
            if (runTask != null && !runTask.IsCompleted)
            {
                return;
            }

            cts = new CancellationTokenSource();
            var token = cts.Token;
            runTask = Task.Run(() => RunAsync(token));
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
            }
            catch
            {
                // best-effort shutdown
            }
        }

        private async Task RunAsync(CancellationToken token)
        {
            var backoffSeconds = 1.0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    SetStatus(CompanionConnectionStatus.Connecting, $"Connecting to {url}…");
                    using var socket = new ClientWebSocket();
                    socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    await socket.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
                    await SendFrameAsync(socket, "{\"protocol\":\"json\",\"version\":1}", token).ConfigureAwait(false);
                    SetStatus(CompanionConnectionStatus.Connected, "Connected");
                    backoffSeconds = 1.0;
                    await ReceiveLoopAsync(socket, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    SetStatus(CompanionConnectionStatus.Reconnecting, $"Disconnected: {ex.Message}");
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                backoffSeconds = Math.Min(15.0, backoffSeconds * 2.0);
            }

            SetStatus(CompanionConnectionStatus.Disconnected, "Stopped");
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192];
            using var accumulator = new MemoryStream();
            var handshakeSeen = false;

            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closing", token).ConfigureAwait(false);
                    return;
                }

                accumulator.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var data = accumulator.ToArray();
                accumulator.SetLength(0);

                var start = 0;
                for (var i = 0; i < data.Length; i++)
                {
                    if (data[i] != RecordSeparator)
                    {
                        continue;
                    }

                    var frame = Encoding.UTF8.GetString(data, start, i - start);
                    start = i + 1;

                    if (!handshakeSeen)
                    {
                        // The first frame on a fresh connection is the handshake response.
                        handshakeSeen = true;
                        continue;
                    }

                    HandleFrame(frame, socket, token);
                }

                if (start < data.Length)
                {
                    // Preserve a trailing partial frame for the next receive.
                    accumulator.Write(data, start, data.Length - start);
                }
            }
        }

        private void HandleFrame(string frame, ClientWebSocket socket, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(frame))
            {
                return;
            }

            JObject message;
            try
            {
                message = JObject.Parse(frame);
            }
            catch
            {
                return;
            }

            var type = message.Value<int?>("type") ?? 0;
            switch (type)
            {
                case 1: // Invocation
                    DispatchInvocation(message);
                    break;
                case 6: // Ping
                    _ = SendFrameAsync(socket, "{\"type\":6}", token);
                    break;
                case 7: // Close
                    throw new IOException("Server closed the hub connection");
            }
        }

        // Exposes deterministic frame parsing for editor validation and offline replay.
        public bool TryDecodeFrame(string frame)
        {
            var before = inbound.Count;
            HandleFrame(frame, null, CancellationToken.None);
            return inbound.Count > before;
        }

        private void DispatchInvocation(JObject message)
        {
            var target = message.Value<string>("target");
            if (message["arguments"] is not JArray args || args.Count == 0)
            {
                return;
            }

            var payload = args[0];
            switch (target)
            {
                case "OnMapSnapshot":
                    var map = payload.ToObject<MapSnapshot>(serializer);
                    if (map != null)
                    {
                        inbound.Enqueue(InboundMessage.ForMap(map));
                    }

                    break;
                case "OnCommandExecuted":
                    var command = payload.ToObject<CommandExecutedEvent>(serializer);
                    if (command != null)
                    {
                        inbound.Enqueue(InboundMessage.ForCommand(command));
                    }

                    break;
            }
        }

        private async Task SendFrameAsync(ClientWebSocket socket, string payload, CancellationToken token)
        {
            if (socket == null)
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(payload);
            var framed = new byte[bytes.Length + 1];
            Buffer.BlockCopy(bytes, 0, framed, 0, bytes.Length);
            framed[bytes.Length] = RecordSeparator;
            await socket.SendAsync(new ArraySegment<byte>(framed), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
        }

        private void SetStatus(CompanionConnectionStatus status, string detail)
        {
            Status = status;
            StatusDetail = detail;
        }

        public static string NormalizeUrl(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "ws://localhost:5000/gameHub";
            }

            raw = raw.Trim();
            if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return "wss://" + raw.Substring("https://".Length);
            }

            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return "ws://" + raw.Substring("http://".Length);
            }

            if (!raw.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
                !raw.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                return "ws://" + raw;
            }

            return raw;
        }
    }
}
