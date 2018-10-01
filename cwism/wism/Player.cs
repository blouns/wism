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

        private IList<Unit> units = new List<Unit>();

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
            player.HireHero();

            return player;
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

        public IList<Unit> GetUnits()
        {
            return units;
        }

        public void HireHero()
        {
            Tile tile = FindTileForNewHero();

            // TODO: Money talks 
            Hero hero = Hero.Create();
            this.Deploy(tile, hero);
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

            Unit newUnit = Unit.Create(unitInfo);
            Deploy(tile, newUnit);
        }

        private void Deploy(Tile tile, Unit newUnit)
        {
            newUnit.Affiliation = this.Affiliation;
            newUnit.Tile = tile;
            tile.Unit = newUnit;
            this.units.Add(newUnit);
        }

        private bool CanDeploy(UnitInfo unitInfo, Tile tile)
        {
            Terrain terrain = tile.Terrain;
            return ((terrain.CanTraverse(unitInfo.CanWalk, unitInfo.CanFloat, unitInfo.CanFly)) &&
                    (tile.Unit == null));            
        }

        public void KillUnit(Unit targetUnit)
        {
            if (targetUnit == null)
            {
                throw new ArgumentNullException(nameof(targetUnit));
            }

            if (!IsMine(targetUnit))
                throw new ArgumentException("Cannot remove unit that is not mine!");

            this.units.Remove(targetUnit);
        }

        public bool IsMine(Unit unit)
        {
            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            // Verify affiliation and in collection
            return (FindUnit(unit) != null) && (unit.Affiliation == this.Affiliation);
        }

        private Unit FindUnit(Unit targetUnit)
        {
            foreach (Unit unit in this.units)
            {
                if (unit == targetUnit)
                    return unit;
            }

            return null;
        }
    }
}
