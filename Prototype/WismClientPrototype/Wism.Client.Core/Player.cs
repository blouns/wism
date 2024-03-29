using System;
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
        public Army SelectedArmy { get => selectedArmy; }

        private IList<Army> myArmies = new List<Army>();

        private Army selectedArmy = null;

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

            return player;
        }

        private Tile FindTileForNewHero()
        {
            // TODO: Temp code; need a home tile for the Hero; for now random location
            Tile tile = null;
            int retries = 0;
            int maxRetries = 100;
            UnitInfo unitInfo = UnitInfo.GetHeroInfo();

            bool deployedHero = false;
            while (!deployedHero)
            {
                if (retries++ > maxRetries)
                {
                    throw new ArgumentException(
                        String.Format("Hero cannot be deployed to '{0}'.", tile.Terrain.DisplayName));
                }

                int x = World.Current.Random.Next(0, World.Current.Map.GetLength(0));
                int y = World.Current.Random.Next(0, World.Current.Map.GetLength(1));

                tile = World.Current.Map[x, y];
                
                deployedHero = CanDeploy(unitInfo, tile);
            }

            return tile;
        }

        public IList<Army> GetArmies()
        {
            return myArmies;
        }

        public void HireHero()
        {
            Tile tile = FindTileForNewHero();

            HireHero(tile);
        }

        public void HireHero(Tile tile)
        {
            Hero hero = Hero.Create(this);
            DeployArmy(tile, Army.Create(this, hero));
        }

        public Army ConscriptArmy(UnitInfo unitInfo, Tile tile)
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

            Army newUnit = Army.Create(this, unitInfo);
            DeployArmy(tile, newUnit);

            return newUnit;
        }

        public Army ConscriptArmies(IList<Unit> units)
        {
            if (units is null)
            {
                throw new ArgumentNullException(nameof(units));
            }

            if (units.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(units));
            }

            return null;
        }

        public Army SelectArmy(IList<Unit> units)
        {
            this.selectedArmy = Army.Create(this, units);
            return SelectedArmy;
        }

        private void DeployArmy(Tile tile, Army newArmy)
        {
            if (tile.HasArmy())
            {
                tile.Army.Merge(newArmy);
                // TODO: Automatically deploy in adjacent tile if blocked?
                newArmy.SetTile(tile);
            }
            else
            {
                tile.AddArmy(newArmy);
            }
            this.myArmies.Add(newArmy);
        }

        private bool CanDeploy(UnitInfo unitInfo, Tile tile)
        {
            if (unitInfo == null)
            {
                throw new ArgumentNullException(nameof(unitInfo));
            }

            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            Terrain terrain = tile.Terrain;
            return ((terrain.CanTraverse(unitInfo.CanWalk, unitInfo.CanFloat, unitInfo.CanFly)) &&
                    (!tile.HasArmy() || (tile.Army.Size < Army.MaxUnits)));
        }

        public void KillArmy(Army targetArmy)
        {
            if (targetArmy == null)
            {
                throw new ArgumentNullException(nameof(targetArmy));
            }

            if (!IsMine(targetArmy))
                throw new ArgumentException("Cannot remove army that is not mine!");

            // BUGBUG: This will remove the entire stack that isn't obvious and may cause issues.
            this.myArmies.Remove(targetArmy);
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
            foreach (Army army in this.myArmies)
            {
                if (army.Guid == targetArmy.Guid)
                    return army;
            }

            return null;
        }
    }
}
