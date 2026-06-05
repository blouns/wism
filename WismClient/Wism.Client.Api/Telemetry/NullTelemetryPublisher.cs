namespace Wism.Client.Api.Telemetry
{
    public sealed class NullTelemetryPublisher : ITelemetryPublisher
    {
        public void Publish(object payload)
        {
        }
    }
}
