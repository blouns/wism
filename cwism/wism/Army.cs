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

        IList<Unit> units;

        public static Army Create(IList<Unit> units)
        {
            Army composite = new Army();
            composite.Units = units ?? throw new ArgumentNullException(nameof(units));

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

        public Army()
        {
        }
        
        public bool IsSpecial()
        {
            return Units.Any<Unit>(v => v.IsSpecial());
        }

        public override string DisplayName => "Army";

        // TODO: Show the first unit's info instead
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
        }

        public void Add(Unit unit)
        {
            if (this.Units.Count == Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());

            this.Units.Add(unit);
        }

        public void Concat(Army army)
        {
            if ((this.Units.Count + army.Units.Count) == Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());

            this.Units = new List<Unit>(this.Units.Concat(army.Units));
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
            Tile targetTile;
            Tile[,] map = World.Current.Map;

            // Where are we going?
            switch (direction)
            {
                case Direction.North:
                    targetTile = map[coord.X, coord.Y - 1];
                    break;
                case Direction.East:
                    targetTile = map[coord.X + 1, coord.Y];
                    break;
                case Direction.South:
                    targetTile = map[coord.X, coord.Y + 1];
                    break;
                case Direction.West:
                    targetTile = map[coord.X - 1, coord.Y];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction", "Unexpected direction given to unit.");
            }

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

        public IList<Unit> Units { get => units; set => units = value; }
    }

    public enum Direction
    {
        North,
        South,
        East,
        West
    }
}
