using System;
using System.Collections.Generic;
using Wism.Client.Agent.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Player
    {
        private List<Army> myArmies = new List<Army>();

        public Clan Clan { get; set; }



        private Player()
        {
        }

        public static Player Create(Clan clan)
        {
            if (clan is null)
            {
                throw new System.ArgumentNullException(nameof(clan));
            }

            Player player = new Player()
            {
                Clan = clan
            };

            return player;
        }

        public List<Army> GetArmies()
        {
            return new List<Army>(this.myArmies);
        }

        public void HireHero()
        {
            Tile tile = FindTileForNewHero();

            HireHero(tile);
        }

        public void HireHero(Tile tile)
        {
            ConscriptArmy(ArmyInfo.GetHeroInfo(), tile);
        }

        public Army ConscriptArmy(ArmyInfo armyInfo, Tile tile)
        {
            if (armyInfo == null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (!CanDeploy(armyInfo, tile))
                throw new ArgumentException(
                    String.Format("Army type '{0}' cannot be deployed to '{1}'.", armyInfo.DisplayName, tile.Terrain.DisplayName));

            Army newArmy = ArmyFactory.CreateArmy(this, armyInfo);
            var newArmies = new List<Army>() { newArmy };

            DeployArmies(tile, newArmies);

            return newArmy;
        }

        /// <summary>
        /// End the players turn.
        /// </summary>
        /// <remarks>
        /// Resets moves, triggers production, and allows for other clans 
        /// to complete their turns.
        /// </remarks>
        internal void EndTurn()
        {
            if (Game.Current.GetCurrentPlayer() != this)
            {
                throw new InvalidOperationException("Cannot end turn; it's not my turn!");
            }

            ResetArmies();
        }

        /// <summary>
        /// Reset armies to their start-of-turn state.
        /// </summary>
        private void ResetArmies()
        {
            foreach (Army army in GetArmies())
            {
                if (army.IsDead)
                {
                    throw new InvalidOperationException("Player cannot reset a dead army.");
                }

                army.Reset();
                army.MovesRemaining = army.Info.Moves;
            }
        }

        internal void KillArmy(Army army)
        {
            // Remove from the world
            var armies = new List<Army>() { army };
            army.Tile.RemoveArmies(armies);

            // Remove from player armies for tracking
            myArmies.Remove(army);
        }

        private void DeployArmies(Tile tile, List<Army> newArmies)
        {
            // Deploy to the world
            tile.AddArmies(newArmies);

            // Add to player armies for tracking            
            this.myArmies.AddRange(newArmies);
        }

        private bool CanDeploy(ArmyInfo armyInfo, Tile tile)
        {
            if (armyInfo == null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            Terrain terrain = tile.Terrain;
            return ((terrain.CanTraverse(armyInfo.CanWalk, armyInfo.CanFloat, armyInfo.CanFly)) &&
                    (!tile.HasArmies() || (tile.Armies.Count < Army.MaxUnits)));
        }

        private Tile FindTileForNewHero()
        {
            // TODO: Temp code; need a home tile for the Hero; for now random location
            Tile tile = null;
            int retries = 0;
            int maxRetries = 100;
            ArmyInfo unitInfo = ArmyInfo.GetHeroInfo();

            bool deployedHero = false;
            while (!deployedHero)
            {
                if (retries++ > maxRetries)
                {
                    throw new ArgumentException(
                        String.Format("Hero cannot be deployed to '{0}'.", tile.Terrain.DisplayName));
                }

                int x = Game.Current.Random.Next(0, World.Current.Map.GetLength(0));
                int y = Game.Current.Random.Next(0, World.Current.Map.GetLength(1));

                tile = World.Current.Map[x, y];

                deployedHero = CanDeploy(unitInfo, tile);
            }

            return tile;
        }

        public override string ToString()
        {
            return this.Clan.ToString();
        }
    }
}