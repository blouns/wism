﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Player
    {
        private Affiliation affiliation;

        public Affiliation Affiliation { get => affiliation; set => affiliation = value; }

        private IList<Army> armies = new List<Army>();

        public Player()
        {
        }

        public static Player Create(Affiliation affiliation)
        {
            if (affiliation == null)
            {
                throw new ArgumentNullException(nameof(affiliation));
            }

            Player player = new Player();
            player.Affiliation = affiliation;
            //player.FoundCapitol();
            player.HireHero();

            return player;
        }

        private void FoundCapitol()
        {
            throw new NotImplementedException();
        }

        private Tile FindTileForNewHero()
        {
            // TODO: Need a home tile for the Hero; for now hardcode
            Tile tile = World.Current.Map[2, 2];
            UnitInfo unitInfo = UnitInfo.GetHeroInfo();

            if (!CanDeploy(unitInfo, tile))
                throw new ArgumentException(
                    String.Format("Hero cannot be deployed to '{1}'.", unitInfo.DisplayName, tile.Terrain.DisplayName));

            return tile;
        }

        public IList<Army> GetArmies()
        {
            return armies;
        }

        public void HireHero()
        {
            Tile tile = FindTileForNewHero();

            HireHero(tile);
        }

        public void HireHero(Tile tile)
        {
            Hero hero = Hero.Create();
            this.DeployArmy(tile, Army.Create(hero));
        }

        public void ConscriptUnit(UnitInfo unitInfo, Tile tile)
        {
            if (unitInfo == null)
            {
                throw new ArgumentNullException(nameof(unitInfo));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (!CanDeploy(unitInfo, tile))
                throw new ArgumentException(
                    String.Format("Unit type '{0}' cannot be deployed to '{1}'.", unitInfo.DisplayName, tile.Terrain.DisplayName));

            Army newUnit = Army.Create(unitInfo);
            DeployArmy(tile, newUnit);
        }

        private void DeployArmy(Tile tile, Army newArmy)
        {
            newArmy.Affiliation = this.Affiliation;
            newArmy.Tile = tile;
            tile.AddArmy(newArmy);
            this.armies.Add(newArmy);
        }

        private bool CanDeploy(UnitInfo unitInfo, Tile tile)
        {
            Terrain terrain = tile.Terrain;
            return ((terrain.CanTraverse(unitInfo.CanWalk, unitInfo.CanFloat, unitInfo.CanFly)) &&
                    (!tile.HasArmy() || (tile.Army.Count < Army.MaxUnits)));
        }

        public void KillArmy(Army targetArmy)
        {
            if (targetArmy == null)
            {
                throw new ArgumentNullException(nameof(targetArmy));
            }

            if (!IsMine(targetArmy))
                throw new ArgumentException("Cannot remove army that is not mine!");

            this.armies.Remove(targetArmy);
        }

        public bool IsMine(Army army)
        {
            if (army == null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            // Verify affiliation and in collection
            return (FindArmy(army) != null) && (army.Affiliation == this.Affiliation);
        }

        private Army FindArmy(Army targetArmy)
        {
            foreach (Army army in this.armies)
            {
                if (army == targetArmy)
                    return army;
            }

            return null;
        }
    }
}