using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>
        /// Armies are optional (null) and are present when stationary on tile
        /// </summary>
        public List<Army> Armies { get; set; }

        /// <summary>
        /// Visiting armies are optional (null) and are present when an army is moving
        /// </summary>
        public List<Army> VisitingArmies { get; set; }

        /// <summary>
        /// Must have a Terrain (Void is default)
        /// </summary>
        public Terrain Terrain { get; set; }

        /// <summary>
        /// May have zero or one buildings (e.g. City, Tower, Ruins, Temple)
        /// </summary>
        public City City { get; set; }

        public bool IsNeighbor(Tile other)
        {
            return (((other.X == this.X - 1) && (other.Y == this.Y - 1)) ||
                    ((other.X == this.X - 1) && (other.Y == this.Y)) ||
                    ((other.X == this.X - 1) && (other.Y == this.Y + 1)) ||
                    ((other.X == this.X) && (other.Y == this.Y - 1)) ||
                    ((other.X == this.X) && (other.Y == this.Y + 1)) ||
                    ((other.X == this.X + 1) && (other.Y == this.Y - 1)) ||
                    ((other.X == this.X + 1) && (other.Y == this.Y)) ||
                    ((other.X == this.X + 1) && (other.Y == this.Y + 1)));
        }

        /// <summary>
        /// Checks if armies are present on the tile
        /// </summary>
        /// <returns>True if armies are present; otherwise, false</returns>
        public bool HasArmies()
        {
            return (Armies != null && Armies.Count > 0);
        }

        /// <summary>
        /// Check is visiting armies are present on the tile
        /// </summary>
        /// <returns>True if armies are present; otherwise, false</returns>
        public bool HasVisitingArmies()
        {
            return (VisitingArmies != null && VisitingArmies.Count > 0);
        }

        /// <summary>
        /// Checks if specific armies are present on the tile
        /// </summary>
        /// <param name="armies">Armies to check for</param>
        /// <returns>True if all armies are present; otherwise, false</returns>
        public bool ContainsArmies(List<Army> armies)
        {
            if (!HasArmies())
            {
                return false;
            }

            foreach (Army army in armies)
            {
                if (!this.Armies.Contains(army))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Raze the any structure on the tile
        /// </summary>
        /// <remarks>Internal only; call Raze on IBuildable (tower, city)</remarks>
        internal void RazeInternal()
        {
            // TODO: Raze Tower    
            if (City == null)
            {
                throw new InvalidOperationException("Cannot raze a tile that has no structures!");
            }

            if (City != null)
            {
                City = null;
                Terrain = MapBuilder.TerrainKinds["Ruins"];
                return;
            }        
        }

        /// <summary>
        /// Checks if specific armies are present as visiting on the tile
        /// </summary>
        /// <param name="armies">Armies to check for</param>
        /// <returns>True if all armies are present; otherwise, false</returns>
        public bool ContainsVisitingArmies(List<Army> armies)
        {
            if (!HasVisitingArmies())
            {
                return false;
            }

            foreach (Army army in armies)
            {
                if (!this.VisitingArmies.Contains(army))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds a set of armies to the tile
        /// </summary>
        /// <param name="newArmies">Armies to add</param>
        public void AddArmies(List<Army> newArmies)
        {
            if (!HasRoom(newArmies.Count))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (!HasArmies())
            {
                this.Armies = new List<Army>();
            }

            this.Armies.AddRange(newArmies);
            newArmies.ForEach(a => a.Tile = this);
        }

        public bool HasAnyArmies()
        {
            return HasVisitingArmies() || HasArmies();
        }

        public List<Army> GetAllArmies()
        {
            var armies = new List<Army>();
            if (HasVisitingArmies())
            {
                armies.AddRange(VisitingArmies);
            }

            if (HasArmies())
            {
                armies.AddRange(Armies);
            }

            return armies;
        }

        /// <summary>
        /// Removes armies from a tile
        /// </summary>
        /// <param name="armiesToRemove">Armies to remove</param>
        public void RemoveArmies(List<Army> armiesToRemove)
        {
            if (armiesToRemove is null)
            {
                throw new ArgumentNullException(nameof(armiesToRemove));
            }

            if (Armies == null)
            {
                throw new InvalidOperationException("Cannot remove armies from a tile with no armies.");
            }

            foreach (Army army in armiesToRemove)
            {
                if (!this.Armies.Contains(army))
                {
                    throw new InvalidOperationException("Cannot remove army from tile as it does not exist.");
                }

                this.Armies.Remove(army);
            }
        }

        /// <summary>
        /// Removes visiting armies from a tile
        /// </summary>
        /// <param name="armiesToRemove">Armies to remove</param>
        public void RemoveVisitingArmies(List<Army> armiesToRemove)
        {
            if (armiesToRemove is null)
            {
                throw new ArgumentNullException(nameof(armiesToRemove));
            }

            if (VisitingArmies == null)
            {
                throw new InvalidOperationException("Cannot remove visiting armies from a tile with no armies.");
            }

            foreach (Army army in armiesToRemove)
            {
                if (!this.VisitingArmies.Contains(army))
                {
                    throw new InvalidOperationException("Cannot remove visiting army from tile as it does not exist.");
                }

                this.VisitingArmies.Remove(army);
            }
        }

        /// <summary>
        /// Checks if there is room in the tile
        /// </summary>
        /// <param name="newArmyCount">Number of new armies to test if there is room for</param>
        /// <returns></returns>
        public bool HasRoom(int newArmyCount)
        {
            return 
                (!HasArmies() || 
                ((this.Armies.Count + newArmyCount) <= Army.MaxArmies));
        }

        public bool CanAttackHere(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            return (this.HasArmies() && (this.Armies[0].Clan != armies[0].Clan)) ||
                   (this.HasCity() && (this.City.Clan != armies[0].Clan));            
        }

        /// <summary>
        /// Check if the army info can move onto this tile
        /// </summary>
        /// <param name="clan">Clan of the army</param>
        /// <param name="armyInfo">Army kind</param>
        /// <returns>True if the army can move here; otherwise, false</returns>
        public bool CanTraverseHere(Clan clan, ArmyInfo armyInfo)
        {
            if (clan is null)
            {
                throw new ArgumentNullException(nameof(clan));
            }

            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            bool canTraverse = true;
            if (HasCity())
            {
                canTraverse = this.City.CanTraverse(clan);
            }
            
            canTraverse &= this.Terrain.CanTraverse(armyInfo.CanWalk, armyInfo.CanFloat, armyInfo.CanFly);

            return canTraverse;
        }

        /// <summary>
        /// Check if the given army can move onto this tile
        /// </summary>
        /// <param name="army">Army to test</param>
        /// <returns>True if the army can move here; otherwise, false</returns>
        public bool CanTraverseHere(Army army)
        {
            return CanTraverseHere(army.Clan, army.Info);
        }

        public bool HasCity()
        {
            return (this.City != null);
        }

        /// <summary>
        /// Check if the given armies can move onto this tile
        /// </summary>
        /// <param name="army">Armies to test</param>
        /// <returns>True if the armies can move here; otherwise, false</returns>
        public bool CanTraverseHere(List<Army> armies)
        {
            return armies.TrueForAll(
                army => this.CanTraverseHere(army));
        }

        /// <summary>
        /// Print the tile
        /// </summary>
        /// <returns>String representation of the tile</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"({X},{Y}):{Terrain}");
            if (HasArmies())
            {
                sb.Append($"[{Armies.Count}:{Armies[0]}]");
            }

            return sb.ToString();
        }

        public void AddArmy(Army army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            AddArmies(new List<Army>() { army });
        }

        /// <summary>
        /// Gets all armies stationed on the tile. If in a city, 
        /// returns all the armies in all city tiles as a garrison.
        /// </summary>
        /// <returns>Cities to defend current tile</returns>
        public List<Army> MusterArmy()
        {
            var allArmies = new List<Army>();

            if (HasCity())
            {
                // Muster troops from across the city!
                var tiles = City.GetTiles();
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i].HasArmies())
                    {
                        allArmies.AddRange(tiles[i].Armies);
                    }
                }
            }
            else
            {
                if (this.HasArmies())
                {
                    allArmies.AddRange(this.Armies);
                }
            }

            allArmies.Sort(new ByArmyBattleOrder(this));

            return allArmies;
        }

        public void CommitVisitingArmies()
        {
            if (this.VisitingArmies == null)
            {
                throw new InvalidOperationException("There are no visiting armies to commit on this tile.");
            }

            if (!HasArmies())
            {
                this.Armies = new List<Army>();                
            }

            this.Armies.AddRange(this.VisitingArmies);
            this.Armies.Sort(new ByArmyViewingOrder());
            this.VisitingArmies = null;
        }
        
        /// <summary>
        /// Sets the owner for the tile.
        /// </summary>
        /// <param name="player">Player for which to stake the claim</param>
        public void Claim(Player player)
        {
            if (City != null)
            {
                City.Claim(player);
            }
        }

        /// <summary>
        /// Returns the surrounding tiles as a nine-grid with current tile at the center (1, 1).        
        /// </summary>
        /// <returns>
        /// 3x3 tile grid in the form:
        ///  { [0] [1] [2] },
        ///  { [3] [4] [5] },
        ///  { [6] [7] [8] }
        ///  where [4] is the current tile.
        /// </returns>
        public Tile[,] GetNineGrid()
        {
            var map = World.Current.Map;
            var nineGrid = new Tile[3, 3];

            for (int yDelta = 1; yDelta >= -1; yDelta--)
            {
                for (int xDelta = -1; xDelta <= 1; xDelta++)
                {
                    if (X + xDelta > map.GetUpperBound(0) ||
                        X + xDelta < map.GetLowerBound(0) ||
                        Y + yDelta > map.GetUpperBound(1) ||
                        Y + yDelta < map.GetLowerBound(1))
                    {
                        nineGrid[1 + xDelta, 1 + yDelta] = null;
                    }
                    else
                    {
                        nineGrid[1 + xDelta, 1 + yDelta] = map[X + xDelta, Y + yDelta];
                    }
                }
            }

            return nineGrid;
        }
    }
}