namespace Wism.Client.Api.Telemetry
{
    public interface ITelemetryPublisher
    {
        void Publish(object payload);
    }
}
