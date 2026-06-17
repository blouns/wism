using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.Core.Telemetry;
using Wism.Client.MapObjects;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Api.Telemetry
{
    public class MapSnapshotBuilder
    {
        public bool TryBuild(out MapSnapshot? snapshot)
        {
            snapshot = null;

            if (Game.Current == null || World.Current == null)
                return false;

            try
            {
                var map = World.Current.Map;
                var width = map.GetUpperBound(0) + 1;
                var height = map.GetUpperBound(1) + 1;

                var tiles = GameStateExport.GetAllTiles()
                    .Where(t => t != null)
                    .Select(t => new TileDto
                    {
                        X = t.X,
                        Y = t.Y,
                        TerrainType = t.Terrain?.ToString() ?? "Unknown",
                        HasCity = t.HasCity()
                    }).ToList();

                var armies = GameStateExport.GetAllArmies()
                    .Where(a => a?.Tile != null)
                    .Select(ToArmyDto).ToList();

                var cities = GameStateExport.GetCities()
                    .Where(c => c?.Tile != null)
                    .Select(c => new CityDto
                    {
                        Name = c.ShortName,
                        Owner = c.Player?.Clan?.ShortName ?? "Unknown",
                        Defense = c.Defense,
                        Position = new PositionDto
                        {
                            X = c.Tile.X,
                            Y = c.Tile.Y
                        }
                    }).ToList();

                var items = GameStateExport.GetAllTiles()
                    .Where(t => t?.Items != null)
                    .SelectMany(t => t.Items)
                    .Where(i => i != null && i.Tile != null)
                    .Select(i => new ItemDto
                    {
                        Name = i.ShortName,
                        Position = new PositionDto
                        {
                            X = i.Tile.X,
                            Y = i.Tile.Y
                        }
                    }).ToList();

                var locations = GameStateExport.GetAllTiles()
                    .Where(t => t?.HasLocation() == true && t.Location != null)
                    .Select(t => new LocationDto
                    {
                        Name = t.Location?.ShortName ?? "Unknown",
                        Type = GetLocationType(t),
                        Position = new PositionDto { X = t.X, Y = t.Y }
                    }).ToList();

                snapshot = new MapSnapshot
                {
                    Width = width,
                    Height = height,
                    Tiles = tiles,
                    Armies = armies,
                    Cities = cities,
                    Items = items,
                    Locations = locations,
                    Timestamp = DateTime.UtcNow
                };

                if (Game.IsInitialized())
                {
                    var currentPlayer = Game.Current.GetCurrentPlayer();
                    snapshot.CurrentPlayer = currentPlayer?.Clan?.ShortName;
                    snapshot.CurrentCapital = ToCityDto(currentPlayer?.Capitol);

                    var selected = Game.Current.GetSelectedArmies()?.FirstOrDefault();
                    if (selected != null)
                    {
                        snapshot.SelectedArmy = ToArmyDto(selected);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Snapshot] Error during TryBuild: " + ex.Message);
                throw;
            }
        }

        private static ArmyDto ToArmyDto(Army army)
        {
            return new ArmyDto
            {
                Name = army.ShortName,
                UnitType = army.ShortName,
                Owner = army.Player?.Clan?.ShortName ?? "Unknown",
                Health = army.HitPoints,
                Strength = army.Strength,
                Moves = army.Moves,
                IsHero = army is Hero,
                IsSpecial = army.IsSpecial(),
                CanFly = army.CanFly,
                Position = army.Tile == null ? null : new PositionDto
                {
                    X = army.Tile.X,
                    Y = army.Tile.Y
                }
            };
        }

        private static CityDto? ToCityDto(City? city)
        {
            if (city?.Tile == null)
            {
                return null;
            }

            return new CityDto
            {
                Name = city.ShortName,
                Owner = city.Player?.Clan?.ShortName ?? "Unknown",
                Defense = city.Defense,
                Position = new PositionDto
                {
                    X = city.Tile.X,
                    Y = city.Tile.Y
                }
            };
        }

        private static string GetLocationType(Tile tile)
        {
            if (string.Equals(tile?.Terrain?.ShortName, "Ruins", StringComparison.OrdinalIgnoreCase))
            {
                return "Ruins";
            }

            return tile?.Location?.VisualTerrain ??
                tile?.Location?.Kind ??
                "UnknownType";
        }
    }
}
