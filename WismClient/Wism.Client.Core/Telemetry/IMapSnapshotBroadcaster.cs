namespace Wism.Client.Core.Telemetry
{
    public interface IMapSnapshotBroadcaster
    {
        void TryEmitSnapshot();
    }
}
