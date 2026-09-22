using System;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.Api.Telemetry;
using Wism.Client.Core.Telemetry;

namespace Assets.Scripts.Telemetry
{
    public class UnityMapSnapshotBroadcaster : IMapSnapshotBroadcaster
    {
        private readonly MapSnapshotBuilder builder;
        private readonly MapSnapshotEmitter emitter;
        private readonly Func<bool> includeInfluence;

        public UnityMapSnapshotBroadcaster(MapSnapshotBuilder builder, MapSnapshotEmitter emitter,
            Func<bool> includeInfluence = null)
        {
            this.builder = builder;
            this.emitter = emitter;
            this.includeInfluence = includeInfluence;
        }

        public void TryEmitSnapshot()
        {
            if (builder.TryBuild(out var snapshot))
            {
                snapshot.InvertYAxis = true;

                // Attach the current player's spatial influence field for the overlay.
                // Observation-only: a fresh deterministic flood, no effect on AI decisions.
                // Retain the basic map for late joiners without flooding an unseen overlay.
                if (includeInfluence == null || includeInfluence())
                    snapshot.Influence = InfluenceFieldExporter.BuildForCurrentPlayer();

                emitter.Publish(snapshot);
            }
        }
    }
}
