using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Wism.Companion.Shared.Events;
using Wism.Client.Common;

namespace Wism.Client.Api.Telemetry
{
    public class CommandIpcPublisher
    {
        private NamedPipeClientStream? _pipe;
        private StreamWriter? _writer;
        private bool _connected = false;
        private bool _warned = false;
        private bool _hasPublished = false;
        private readonly IWismLogger _logger;

        public CommandIpcPublisher(IWismLoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger();
        }

        public void Connect()
        {
            if (_connected)
                return;

            try
            {
                _pipe = new NamedPipeClientStream(".", "wism-commands", PipeDirection.Out);
                _pipe.Connect(200);
                _writer = new StreamWriter(_pipe) { AutoFlush = true };
                _connected = true;
                _logger.LogInformation("Connected to companion pipe: wism-commands");
            }
            catch (IOException ex)
            {
                if (!_warned)
                {
                    _logger.LogWarning($"Companion pipe not available (ignored): {ex.Message}");
                    _warned = true;
                }
            }
        }

        public void Publish(CommandExecutedEvent evt)
        {
            try
            {
                Connect();

                if (!_connected || _writer == null)
                    return;

                var envelope = new
                {
                    Type = nameof(CommandExecutedEvent),
                    Payload = evt
                };

                string json = JsonConvert.SerializeObject(envelope, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                _writer.WriteLine(json);
                // Only log the first publish as confirmation
                if (!_hasPublished)
                {
                    _logger.LogInformation($"First command sent to companion: {evt.CommandType}");
                    _hasPublished = true;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to publish command event: {ex.Message}");
            }
        }
    }
}
