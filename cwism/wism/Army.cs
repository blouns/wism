using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Army : MapObject
    {
        public const int MaxUnits = 8;

        List<Unit> units;

        public static Army Create(IList<Unit> units)
        {
            if (units == null || units.Count == 0)
            {
                throw new ArgumentNullException(nameof(units));
            }

            Army composite = new Army();
            composite.units = new List<Unit>(units);
            composite.Units.Sort(new ByUnitViewingOrder());

            return composite;
        }

        public static Army Create(Unit unit)
        {
            return Create(new List<Unit>() { unit });
        }

        public static Army Create(UnitInfo info)
        {
            return Create(Unit.Create(info));
        }

        private Army()
        {
        }

        public override Guid Guid { get => this.Units[0].Guid; }
        public bool IsSpecial()
        {
            return Units.Any<Unit>(v => v.IsSpecial());
        }
        
        public override string DisplayName
        {
            get
            {
                return Units[0].DisplayName;
            }
            set
            {
                // N/A
            }
        }

        public override string ID
        {
            get
            {
                return Units[0].ID;
            }
            set
            {
                // N/A
            }
        }

        public int GetCompositeAttackModifier()
        {
            return Units.Sum<Unit>(v => v.GetAttackModifier());
        }

        public int GetCompositeDefenseModifier()
        {
            return Units.Sum<Unit>(v => v.GetDefenseModifier());
        }

        public bool Contains(Unit unit)
        {
            return this.Units.Contains<Unit>(unit);
        }

        public void Kill(Unit unit)
        {
            if (!Contains(unit))
                throw new ArgumentException("Unit not in the army: {0}", unit.ToString());
            
            this.Units.Remove(unit);
            if (this.Units.Count == 0)
            {
                // No more units in the army; kill it!
                foreach (Player player in World.Current.Players)
                {
                    if (player.IsMine(this))
                    {
                        player.KillArmy(this);
                    }
                }
            }

            this.Units.Sort(new ByUnitViewingOrder());
        }

        public void Add(Unit unit)
        {
            if (this.Units.Count == Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());

            this.Units.Add(unit);
            this.Units.Sort(new ByUnitViewingOrder());
        }

        public void Concat(Army army)
        {
            if ((this.Units.Count + army.Units.Count) > Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());

            this.units = new List<Unit>(this.Units.Concat(army.Units));
            this.Units.Sort(new ByUnitViewingOrder());
        }

        public bool CanWalk()
        {
           bool canWalk = true;
            foreach (Unit unit in this.units)
            {
                canWalk &= unit.CanWalk;
            }

            return canWalk;       
        }

        public bool CanFloat()
        {
            bool canFloat = true;
            foreach (Unit unit in this.units)
            {
                canFloat &= unit.CanFloat;
            }

            return canFloat;
        }

        public bool CanFly()
        {
            bool canFly = true;
            foreach (Unit unit in this.units)
            {
                canFly &= unit.CanFly;
            }

            return canFly;
        }

        public bool TryMove(Direction direction)
        {
            Coordinate coord = this.GetCoordinates();

            // Where are we going?
            switch (direction)
            {
                case Direction.North:
                    coord = new Coordinate(coord.X, coord.Y - 1);
                    break;
                case Direction.East:
                    coord = new Coordinate(coord.X + 1, coord.Y);
                    break;
                case Direction.South:
                    coord = new Coordinate(coord.X, coord.Y + 1);
                    break;
                case Direction.West:
                    coord = new Coordinate(coord.X - 1, coord.Y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction", "Unexpected direction given to unit.");
            }

            return TryMove(coord);
        }

        public bool TryMove(Coordinate to)
        {
            Coordinate coord = this.GetCoordinates();
            Tile[,] map = World.Current.Map;
            Tile targetTile = map[to.X, to.Y];
            
            // Can we traverse in that terrain?
            if (!targetTile.CanTraverseHere(this))
                return false;

            if (targetTile.HasArmy())
            {
                // Does the tile has room for the unit of the same team?
                if ((targetTile.Army.Affiliation == this.Affiliation) &&
                    (!targetTile.HasRoom(this)))
                {
                    return false;
                }

                // Is it an enemy tile?
                if ((targetTile.Army != null) &&
                    (targetTile.Army.Affiliation != this.Affiliation))
                {
                    IWarStrategy war = World.Current.WarStrategy;

                    // WAR! ...in a senseless mind.
                    if (!war.Attack(this, targetTile))
                    {
                        // We have lost!
                        return false;
                    }
                }
            }

            // We are clear to advance!
            this.Tile.MoveArmy(this, targetTile);

            return true;
        }
        
        public int Count
        {
            get
            {
                return this.Units.Count();
            }
        }

        public List<Unit> Units { get => units; }
    }

    public enum Direction
    {
        North,
        South,
        East,
        West
    }

    public class ByUnitViewingOrder : Comparer<Unit>
    {
        public override int Compare(Unit x, Unit y)
        {
            int compare = 0;

            // Heros stack to top
            if ((x is Hero) && !(y is Hero))
            {
                compare = 1;
            }
            else if (y is Hero)
            {
                compare = -1;
            }
            else
            {
                int xStrength = x.GetAttackModifier() + x.Strength;
                int yStrength = y.GetAttackModifier() + y.Strength;
                compare = xStrength.CompareTo(yStrength);
            }

            return compare;
        }
    }
}
