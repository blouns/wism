using System;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public class ArtifactFactory
    {
        public static Artifact Load(ArtifactEntity snapshot, Tile tile = null)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var artifact = Artifact.Create(
                ModFactory.FindArtifactInfo(snapshot.ArtifactShortName));

            // Artifact details
            artifact.Id = snapshot.Id;
            artifact.Tile = tile;

            // Player
            if (snapshot.PlayerIndex >= 0)
            {
                artifact.Player = Game.Current.Players[snapshot.PlayerIndex];
            }

            return artifact;
        }
    }
}