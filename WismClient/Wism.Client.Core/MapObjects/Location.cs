using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Wism.Client.Core;
using Wism.Client.Modules;

[assembly: InternalsVisibleTo("Wism.Client.Test")]

namespace Wism.Client.MapObjects
{
    public class Location : MapObject
    {
        protected LocationInfo info;
        private ISearchable searchStrategy;
        protected Terrain terrain;

        protected Location(LocationInfo info)
        {
            this.info = info ?? throw new ArgumentNullException(nameof(info));
            this.DisplayName = info.DisplayName;
        }

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

                return this.info;
            }
        }

        public override string ShortName => this.Info.ShortName;
        public string Kind => this.Info.Kind;
        public IBoon Boon { get; set; }
        public string Monster { get; set; }
        public bool Searched { get; set; }

        public static Location Create(LocationInfo info)
        {
            var location = new Location(info);
            switch (info.Kind)
            {
                case "Temple":
                    location.searchStrategy = SearchTemple.Instance;
                    break;
                case "Ruins":
                case "Tomb":
                    location.searchStrategy = SearchRuins.Instance;
                    break;
                case "Sage":
                    location.searchStrategy = SearchSage.Instance;
                    break;
                case "Library":
                    location.searchStrategy = SearchLibrary.Instance;
                    break;
                default:
                    throw new ArgumentException("Location kind does is not recognized: " + info.Kind);
            }

            return location;
        }

        public Location Clone()
        {
            return Create(this.Info);
        }

        public bool Search(List<Army> armies, out object result)
        {
            if (!this.searchStrategy.CanSearchKind(this.Kind))
            {
                throw new InvalidOperationException("Search strategy cannot be used with this location: " +
                                                    this.searchStrategy);
            }

            var success = this.searchStrategy.Search(armies, this, out result);
            if (success)
            {
                this.Searched = true;

                if (this.HasBoon())
                {
                    this.Boon = null;
                }

                if (this.HasMonster())
                {
                    this.Monster = null;
                }
            }

            return success;
        }

        public bool HasBoon()
        {
            return this.Boon != null;
        }

        public bool HasMonster()
        {
            return this.Monster != null;
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
            return $"{this.ShortName}-{this.Kind}({this.Id})";
        }
    }
}