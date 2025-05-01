// File: Wism.Client.AI/InfluenceMaps/SimpleInfluenceMap.cs

using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.InfluenceMaps
{
    public class SimpleInfluenceMap : IInfluenceMap
    {
        private List<Army> enemyArmies;
        private Player currentPlayer;

        public void Update()
        {
            this.currentPlayer = Game.Current.GetCurrentPlayer();
            this.enemyArmies = new List<Army>();

            foreach (var player in Game.Current.Players)
            {
                if (player != currentPlayer)
                {
                    this.enemyArmies.AddRange(player.GetArmies());
                }
            }
        }

        public double GetInfluence(Tile tile)
        {
            if (enemyArmies == null || enemyArmies.Count == 0)
            {
                return 0.0;
            }

            double totalDistance = 0;
            foreach (var enemy in enemyArmies)
            {
                totalDistance += GetManhattanDistance(tile, enemy.Tile);
            }

            if (totalDistance == 0)
            {
                return 0.0;
            }

            return 1.0 / totalDistance;
        }

        private int GetManhattanDistance(Tile tile1, Tile tile2)
        {
            return Math.Abs(tile1.X - tile2.X) + Math.Abs(tile1.Y - tile2.Y);
        }
    }
}
