using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Core
{
    public class ArtifactBoon : IBoon
    {
        public ArtifactBoon(Artifact artifact)
        {
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
        }

        public bool IsDefended => true;

        public Artifact Artifact { get; }

        public object Result { get; set; }

        public object Redeem(Tile target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!target.HasItems())
            {
                target.Items = new List<IItem>();
            }

            target.AddItem(Artifact);

            Result = Artifact;
            return Artifact;
        }
    }
}
