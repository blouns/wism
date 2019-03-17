using BranallyGames.Wism.Pathing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Army : Unit, IEnumerable<Unit>
    {
        public const int MaxUnits = 8;

        List<Unit> units;

        public static Army Create(Affiliation affiliation, IList<Unit> units)
        {
            if (units == null || units.Count == 0)
            {
                throw new ArgumentNullException(nameof(units));
            }

            Army composite = new Army();
            composite.Affiliation = affiliation;
            composite.units = new List<Unit>(units);            
            composite.UpdateCompositeUnits();

            return composite;
        }

        public static Army Create(Affiliation affiliation, Unit unit)
        {
            return Create(affiliation, new List<Unit>() { unit });
        }

        public static Army Create(Affiliation affiliation, UnitInfo info)
        {
            return Create(affiliation, Unit.Create(info));
        }

        private Army()
        {
        }

        public override Guid Guid
        {
            get
            {
                if (units.Count > 0)
                    return units[0].Guid;

                return Guid.Empty;
            }
        }        

        public bool IsSpecial()
        {
            return units.Any<Unit>(v => v.IsSpecial());
        }
        
        public override string DisplayName
        {
            get
            {
                if (units.Count > 0)
                    return units[0].DisplayName;

                return "Empty army";
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
                return units[0].ID;
            }
        }

        public int GetCompositeAttackModifier(Tile target)
        {
            return units.Sum<Unit>(v => v.GetAttackModifier(target));
        }

        public int GetCompositeDefenseModifier()
        {
            return units.Sum<Unit>(v => v.GetDefenseModifier());
        }

        public bool Contains(Unit unit)
        {
            return this.units.Contains<Unit>(unit);
        }

        public void Kill(Unit unit)
        {
            if (!Contains(unit))
                throw new ArgumentException("Unit not in the army: {0}", unit.ToString());
            
            this.units.Remove(unit);
            if (this.units.Count == 0)
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

            this.units.Sort(new ByUnitViewingOrder());
        }

        public void Add(Unit unit)
        {
            if (this.units.Count == Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());

            MergeArmies(Army.Create(this.Affiliation, unit));
        }

        public void MergeArmies(Army army)
        {
            if ((this.units.Count + army.units.Count) > Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());
            
            this.units.AddRange(army.units);
            UpdateCompositeUnits();            
        }

        private void UpdateCompositeUnits()
        {
            this.units.ForEach(u => 
            {
                u.Affiliation = this.Affiliation;
                u.Tile = this.Tile;
            });
            this.units.Sort(new ByUnitViewingOrder());
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

        public void FindPath(Tile target, out IList<Tile> path, out float distance)
        {
            IList<Tile> myPath = null;
            float myDistance = 0.0f;

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            pathingStrategy.FindShortestRoute(World.Current.Map, this, target, out myPath, out myDistance);

            path = myPath;
            distance = myDistance;
        }

        public bool TryMoveOneStep(Tile target, ref IList<Tile> path, out float distance)
        {
            return TryMoveOneStep(target.Coordinates, ref path, out distance);
        }

        /// <summary>
        /// Advance the army one step along the shortest route.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public bool TryMoveOneStep(Coordinates coord, ref IList<Tile> path, out float distance)
        {
            if (coord == null)
            {
                throw new ArgumentNullException(nameof(coord));
            }

            Tile[,] map = World.Current.Map;
            if ((coord.X > map.GetLength(0)) || (coord.Y > map.GetLength(1)))
            {
                throw new ArgumentOutOfRangeException(nameof(coord));
            }

            if (path != null && path.Count == 1)
            {
                // We have arrived
                path.Clear();
                distance = 0;
                return false;
            }

            IList<Tile> myPath = path;
            float myDistance = 0.0f;
            Tile target = map[coord.X, coord.Y];
           
            if (myPath == null)
            {
                // No current path; calculate the shortest route
                IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
                pathingStrategy.FindShortestRoute(map, this, target, out myPath, out myDistance);

                if (myPath == null || myPath.Count == 0)
                {
                    // Impossible route
                    Log.WriteLine(Log.TraceLevel.Information, String.Format("Path between {0} and {1} is impassable.", this.GetCoordinates(), target.Coordinates));

                    path = null;
                    distance = 0.0f;
                    return false;
                }
            }

            // Now we have a route
            bool moveSuccessful = TryMove(myPath[1].Coordinates);
            if (moveSuccessful)
            { 
                // Pop the starting location and return updated path and distance
                myPath.RemoveAt(0);
                myDistance = CalculateDistance(myPath);
            }
            else
            {
                Log.WriteLine(Log.TraceLevel.Warning, String.Format("Move failed during path traversal to: {0}", myPath[1].Coordinates));
                myPath = null;
                myDistance = 0;
            }

            path = myPath;
            distance = myDistance;
            return moveSuccessful;
        }

        public Unit GetUnitAt(int index)
        {
            return this.units[index];
        }

        private int CalculateDistance(IList<Tile> myPath)
        {
            // TODO: Calculate based on true unit and affiliation cost; for now, static
            return myPath.Sum<Tile>(tile => tile.Terrain.MovementCost);
        }

        public bool TryMove(Direction direction)
        {
            Coordinates coord = this.GetCoordinates();

            // Where are we going?
            switch (direction)
            {
                case Direction.North:
                    coord = new Coordinates(coord.X, coord.Y - 1);
                    break;
                case Direction.East:
                    coord = new Coordinates(coord.X + 1, coord.Y);
                    break;
                case Direction.South:
                    coord = new Coordinates(coord.X, coord.Y + 1);
                    break;
                case Direction.West:
                    coord = new Coordinates(coord.X - 1, coord.Y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction", "Unexpected direction given to unit.");
            }

            return TryMove(coord);
        }

        public bool TryMove(Coordinates to)
        {
            Coordinates coord = this.GetCoordinates();
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
            UpdateCompositeUnits();

            return true;
        }
        
        public int Size
        {
            get
            {
                return this.units.Count();
            }
        }

        public List<Unit> GetUnits()
        {
            return new List<Unit>(units);
        }

        public IEnumerator<Unit> GetEnumerator()
        {
            return units.GetEnumerator();
        } 

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    
        public Unit this[int index] 
        {                                                                               
            get
            {
                return units[index];
            }

            set
            {
                units[index] = value;
            }
        }

        public List<Unit> SortByBattleOrder(Tile target)
        {
            List<Unit> battleUnits = new List<Unit>(this.units);
            battleUnits.Sort(new ByUnitBattleOrder(target));
            return battleUnits;
        }

        public List<Unit> SortByViewingOrder()
        {
            List<Unit> viewUnits = new List<Unit>(this.units);
            viewUnits.Sort(new ByUnitViewingOrder());
            return viewUnits;
        }
    }
    
    public enum Direction
    {
        North,
        South,
        East,
        West
    }    
}
