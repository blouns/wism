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

        public IList<Unit> GetUnits()
        {
            return units;
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

            Terrain terrain = tile.Terrain;
            if (!terrain.CanTraverse(unitInfo.CanWalk, unitInfo.CanFloat, unitInfo.CanFly))
            {
                throw new ArgumentException(
                    String.Format("Unit type '{0}' cannot be deployed to '{1}'.",
                    unitInfo.DisplayName,
                    terrain.DisplayName));
            }

            Unit newUnit = Unit.Create(unitInfo);
            newUnit.Affiliation = this.Affiliation;
            newUnit.Tile = tile;
            tile.Unit = newUnit;
            this.units.Add(newUnit);
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
