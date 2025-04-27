using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class World
    {
        private static World current;
        private readonly List<City> cities = new List<City>();
        private readonly List<Location> locations = new List<Location>();
        private readonly List<Artifact> looseItems = new List<Artifact>();

        public Tile[,] Map { get; protected set; }

        public string Name { get; set; }

        public static World Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException("No current world exists.");
                }

                return current;
            }
        }

        public static void CreateDefaultWorld()
        {
            CreateWorld(ModFactory.WorldPath);
        }

        public static void CreateWorld(string worldName)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                throw new ArgumentException($"'{nameof(worldName)}' cannot be null or whitespace", nameof(worldName));
            }

            var oldWorld = current;
            try
            {
                MapBuilder.Initialize(ModFactory.ModPath, worldName);
                current = new World();
                current.Name = worldName;
                current.Reset();
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the default world.");
                current = oldWorld;
                throw;
            }
        }

        public static void CreateWorld(Tile[,] map)
        {
            var oldWorld = current;
            try
            {
                current = new World();
                current.Reset(map);
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the world from the given map.");
                current = oldWorld;
                throw;
            }
        }

        public void AddCity(City city, Tile tile)
        {
            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (city is null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            // Ensure the city does not already exist
            if (this.cities.Contains(city))
            {
                throw new ArgumentException($"{city} already exists in the world.");
            }

            var x = tile.X;
            var y = tile.Y;

            // Add to map at top-left tile (4x4 grid)
            city.Tile = Current.Map[x, y];
            var tiles = new[]
            {
                Current.Map[x, y],
                Current.Map[x, y - 1],
                Current.Map[x + 1, y],
                Current.Map[x + 1, y - 1]
            };

            for (var i = 0; i < 4; i++)
            {
                tiles[i].City = city;
                tiles[i].Terrain = MapBuilder.TerrainKinds["Castle"];
            }

            // Add city for tracking
            this.cities.Add(city);
        }

        public void AddLocation(Location location, Tile tile)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (this.locations.Contains(location))
            {
                throw new ArgumentException($"{location} already exists in the world.");
            }

            location.Tile = tile;
            tile.Location = location;
            tile.Terrain = location.Terrain;

            // Add location for tracking
            this.locations.Add(location);
        }

        public void AddLooseItem(Artifact item, Tile tile)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (this.looseItems.Contains(item))
            {
                throw new ArgumentException($"{item} already exists in world.");
            }

            // Add loose item for tracking
            this.looseItems.Add(item);
        }

        public List<City> GetCities()
        {
            return new List<City>(this.cities);
        }

        public City FindCity(string shortName)
        {
            if (string.IsNullOrWhiteSpace(shortName))
            {
                throw new ArgumentException($"'{nameof(shortName)}' cannot be null or whitespace", nameof(shortName));
            }

            return this.cities.Find(c => c.ShortName == shortName);
        }

        public List<Location> GetLocations()
        {
            return new List<Location>(this.locations);
        }

        public List<Artifact> GetLooseItems()
        {
            return new List<Artifact>(this.looseItems);
        }

        public void RemoveLooseItem(Artifact artifact, Tile tile)
        {
            if (artifact is null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (this.looseItems.Contains(artifact))
            {
                this.looseItems.Remove(artifact);
            }
        }

        public void Reset()
        {
            // Factory reset
            ArmyFactory.LastId = 0;

            var map = MapBuilder.CreateDefaultMap();
            this.Reset(map);
        }

        public void Reset(Tile[,] map)
        {
            this.Validate(map);
            this.Map = map;
        }

        private void Validate(Tile[,] map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y] == null)
                    {
                        throw new ArgumentException(
                            string.Format("Map tile is null at ({0}, {1})", x, y));
                    }

                    if (!(map[x, y] is Tile))
                    {
                        throw new ArgumentException(
                            string.Format("Map contains element not of type Tile at ({0}, {1})", x, y));
                    }

                    if (map[x, y].Terrain == null)
                    {
                        throw new ArgumentException(
                            string.Format("Map contains null Terrain at ({0}, {1})", x, y));
                    }

                    if (!MapBuilder.TerrainKinds.ContainsKey(map[x, y].Terrain.ShortName))
                    {
                        throw new ArgumentException(
                            string.Format("Map contains unknown Terrain '{2}' at ({0}, {1})", x, y,
                                map[x, y].Terrain.ShortName));
                    }

                    // Valid units; units are optional
                    if (map[x, y].HasArmies() &&
                        !MapBuilder.ArmyKinds.ContainsKey(map[x, y].Armies[0].ShortName))
                    {
                        throw new ArgumentException(
                            string.Format("Map tile contains unknown Army type '{2}' at ({0}, {1})", x, y,
                                map[x, y].Armies[0].ShortName));
                    }
                }
            }
        }
    }
}