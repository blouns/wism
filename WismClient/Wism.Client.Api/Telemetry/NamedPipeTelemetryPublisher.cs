using System;
using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Wism.Client.Common;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Api.Telemetry
{
    public class NamedPipeTelemetryPublisher : ITelemetryPublisher
    {
        private const string PipeName = "wism-commands";

        private readonly IWismLogger logger;
        private readonly TelemetryContext telemetryContext;
        private NamedPipeClientStream pipe;
        private StreamWriter writer;
        private bool connected;
        private bool warned;
        private bool hasPublished;

        public NamedPipeTelemetryPublisher(
            IWismLoggerFactory loggerFactory,
            TelemetryContext telemetryContext = null)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.telemetryContext = telemetryContext;
        }

        public void Publish(object payload)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            try
            {
                Connect();

                if (!connected || writer == null)
                {
                    return;
                }

                ApplyTelemetry(payload);

                var envelope = new
                {
                    Type = payload.GetType().Name,
                    Payload = payload
                };

                var json = JsonConvert.SerializeObject(envelope, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                writer.WriteLine(json);

                if (!hasPublished)
                {
                    logger.LogInformation($"First telemetry payload sent to companion: {payload.GetType().Name}");
                    hasPublished = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to publish telemetry payload: {ex.Message}");
            }
        }

        private void Connect()
        {
            if (connected)
            {
                return;
            }

            try
            {
                pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipe.Connect(200);
                writer = new StreamWriter(pipe) { AutoFlush = true };
                connected = true;
                logger.LogInformation($"Connected to companion pipe: {PipeName}");
            }
            catch (IOException ex)
            {
                if (!warned)
                {
                    logger.LogWarning($"Companion pipe not available (ignored): {ex.Message}");
                    warned = true;
                }
            }
        }

        private void ApplyTelemetry(object payload)
        {
            if (telemetryContext is null)
            {
                return;
            }

            if (payload is CommandExecutedEvent command && command.Telemetry is null)
            {
                command.Telemetry = telemetryContext;
            }
            else if (payload is MapSnapshot snapshot && snapshot.Telemetry is null)
            {
                snapshot.Telemetry = telemetryContext;
            }
        }
    }
}
