using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wism.Client.Api.Telemetry;
using Wism.Client.Common;
using Wism.Companion.Shared.Events;

namespace Assets.Scripts.Telemetry
{
    /// <summary>
    /// Hosts a loopback WebSocket endpoint that speaks the small SignalR JSON subset
    /// consumed by WismCompanion.
    /// </summary>
    public sealed class UnitySocketTelemetryPublisher : ITelemetryPublisher, IDisposable
    {
        private const int DefaultPort = 5000;
        private const string HubPath = "/gameHub";
        private const string WebSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private const byte RecordSeparator = 0x1e;
        private const int MaxClientFrameBytes = 1024 * 1024;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        private readonly IWismLogger logger;
        private readonly TelemetryContext telemetryContext;
        private readonly ITelemetryPublisher fallbackPublisher;
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();
        private readonly object clientsLock = new object();
        private readonly object lifecycleLock = new object();
        private int nextClientId;
        private bool disposed;
        private bool listening;
        private bool warned;
        private bool hasPublished;
        private string latestFrame;

        public UnitySocketTelemetryPublisher(
            IWismLoggerFactory loggerFactory,
            TelemetryContext telemetryContext = null,
            ITelemetryPublisher fallbackPublisher = null,
            int port = DefaultPort)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.telemetryContext = telemetryContext;
            this.fallbackPublisher = fallbackPublisher;

            try
            {
                this.listener = new TcpListener(IPAddress.Loopback, port);
                this.listener.Start();
                this.listening = true;
                Task.Run(() => AcceptLoopAsync(this.cancellationTokenSource.Token));
                this.logger.LogInformation($"WismUnity telemetry socket listening at ws://localhost:{port}{HubPath}");
            }
            catch (Exception ex)
            {
                this.listening = false;
                this.logger.LogWarning($"WismUnity telemetry socket unavailable: {ex.Message}");
            }
        }

        public void Publish(object payload)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var target = GetTarget(payload);
            if (target == null || this.disposed)
            {
                return;
            }

            if (!this.listening)
            {
                this.fallbackPublisher?.Publish(payload);
                return;
            }

            try
            {
                ApplyTelemetry(payload);

                var invocation = new HubInvocation
                {
                    Target = target,
                    Arguments = new object[] { payload }
                };
                var json = JsonConvert.SerializeObject(invocation, JsonSettings);
                var framed = json + (char)RecordSeparator;
                this.latestFrame = framed;

                var clientsSnapshot = GetClientsSnapshot();
                if (clientsSnapshot.Count == 0)
                {
                    return;
                }

                foreach (var client in clientsSnapshot)
                {
                    _ = client.SendTextAsync(framed, this.cancellationTokenSource.Token)
                        .ContinueWith(
                            task =>
                            {
                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    RemoveClient(client.Id);
                                }
                            },
                            TaskScheduler.Default);
                }

                if (!this.hasPublished)
                {
                    this.logger.LogInformation($"First telemetry payload sent to socket clients: {payload.GetType().Name}");
                    this.hasPublished = true;
                }
            }
            catch (Exception ex)
            {
                if (!this.warned)
                {
                    this.logger.LogWarning($"Failed to publish socket telemetry payload: {ex.Message}");
                    this.warned = true;
                }
            }
        }

        public void Dispose()
        {
            lock (this.lifecycleLock)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
            }

            try
            {
                this.cancellationTokenSource.Cancel();
                this.listener?.Stop();
            }
            catch
            {
                // Best-effort shutdown.
            }

            foreach (var client in GetClientsSnapshot())
            {
                client.Dispose();
            }

            this.cancellationTokenSource.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && this.listening)
            {
                TcpClient tcpClient = null;
                try
                {
                    tcpClient = await this.listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleClientAsync(tcpClient, token), token);
                }
                catch
                {
                    tcpClient?.Close();
                    if (!token.IsCancellationRequested)
                    {
                        await Task.Delay(250, token).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            ClientConnection client = null;
            try
            {
                tcpClient.NoDelay = true;
                var stream = tcpClient.GetStream();
                var headers = await ReadHttpHeadersAsync(stream, token).ConfigureAwait(false);
                if (!TryWriteWebSocketHandshake(headers, stream))
                {
                    tcpClient.Close();
                    return;
                }

                var handshakeFrame = await ReadFrameAsync(stream, token).ConfigureAwait(false);
                if (handshakeFrame == null || handshakeFrame.Opcode != WebSocketOpcode.Text)
                {
                    tcpClient.Close();
                    return;
                }

                await WriteFrameAsync(stream, WebSocketOpcode.Text, Encoding.UTF8.GetBytes("{}" + (char)RecordSeparator), token).ConfigureAwait(false);

                client = new ClientConnection(Interlocked.Increment(ref this.nextClientId), tcpClient);
                AddClient(client);
                await DrainClientAsync(client, token).ConfigureAwait(false);
            }
            catch
            {
                // Client disconnects and malformed probes are ignored.
            }
            finally
            {
                if (client != null)
                {
                    RemoveClient(client.Id);
                }
                else
                {
                    tcpClient?.Close();
                }
            }
        }

        private async Task DrainClientAsync(ClientConnection client, CancellationToken token)
        {
            while (!token.IsCancellationRequested && client.IsConnected)
            {
                var frame = await ReadFrameAsync(client.Stream, token).ConfigureAwait(false);
                if (frame == null)
                {
                    return;
                }

                if (frame.Opcode == WebSocketOpcode.Close)
                {
                    await client.SendFrameAsync(WebSocketOpcode.Close, Array.Empty<byte>(), token).ConfigureAwait(false);
                    return;
                }

                if (frame.Opcode == WebSocketOpcode.Ping)
                {
                    await client.SendFrameAsync(WebSocketOpcode.Pong, frame.Payload, token).ConfigureAwait(false);
                }
            }
        }

        private static async Task<string> ReadHttpHeadersAsync(NetworkStream stream, CancellationToken token)
        {
            var bytes = new List<byte>();
            var buffer = new byte[1];

            while (bytes.Count < 16 * 1024)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                bytes.Add(buffer[0]);
                if (bytes.Count >= 4 &&
                    bytes[bytes.Count - 4] == '\r' &&
                    bytes[bytes.Count - 3] == '\n' &&
                    bytes[bytes.Count - 2] == '\r' &&
                    bytes[bytes.Count - 1] == '\n')
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static bool TryWriteWebSocketHandshake(string headers, NetworkStream stream)
        {
            if (string.IsNullOrWhiteSpace(headers))
            {
                return false;
            }

            var lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || !IsExpectedRequestLine(lines[0]))
            {
                return false;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var separator = lines[i].IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                values[lines[i].Substring(0, separator).Trim()] = lines[i].Substring(separator + 1).Trim();
            }

            if (!values.TryGetValue("Sec-WebSocket-Key", out var key) || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var accept = CreateWebSocketAccept(key);
            var response =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                $"Sec-WebSocket-Accept: {accept}\r\n" +
                "\r\n";
            var responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            return true;
        }

        private static bool IsExpectedRequestLine(string requestLine)
        {
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                return false;
            }

            var parts = requestLine.Split(' ');
            return parts.Length >= 2 &&
                string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(parts[1], HubPath, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(parts[1], HubPath + "/", StringComparison.OrdinalIgnoreCase));
        }

        private static string CreateWebSocketAccept(string key)
        {
            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(key + WebSocketGuid));
                return Convert.ToBase64String(hash);
            }
        }

        private static async Task<WebSocketFrame> ReadFrameAsync(NetworkStream stream, CancellationToken token)
        {
            var header = await ReadExactAsync(stream, 2, token).ConfigureAwait(false);
            if (header == null)
            {
                return null;
            }

            var opcode = (WebSocketOpcode)(header[0] & 0x0f);
            var masked = (header[1] & 0x80) != 0;
            ulong length = (ulong)(header[1] & 0x7f);

            if (length == 126)
            {
                var extended = await ReadExactAsync(stream, 2, token).ConfigureAwait(false);
                if (extended == null)
                {
                    return null;
                }

                length = ((ulong)extended[0] << 8) | extended[1];
            }
            else if (length == 127)
            {
                var extended = await ReadExactAsync(stream, 8, token).ConfigureAwait(false);
                if (extended == null)
                {
                    return null;
                }

                length = 0;
                for (var i = 0; i < extended.Length; i++)
                {
                    length = (length << 8) | extended[i];
                }
            }

            if (length > MaxClientFrameBytes)
            {
                return null;
            }

            var mask = masked
                ? await ReadExactAsync(stream, 4, token).ConfigureAwait(false)
                : null;
            var payload = await ReadExactAsync(stream, (int)length, token).ConfigureAwait(false);
            if (payload == null)
            {
                return null;
            }

            if (masked && mask != null)
            {
                for (var i = 0; i < payload.Length; i++)
                {
                    payload[i] = (byte)(payload[i] ^ mask[i % 4]);
                }
            }

            return new WebSocketFrame(opcode, payload);
        }

        private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken token)
        {
            var buffer = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var read = await stream.ReadAsync(buffer, offset, count - offset, token).ConfigureAwait(false);
                if (read == 0)
                {
                    return null;
                }

                offset += read;
            }

            return buffer;
        }

        private static Task WriteFrameAsync(Stream stream, WebSocketOpcode opcode, byte[] payload, CancellationToken token)
        {
            var frame = BuildFrame(opcode, payload);
            return stream.WriteAsync(frame, 0, frame.Length, token);
        }

        private static byte[] BuildFrame(WebSocketOpcode opcode, byte[] payload)
        {
            payload = payload ?? Array.Empty<byte>();
            var headerLength = payload.Length < 126 ? 2 : payload.Length <= ushort.MaxValue ? 4 : 10;
            var frame = new byte[headerLength + payload.Length];
            frame[0] = (byte)(0x80 | (byte)opcode);

            if (payload.Length < 126)
            {
                frame[1] = (byte)payload.Length;
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                frame[1] = 126;
                frame[2] = (byte)((payload.Length >> 8) & 0xff);
                frame[3] = (byte)(payload.Length & 0xff);
            }
            else
            {
                frame[1] = 127;
                var length = (ulong)payload.Length;
                for (var i = 0; i < 8; i++)
                {
                    frame[2 + i] = (byte)((length >> (8 * (7 - i))) & 0xff);
                }
            }

            Buffer.BlockCopy(payload, 0, frame, headerLength, payload.Length);
            return frame;
        }

        private static string GetTarget(object payload)
        {
            if (payload is MapSnapshot)
            {
                return "OnMapSnapshot";
            }

            if (payload is CommandExecutedEvent)
            {
                return "OnCommandExecuted";
            }

            return null;
        }

        private void ApplyTelemetry(object payload)
        {
            if (this.telemetryContext is null)
            {
                return;
            }

            if (payload is CommandExecutedEvent command && command.Telemetry is null)
            {
                command.Telemetry = this.telemetryContext;
            }
            else if (payload is MapSnapshot snapshot && snapshot.Telemetry is null)
            {
                snapshot.Telemetry = this.telemetryContext;
            }
        }

        private void AddClient(ClientConnection client)
        {
            lock (this.clientsLock)
            {
                this.clients[client.Id] = client;
            }

            var frame = this.latestFrame;
            if (!string.IsNullOrEmpty(frame))
            {
                _ = client.SendTextAsync(frame, this.cancellationTokenSource.Token)
                    .ContinueWith(
                        task =>
                        {
                            if (task.IsFaulted || task.IsCanceled)
                            {
                                RemoveClient(client.Id);
                            }
                        },
                        TaskScheduler.Default);
            }
        }

        private void RemoveClient(int id)
        {
            ClientConnection client = null;
            lock (this.clientsLock)
            {
                if (this.clients.TryGetValue(id, out client))
                {
                    this.clients.Remove(id);
                }
            }

            client?.Dispose();
        }

        private List<ClientConnection> GetClientsSnapshot()
        {
            lock (this.clientsLock)
            {
                return new List<ClientConnection>(this.clients.Values);
            }
        }

        private sealed class HubInvocation
        {
            [JsonProperty("type")]
            public int Type { get; set; } = 1;

            [JsonProperty("target")]
            public string Target { get; set; }

            [JsonProperty("arguments")]
            public object[] Arguments { get; set; }
        }

        private sealed class WebSocketFrame
        {
            public WebSocketFrame(WebSocketOpcode opcode, byte[] payload)
            {
                Opcode = opcode;
                Payload = payload;
            }

            public WebSocketOpcode Opcode { get; }

            public byte[] Payload { get; }
        }

        private sealed class ClientConnection : IDisposable
        {
            private readonly TcpClient tcpClient;
            private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
            private bool disposed;

            public ClientConnection(int id, TcpClient tcpClient)
            {
                Id = id;
                this.tcpClient = tcpClient;
                Stream = tcpClient.GetStream();
            }

            public int Id { get; }

            public NetworkStream Stream { get; }

            public bool IsConnected => !this.disposed && this.tcpClient.Connected;

            public Task SendTextAsync(string text, CancellationToken token)
            {
                return SendFrameAsync(WebSocketOpcode.Text, Encoding.UTF8.GetBytes(text), token);
            }

            public async Task SendFrameAsync(WebSocketOpcode opcode, byte[] payload, CancellationToken token)
            {
                if (this.disposed)
                {
                    return;
                }

                await this.sendLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (!this.disposed)
                    {
                        await WriteFrameAsync(this.Stream, opcode, payload, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    this.sendLock.Release();
                }
            }

            public void Dispose()
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                try
                {
                    this.tcpClient.Close();
                }
                catch
                {
                    // Best-effort shutdown.
                }

                this.sendLock.Dispose();
            }
        }

        private enum WebSocketOpcode : byte
        {
            Text = 1,
            Close = 8,
            Ping = 9,
            Pong = 10
        }
    }
}
