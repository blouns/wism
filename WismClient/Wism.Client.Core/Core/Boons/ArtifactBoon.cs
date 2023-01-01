using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Core
{
    public class ArtifactBoon : IBoon
    {
        public ArtifactBoon(Artifact artifact)
        {
            this.Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
        }

        public Artifact Artifact { get; }

        public bool IsDefended => true;

        public object Result { get; set; }

        public object Redeem(Tile target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!target.HasItems())
            {
                target.Items = new List<Artifact>();
            }

            target.AddItem(this.Artifact);

            this.Result = this.Artifact;
            return this.Artifact;
        }
    }
}