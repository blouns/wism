using System;
using System.IO;
using System.IO.Pipes;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Companion.Shared.Events;

namespace WismCompanion.Transport
{
    /// <summary>
    /// Reads telemetry straight from the game's named pipe by acting as the pipe <b>server</b> — the
    /// role <c>Wism.SignalR.Host</c> normally plays — so no separate bridge process is needed. Mirrors
    /// that host's NamedPipeListenerService: same pipe name (<c>wism-commands</c>) and the same
    /// line-delimited <c>{ Type, Payload }</c> envelope. Desktop/Windows only.
    ///
    /// Launch order is forgiving: the companion can listen first and the game connects when it starts,
    /// or vice-versa (the game's publisher retries on each snapshot). Multiple server instances allow
    /// concurrent producers (multiplexed by channel).
    ///
    /// Note: only one server can own a pipe name. Don't run this alongside <c>Wism.SignalR.Host</c>
    /// (which also owns <c>wism-commands</c>) — use one or the other.
    /// </summary>
    public sealed class NamedPipeTransport : ICompanionTransport
    {
        private const int ListenerCount = 4;

        private readonly string pipeName;
        private readonly ConcurrentQueue<InboundMessage> inbound = new();
        private readonly JsonSerializer serializer;

        private CancellationTokenSource cts;
        private Task runTask;
        private int connectedClients;

        public NamedPipeTransport(string pipeName)
        {
            this.pipeName = string.IsNullOrWhiteSpace(pipeName) ? "wism-commands" : pipeName.Trim();
            serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        public CompanionConnectionStatus Status { get; private set; } = CompanionConnectionStatus.Disconnected;

        public string StatusDetail { get; private set; } = "Idle";

        public string Endpoint => $"pipe://{pipeName}";

        public bool TryDequeue(out InboundMessage message) => inbound.TryDequeue(out message);

        // Exposes deterministic envelope parsing for editor validation and offline replay.
        public bool TryDecodeEnvelope(string line)
        {
            var before = inbound.Count;
            HandleEnvelope(line);
            return inbound.Count > before;
        }

        public void Start()
        {
            if (runTask != null && !runTask.IsCompleted)
            {
                return;
            }

            cts = new CancellationTokenSource();
            var token = cts.Token;
            SetStatus(CompanionConnectionStatus.Connecting, $"Listening on {Endpoint}…");

            var loops = new Task[ListenerCount];
            for (var i = 0; i < ListenerCount; i++)
            {
                loops[i] = Task.Run(() => ListenLoopAsync(token), token);
            }

            runTask = Task.WhenAll(loops);
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

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream server = null;
                try
                {
                    // MaxAllowedServerInstances lets our own instances coexist and tolerates leftovers
                    // from a prior run; PipeOptions.Asynchronous + token registration ensures the
                    // blocking wait is released and the instance is freed on shutdown.
                    server = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    using (token.Register(() => SafeDispose(server)))
                    {
                        await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                    }

                    OnClientConnected();
                    try
                    {
                        await ReadMessagesAsync(server, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        OnClientDisconnected();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    SetStatus(CompanionConnectionStatus.Reconnecting, FriendlyError(ex));
                    try
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                finally
                {
                    SafeDispose(server);
                }
            }
        }

        private async Task ReadMessagesAsync(NamedPipeServerStream server, CancellationToken token)
        {
            using var reader = new StreamReader(server, Encoding.UTF8);
            while (!token.IsCancellationRequested && server.IsConnected)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break; // producer closed the pipe
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    HandleEnvelope(line);
                }
            }
        }

        private void HandleEnvelope(string line)
        {
            JObject envelope;
            try
            {
                envelope = JObject.Parse(line);
            }
            catch
            {
                return;
            }

            var type = envelope.Value<string>("Type");
            var payload = envelope["Payload"];
            if (payload == null)
            {
                return;
            }

            if (type == nameof(MapSnapshot))
            {
                var map = payload.ToObject<MapSnapshot>(serializer);
                if (map != null)
                {
                    inbound.Enqueue(InboundMessage.ForMap(map));
                }
            }
            else if (type == nameof(CommandExecutedEvent))
            {
                var command = payload.ToObject<CommandExecutedEvent>(serializer);
                if (command != null)
                {
                    inbound.Enqueue(InboundMessage.ForCommand(command));
                }
            }
        }

        private void OnClientConnected()
        {
            Interlocked.Increment(ref connectedClients);
            SetStatus(CompanionConnectionStatus.Connected, "Game connected");
        }

        private void OnClientDisconnected()
        {
            if (Interlocked.Decrement(ref connectedClients) <= 0)
            {
                SetStatus(CompanionConnectionStatus.Connecting, $"Listening on {Endpoint}…");
            }
        }

        private static void SafeDispose(NamedPipeServerStream server)
        {
            try
            {
                server?.Dispose();
            }
            catch
            {
                // ignore disposal races
            }
        }

        private string FriendlyError(Exception ex)
        {
            if (ex is IOException && ex.Message.IndexOf("busy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Pipe {pipeName} is in use — close Wism.SignalR.Host or another companion (restart Unity if it persists).";
            }

            return $"Pipe error: {ex.Message}";
        }

        private void SetStatus(CompanionConnectionStatus status, string detail)
        {
            Status = status;
            StatusDetail = detail;
        }
    }
}
