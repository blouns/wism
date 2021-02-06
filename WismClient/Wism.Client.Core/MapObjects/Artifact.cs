using System;
using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Artifact : MapObject, IItem
    {
        public Artifact(ArtifactInfo info)
        {
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public override string ShortName => Info.ShortName;        

        public ArtifactInfo Info { get; set; }

        public int CombatBonus { get => Info.CombatBonus; }

        public int CommandBonus { get => Info.CommandBonus; }        

        public void Drop(Hero hero)
        {
            if (hero is null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            hero.Tile.AddItem(this);
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
                    hero.Items = new List<IItem>();
                }
                hero.Items.Add(this);
                hero.Tile.RemoveItem(this);
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
