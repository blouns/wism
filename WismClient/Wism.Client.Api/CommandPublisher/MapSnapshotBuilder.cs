using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Api.CommandPublisher
{
    public class MapSnapshotBuilder
    {
        public bool TryBuild(out MapSnapshot? snapshot)
        {
            snapshot = null;

            if (Game.Current == null || World.Current == null)
            {
                return false;
            }

            var map = World.Current.Map;
            var world = World.Current;

            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            var tiles = new List<TileDto>(width * height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = map[x, y];
                    tiles.Add(new TileDto
                    {
                        X = x,
                        Y = y,
                        TerrainType = tile.Terrain?.ToString() ?? "Unknown",
                        HasCity = tile.HasCity()
                    });
                }
            }

            var heroes = new List<HeroDto>();
            foreach (var player in Game.Current.Players)
            {
                foreach (var army in player.GetArmies())
                {
                    var hero = army as Hero;
                    if (hero != null)
                    {
                        heroes.Add(new HeroDto
                        {
                            Name = hero.ShortName,
                            Owner = player.Clan.ToString(),
                            Health = hero.HitPoints,
                            Position = new PositionDto
                            {
                                X = hero.Tile?.X ?? -1,
                                Y = hero.Tile?.Y ?? -1
                            }
                        });
                    }
                }
            }

            snapshot = new MapSnapshot
            {
                Width = width,
                Height = height,
                Tiles = tiles,
                Heroes = heroes,
                Timestamp = DateTime.UtcNow
            };

            return true;
        }
    }
}
