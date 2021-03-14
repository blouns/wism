using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class World
    {
        private List<City> cities = new List<City>();
        private List<Location> locations = new List<Location>();

        public Tile[,] Map { get; protected set; }

        public string Name { get; set; }

        // Navigation associations
        public Game Game { get; }

        private static World current;

        public static World Current
        {
            get
            {
                if (World.current == null)
                {
                    throw new InvalidOperationException("No current world exists.");
                }

                return World.current;
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

            World oldWorld = World.current;
            try
            {
                MapBuilder.Initialize(ModFactory.ModPath, worldName);
                World.current = new World();
                World.current.Name = worldName;
                World.current.Reset();
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the default world.");
                World.current = oldWorld;
                throw;
            }
        }

        public static void CreateWorld(Tile[,] map)
        {
            World oldWorld = World.current;
            try
            {
                World.current = new World();
                World.current.Reset(map);
            }
            catch
            {
                Log.WriteLine(Log.TraceLevel.Critical, "Unable to create the world from the given map.");
                World.current = oldWorld;
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
            
            int x = tile.X;
            int y = tile.Y;

            // Add to map at top-left tile (4x4 grid)
            city.Tile = World.Current.Map[x, y];
            var tiles = new Tile[]
            {
                World.Current.Map[x,y],
                World.Current.Map[x,y-1],
                World.Current.Map[x+1,y],
                World.Current.Map[x+1,y-1]
            };

            for (int i = 0; i < 4; i++)
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
            locations.Add(location);
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

        public void Reset()
        {
            // Factory reset
            ArmyFactory.LastId = 0;

            Tile[,] map = MapBuilder.CreateDefaultMap();
            Reset(map);
        }

        public void AddDefaultCities()
        {
            // Add cities
            MapBuilder.AddCity(this, 1, 3, "Marthos", "Sirians");
            MapBuilder.AddCity(this, 3, 1, "BanesCitadel", "LordBane");
        }

        public void Reset(Tile[,] map)
        {
            Validate(map);
            this.Map = map;
        }

        private void Validate(Tile[,] map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y] == null)
                    {
                        throw new ArgumentException(
                            String.Format("Map tile is null at ({0}, {1})", x, y));
                    }

                    if (!(map[x, y] is Tile))
                    {
                        throw new ArgumentException(
                            String.Format("Map contains element not of type Tile at ({0}, {1})", x, y));
                    }

                    if (map[x, y].Terrain == null)
                    {
                        throw new ArgumentException(
                            String.Format("Map contains null Terrain at ({0}, {1})", x, y));
                    }

                    if (!MapBuilder.TerrainKinds.ContainsKey(map[x, y].Terrain.ShortName))
                    {
                        throw new ArgumentException(
                            String.Format("Map contains unknown Terrain '{2}' at ({0}, {1})", x, y, map[x, y].Terrain.ShortName));
                    }

                    // Valid units; units are optional
                    if ((map[x, y].HasArmies()) &&
                        (!MapBuilder.ArmyKinds.ContainsKey(map[x, y].Armies[0].ShortName)))
                    {
                        throw new ArgumentException(
                            String.Format("Map tile contains unknown Army type '{2}' at ({0}, {1})", x, y, map[x, y].Armies[0].ShortName));
                    }
                }
            }
        }
    }
}
