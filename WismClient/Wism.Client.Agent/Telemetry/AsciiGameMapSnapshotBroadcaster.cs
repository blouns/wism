using Wism.Client.AI.InfluenceMaps;
using Wism.Client.Api.Telemetry;
using Wism.Client.Core.Telemetry;

namespace Wism.Client.Agent.Telemetry
{
    public class AsciiGameMapSnapshotBroadcaster : IMapSnapshotBroadcaster
    {
        private readonly MapSnapshotBuilder builder;
        private readonly MapSnapshotEmitter emitter;

        public AsciiGameMapSnapshotBroadcaster(MapSnapshotBuilder builder, MapSnapshotEmitter emitter)
        {
            this.builder = builder;
            this.emitter = emitter;
        }

        public void TryEmitSnapshot()
        {
            if (builder.TryBuild(out var snapshot))
            {
                // Mark for inversion in the companion renderer
                snapshot.InvertYAxis = true;

                // Attach the current player's spatial influence field for the overlay (A3).
                // Observation-only: a fresh deterministic flood, no effect on AI decisions.
                snapshot.Influence = InfluenceFieldExporter.BuildForCurrentPlayer();

                emitter.Publish(snapshot);
            }
        }
    }
}
