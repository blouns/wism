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

            if (!this.Tile.HasItems())
            {
                this.Tile.Items = new List<IItem>();
            }

            this.Tile.Items.Add(this);
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
                this.Tile.Items.Remove(this);

                if (!hero.HasItems())
                {
                    hero.Items = new List<IItem>();
                }
                hero.Items.Add(this);
            }
        }

        public static Artifact Create(ArtifactInfo info)
        {
            return new Artifact(info);
        }
    }
}
