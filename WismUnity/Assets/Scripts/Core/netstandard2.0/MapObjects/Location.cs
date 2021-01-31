using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Location : MapObject
    {
        protected LocationInfo info;
        protected Terrain terrain;
        ISearchable[] searchStrategies;
        bool searched;

        public override string ShortName { get => Info.ShortName; }

        public bool Searched { get => this.searched; }

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
            var location = new Location(info);
            location.searchStrategies = new ISearchable[]
            {
                new SearchTemple(),
                new SearchRuins(),
                new SearchSage(),
                new SearchLibrary()
            };

            return location;
        }

        public Location Clone()
        {
            return Create(this.Info);
        }

        public bool Search(List<Army> armies, out object result)
        {
            foreach (var searchable in searchStrategies)
            {
                if (searchable.CanSearchKind(Kind))
                {
                    var success = searchable.Search(armies, searched, out result);
                    if (success)
                    {
                        searched = true;
                    }
                    return success;
                }
            }

            throw new InvalidOperationException("No searchable strategies for this location kind: " + Kind);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Location;
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

        public override string ToString()
        {
            return $"{ShortName}-{Kind}({Id})";
        }
    }
}
