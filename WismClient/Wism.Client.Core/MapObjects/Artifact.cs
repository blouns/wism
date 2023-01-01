using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Artifact : MapObject
    {
        public Artifact(ArtifactInfo info)
        {
            this.Info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public override string ShortName => this.Info.ShortName;

        public ArtifactInfo Info { get; set; }

        public int CombatBonus { get => this.Info.CombatBonus; }

        public int CommandBonus { get => this.Info.CommandBonus; }

        public string CompanionInteraction { get => this.Info.Interaction; }

        public void Drop(Hero hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            var tile = hero.Tile;
            tile.Items.Add(this);
            
            // Add for tracking
            World.Current.AddLooseItem(this, tile);
        }

        public void Take(Hero hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            if (hero.Tile.HasItems() &&
                hero.Tile.ContainsItem(this))
            {
                if (!hero.HasItems())
                {
                    hero.Items = new List<Artifact>();
                }
                hero.Items.Add(this);
                hero.Tile.RemoveItem(this);

                // Remove from tracking
                World.Current.RemoveLooseItem(this, hero.Tile);
            }
        }

        public static Artifact Create(ArtifactInfo info)
        {
            var artifact = new Artifact(info);
            artifact.DisplayName = info.DisplayName;

            return artifact;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Artifact;
            if (other == null)
            {
                return false;
            }

            return this.ShortName == other.ShortName;
        }

        public override int GetHashCode()
        {
            return this.ShortName.GetHashCode();
        }
    }
}
