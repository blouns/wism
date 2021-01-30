using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public abstract class Location : MapObject, ISearchable
    {
        protected LocationInfo info;
        protected Terrain terrain;

        public override string ShortName { get => Info.ShortName; }

        public Terrain Terrain
        {
            get
            { 
                if (this.terrain == null)
                {
                    this.terrain = MapBuilder.TerrainKinds[this.info.Terrain];
                }
                return this.terrain;
            }
        }

        public LocationInfo Info
        {
            get
            {
                if (this.info == null)
                {
                    this.info = MapBuilder.FindLocationInfo(this.ShortName);
                }
                return info;
            }
        }

        public string Kind { get => Info.Kind; }

        protected Location(LocationInfo info)
        {
            this.info = info ?? throw new System.ArgumentNullException(nameof(info));
            this.DisplayName = info.DisplayName;
        }

        public static Location Create(LocationInfo info)
        {
            // TODO: Simplify; favor composition over inheritance
            Location[] locations = new Location[]
            {
                new Temple(info),
                new Ruins(info),
                new Sage(info),
                new Library(info)
            };

            foreach (var location in locations)
            {
                if (location.CanSearchKind(info.Kind))
                {
                    return location;
                }
            }

            throw new ArgumentException("No searchable location kind found for: " + info.Kind);
        }

        public Location Clone()
        {
            return Create(this.Info);
        }

        public abstract bool Search(List<Army> armies, out object result);

        public abstract SearchStatus GetStatus();

        public override bool Equals(object obj)
        {
            var other = obj as Temple;
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

        public abstract bool CanSearchKind(string kind);
    }
}
