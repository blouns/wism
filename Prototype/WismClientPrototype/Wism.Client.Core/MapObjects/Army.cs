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

        public static Army Create(Player player, IList<Unit> units)
        {
            if (units == null || units.Count == 0)
            {
                throw new ArgumentNullException(nameof(units));
            }

            Tile tile = units[0].Tile;
            Army composite = new Army
            {
                Player = player,
                Tile = tile,
                units = new List<Unit>(units)
            };
            composite.UpdateCompositeUnits();

            return composite;
        }

        public static Army Create(Player player, Unit unit)
        {
            return Create(player, new List<Unit>() { unit });
        }

        public static Army Create(Player player, UnitInfo info)
        {
            return Create(player, Unit.Create(info));
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

        public override void SetTile(Tile newTile)
        {
            base.SetTile(newTile);
            UpdateCompositeUnits();
        }

        public new bool IsSpecial()
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

        public override int MovesRemaining
        {
            get
            {
                int min = Int32.MaxValue;
                this.units.ForEach(u => min = (u.MovesRemaining < min) ? u.MovesRemaining : min);
                return min;
            }
            set
            {
                // Do nothing for army container
            }
        }

        public override void ResetMoves()
        {
            this.units.ForEach(u => u.ResetMoves());
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
            
            bool isFound = false;
            if (this.units.Count == 1)
            {
                // No more units in the army; kill it!
                foreach (Player player in World.Current.Players)
                {
                    if (player.IsMine(this))
                    {
                        isFound = true;
                        player.KillArmy(this);
                    }
                }

                if (!isFound)
                {
                    throw new InvalidOperationException("Cannot remove an army that does not belong to a player!");
                }
            }

            this.units.Remove(unit);
            this.units.Sort(new ByUnitViewingOrder());
        }

        internal void RemoveUnit(Unit unitToRemove)
        {
            if (unitToRemove is null)
            {
                throw new ArgumentNullException(nameof(unitToRemove));
            }

            this.units.Remove(unitToRemove);
        }

        public void Add(Unit unit)
        {
            if (this.units.Count == Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());

            Merge(Army.Create(this.Player, unit));
        }

        public void Merge(Army army)
        {
            if ((this.units.Count + army.units.Count) > Army.MaxUnits)
                throw new ArgumentException("Cannot add more than {0} units.", Army.MaxUnits.ToString());
            
            // TODO: Need to remove from the Player context
            this.units.AddRange(army.units);
            UpdateCompositeUnits();            
        }

        public Army Split(IList<Unit> selectedUnits)
        {
            if (this.Size == selectedUnits.Count)
                throw new ArgumentOutOfRangeException("Split must be a subset of the army.");

            for (int i = 0; i < selectedUnits.Count; i++)
            {
                if (!this.units.Contains(selectedUnits[i]))
                {
                    throw new ArgumentException("Split must be a subset of the army.");
                }
            }

            Army selectedArmy = Army.Create(this.Player, selectedUnits);
            selectedArmy.Tile = this.Tile;

            foreach (Unit unitToRemove in selectedUnits)
            {
                this.RemoveUnit(unitToRemove);
            }
            this.units.Sort(new ByUnitViewingOrder());

            return selectedArmy;
        }

        private static void MoveSelectedArmy(Army selectedArmy, Tile targetTile)
        { 
            Tile originatingTile = selectedArmy.Tile;

            targetTile.Army = selectedArmy;
            selectedArmy.Tile = targetTile;
            foreach (Unit unit in selectedArmy.units)
            {
                unit.Tile = targetTile;
                unit.MovesRemaining -= targetTile.Terrain.MovementCost;   // TODO: Account for bonuses
            }

            selectedArmy.units.Sort(new ByUnitViewingOrder());

            RemoveArmiesFromOrginatingTile(selectedArmy, originatingTile);
        }

        private static void RemoveArmiesFromOrginatingTile(Army armyToRemove, Tile originatingTile)
        {
            // If moving entire army (not a subset)
            if (originatingTile.Army.Size == armyToRemove.Size)
            {
                originatingTile.Army = null;
                return;
            }

            // Otherwise remove moved units from original army
            foreach (Unit unitToRemove in armyToRemove.GetUnits())
            {
                originatingTile.Army.RemoveUnit(unitToRemove);
            }

            if (originatingTile.Army.Size == 0)
            {
                originatingTile.Army = null;
            }
            else
            {
                originatingTile.Army.units.Sort(new ByUnitViewingOrder());
            }
        }

        private void UpdateCompositeUnits()
        {
            this.units.ForEach(u => 
            {
                u.Player = this.Player;
                u.Tile = this.Tile;                
            });
            this.units.Sort(new ByUnitViewingOrder());
        }

        public new bool CanWalk()
        {
            bool canWalk = true;
            foreach (Unit unit in this.units)
            {
                canWalk &= unit.CanWalk;
            }

            return canWalk;       
        }

        public new bool CanFloat()
        {
            bool canFloat = true;
            foreach (Unit unit in this.units)
            {
                canFloat &= unit.CanFloat;
            }

            return canFloat;
        }

        public new bool CanFly()
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

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            pathingStrategy.FindShortestRoute(World.Current.Map, this, target, out IList<Tile> myPath, out float myDistance);

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
            Tile target = map[coord.X, coord.Y];

            float myDistance;
            if (myPath == null)
            {
                // No current path; calculate the shortest route
                IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
                pathingStrategy.FindShortestRoute(map, this, target, out myPath, out _);

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
            Tile[,] map = World.Current.Map;
            Tile targetTile = map[to.X, to.Y];

            // If not moving entire army, create a new army subset
            Army selectedArmy = this;
            //if (this.Tile.Army.Size > selectedUnits.Count)
            //{
            //    selectedArmy = Army.Create(selectedArmy.Affiliation, selectedUnits);
            //    //
            //    // TODO: Add army to player? UI? Prob need to return the new object 
            //    //
            //    // Or maybe, first select a subset (Split) which creates the army, then
            //    // if it is the entire army it is the same army object, but
            //    // if it is a subset then a new army object is created.
            //    // If the army is deselected, then it gets merged back
            //    // If it moves to another army then it gets merged.
            //    // UI prob needs to know if it is a new army object though
            //    // this way the UI always knows what it's dealing with.
            //}

            // Can we traverse in that terrain?
            if (!targetTile.CanTraverseHere(this))
                return false;

            // Do we have enough moves?
            // TODO: Account for terrain bonuses
            if (selectedArmy.units.Any<Unit>(u => u.MovesRemaining < selectedArmy.Tile.Terrain.MovementCost))
            {
                return false;
            }

            if (targetTile.HasArmy())
            {
                // Does the tile has room for the unit of the same team?
                if ((targetTile.Army.Affiliation == selectedArmy.Affiliation) &&
                    (!targetTile.HasRoom(this)))
                {
                    return false;
                }

                // Is it an enemy tile?
                if ((targetTile.Army != null) &&
                    (targetTile.Army.Affiliation != selectedArmy.Affiliation))
                {
                    IWarStrategy war = World.Current.WarStrategy;

                    // WAR! ...in a senseless mind.

                    //
                    // TODO: Need to handle this as a subset too (remove from original)
                    // 
                    if (!war.Attack(selectedArmy, targetTile))
                    {
                        // We have lost!
                        return false;
                    }
                }
                else
                {
                    // TODO: Need to implement a selected cohort within the army
                    //       Perhaps implemented using multiple Army objects in an
                    //       army, or using a new Cohort class.
                    //
                    //       Until then, do not auto-merge armies that are moving.
                    return false;
                }
            }

            // We are clear to advance!
            MoveSelectedArmy(selectedArmy, targetTile);

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
