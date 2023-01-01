using System;
using System.Collections.Generic;
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
        ///     Armies are optional (null) and are present when stationary on tile
        /// </summary>
        public List<Army> Armies { get; set; }

        /// <summary>
        ///     Visiting armies are optional (null) and are present when an army is moving
        /// </summary>
        public List<Army> VisitingArmies { get; set; }

        /// <summary>
        ///     Must have a Terrain (Void is default)
        /// </summary>
        public Terrain Terrain { get; set; }

        /// <summary>
        ///     May have zero or one city
        /// </summary>
        public City City { get; set; }

        /// <summary>
        ///     May have zero or one location (e.g. Sage, Tower, Ruins, Temple)
        /// </summary>
        public Location Location { get; set; }

        public List<Artifact> Items { get; internal set; }

        public void AddItem(Artifact artifact)
        {
            if (this.Items == null)
            {
                this.Items = new List<Artifact>();
            }

            this.Items.Add(artifact);
            artifact.Tile = this;
        }

        public void RemoveItem(Artifact artifact)
        {
            if (this.Items == null)
            {
                this.Items = new List<Artifact>();
            }

            this.Items.Remove(artifact);
            artifact.Tile = null;
        }

        public bool ContainsItem(Artifact item)
        {
            return this.HasItems() && this.Items.Contains(item);
        }

        public bool HasItems()
        {
            return this.Items != null && this.Items.Count > 0;
        }

        public bool IsNeighbor(Tile other)
        {
            return (other.X == this.X - 1 && other.Y == this.Y - 1) ||
                   (other.X == this.X - 1 && other.Y == this.Y) ||
                   (other.X == this.X - 1 && other.Y == this.Y + 1) ||
                   (other.X == this.X && other.Y == this.Y - 1) ||
                   (other.X == this.X && other.Y == this.Y + 1) ||
                   (other.X == this.X + 1 && other.Y == this.Y - 1) ||
                   (other.X == this.X + 1 && other.Y == this.Y) ||
                   (other.X == this.X + 1 && other.Y == this.Y + 1);
        }

        /// <summary>
        ///     Checks if armies are present on the tile
        /// </summary>
        /// <returns>True if armies are present; otherwise, false</returns>
        public bool HasArmies()
        {
            return this.Armies != null && this.Armies.Count > 0;
        }

        /// <summary>
        ///     Check is visiting armies are present on the tile
        /// </summary>
        /// <returns>True if armies are present; otherwise, false</returns>
        public bool HasVisitingArmies()
        {
            return this.VisitingArmies != null && this.VisitingArmies.Count > 0;
        }

        /// <summary>
        ///     Checks if specific armies are present on the tile
        /// </summary>
        /// <param name="armies">Armies to check for</param>
        /// <returns>True if all armies are present; otherwise, false</returns>
        public bool ContainsArmies(List<Army> armies)
        {
            if (!this.HasArmies())
            {
                return false;
            }

            foreach (var army in armies)
            {
                if (!this.Armies.Contains(army))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Raze any buildable structure on the tile
        /// </summary>
        /// <remarks>Internal only; call Raze on IBuildable (tower, city)</remarks>
        internal void RazeInternal()
        {
            // TODO: Raze Tower    
            if (this.City == null)
            {
                throw new InvalidOperationException("Cannot raze a tile that has no structures!");
            }

            if (this.City != null)
            {
                this.City = null;
                this.Terrain = MapBuilder.TerrainKinds["Ruins"];
            }
        }

        /// <summary>
        ///     Checks if specific armies are present as visiting on the tile
        /// </summary>
        /// <param name="armies">Armies to check for</param>
        /// <returns>True if all armies are present; otherwise, false</returns>
        public bool ContainsVisitingArmies(List<Army> armies)
        {
            if (!this.HasVisitingArmies())
            {
                return false;
            }

            foreach (var army in armies)
            {
                if (!this.VisitingArmies.Contains(army))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Adds a set of armies to the tile
        /// </summary>
        /// <param name="newArmies">Armies to add</param>
        public void AddArmies(List<Army> newArmies)
        {
            if (!this.HasRoom(newArmies.Count))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (!this.HasArmies())
            {
                this.Armies = new List<Army>();
            }

            this.Armies.AddRange(newArmies);
            newArmies.ForEach(a => a.Tile = this);
        }

        /// <summary>
        ///     Adds a set of visiting armies to the tile
        /// </summary>
        /// <param name="newVisitingArmies">Armies to add</param>
        internal void AddVisitingArmies(List<Army> newVisitingArmies)
        {
            if (!this.HasRoom(newVisitingArmies.Count))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (!this.HasVisitingArmies())
            {
                this.VisitingArmies = new List<Army>();
            }

            this.VisitingArmies.AddRange(newVisitingArmies);
            newVisitingArmies.ForEach(a => a.Tile = this);
        }

        public bool HasAnyArmies()
        {
            return this.HasVisitingArmies() || this.HasArmies();
        }

        public List<Army> GetAllArmies()
        {
            var armies = new List<Army>();
            if (this.HasVisitingArmies())
            {
                armies.AddRange(this.VisitingArmies);
            }

            if (this.HasArmies())
            {
                armies.AddRange(this.Armies);
            }

            return armies;
        }

        /// <summary>
        ///     Removes armies from a tile
        /// </summary>
        /// <param name="armiesToRemove">Armies to remove</param>
        public void RemoveArmies(List<Army> armiesToRemove)
        {
            if (armiesToRemove is null)
            {
                throw new ArgumentNullException(nameof(armiesToRemove));
            }

            if (this.Armies == null)
            {
                throw new InvalidOperationException("Cannot remove armies from a tile with no armies.");
            }

            foreach (var army in armiesToRemove)
            {
                if (!this.Armies.Contains(army))
                {
                    throw new InvalidOperationException("Cannot remove army from tile as it does not exist.");
                }

                this.Armies.Remove(army);
            }
        }

        /// <summary>
        ///     Removes visiting armies from a tile
        /// </summary>
        /// <param name="armiesToRemove">Armies to remove</param>
        public void RemoveVisitingArmies(List<Army> armiesToRemove)
        {
            if (armiesToRemove is null)
            {
                throw new ArgumentNullException(nameof(armiesToRemove));
            }

            if (this.VisitingArmies == null)
            {
                throw new InvalidOperationException("Cannot remove visiting armies from a tile with no armies.");
            }

            foreach (var army in armiesToRemove)
            {
                if (!this.VisitingArmies.Contains(army))
                {
                    throw new InvalidOperationException("Cannot remove visiting army from tile as it does not exist.");
                }

                this.VisitingArmies.Remove(army);
            }
        }

        /// <summary>
        ///     Checks if there is room in the tile
        /// </summary>
        /// <param name="newArmyCount">Number of new armies to test if there is room for</param>
        /// <returns></returns>
        public bool HasRoom(int newArmyCount)
        {
            return
                !this.HasArmies() ||
                this.Armies.Count + newArmyCount <= Army.MaxArmies;
        }

        public bool CanAttackHere(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            return (this.HasArmies() && this.Armies[0].Clan != armies[0].Clan) ||
                   (this.HasCity() && this.City.Clan != armies[0].Clan &&
                    armies.TrueForAll(a => a.MovesRemaining > this.Terrain.MovementCost));
        }

        /// <summary>
        ///     Check if the given armies can move onto this tile
        /// </summary>
        /// <param name="armies">Armies to test</param>
        /// <param name="ignoreClan">Should include clan check?</param>
        /// <returns>True if the armies can move here; otherwise, false</returns>
        public bool CanTraverseHere(List<Army> armies, bool ignoreClan = false)
        {
            return Game.Current.TraversalStrategy.CanTraverse(armies, this, ignoreClan);
        }

        /// <summary>
        ///     Check if the given army can move onto this tile
        /// </summary>
        /// <param name="army">Army to test</param>
        /// <param name="ignoreClan">Should include clan check?</param>
        /// <returns>True if the army can move here; otherwise, false</returns>
        public bool CanTraverseHere(Army army, bool ignoreClan = false)
        {
            return this.CanTraverseHere(new List<Army> { army }, ignoreClan);
        }

        /// <summary>
        ///     Check if the given ArmyInfo and Clan can move onto this tile
        /// </summary>
        /// <param name="clan"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool CanTraverseHere(Clan clan, ArmyInfo info)
        {
            return Game.Current.TraversalStrategy.CanTraverse(clan, info, this);
        }

        /// <summary>
        ///     Check if the tile has a city
        /// </summary>
        /// <returns></returns>
        public bool HasCity()
        {
            return this.City != null;
        }

        public bool HasLocation()
        {
            return this.Location != null;
        }

        /// <summary>
        ///     Print the tile
        /// </summary>
        /// <returns>String representation of the tile</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"({this.X},{this.Y}):{this.Terrain}");
            if (this.HasArmies())
            {
                sb.Append($"[{this.Armies.Count}:{this.Armies[0]}]");
            }

            if (this.HasLocation())
            {
                sb.Append($"[{this.Location}]");
            }

            return sb.ToString();
        }

        public void AddArmy(Army army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            this.AddArmies(new List<Army> { army });
        }

        /// <summary>
        ///     Gets all armies stationed on the tile. If in a city,
        ///     returns all the armies in all city tiles as a garrison.
        /// </summary>
        /// <returns>Cities to defend current tile</returns>
        public List<Army> MusterArmy()
        {
            var allArmies = new List<Army>();

            if (this.HasCity())
            {
                // Muster troops from across the city!
                var tiles = this.City.GetTiles();
                for (var i = 0; i < tiles.Length; i++)
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

            if (!this.HasArmies())
            {
                this.Armies = new List<Army>();
            }

            this.Armies.AddRange(this.VisitingArmies);
            this.Armies.Sort(new ByArmyViewingOrder());
            this.VisitingArmies = null;
        }

        /// <summary>
        ///     Sets the owner for the tile.
        /// </summary>
        /// <param name="player">Player for which to stake the claim</param>
        public void Claim(Player player)
        {
            if (this.City != null)
            {
                this.City.Claim(player);
            }
        }

        /// <summary>
        ///     Returns the surrounding tiles as a nine-grid with current tile at the center (1, 1).
        /// </summary>
        /// <returns>
        ///     3x3 tile grid in the form:
        ///     { [0] [1] [2] },
        ///     { [3] [4] [5] },
        ///     { [6] [7] [8] }
        ///     where [4] is the current tile.
        /// </returns>
        public Tile[,] GetNineGrid()
        {
            var map = World.Current.Map;
            var nineGrid = new Tile[3, 3];

            for (var yDelta = 1; yDelta >= -1; yDelta--)
            {
                for (var xDelta = -1; xDelta <= 1; xDelta++)
                {
                    if (this.X + xDelta > map.GetUpperBound(0) ||
                        this.X + xDelta < map.GetLowerBound(0) ||
                        this.Y + yDelta > map.GetUpperBound(1) ||
                        this.Y + yDelta < map.GetLowerBound(1))
                    {
                        nineGrid[1 + xDelta, 1 + yDelta] = null;
                    }
                    else
                    {
                        nineGrid[1 + xDelta, 1 + yDelta] = map[this.X + xDelta, this.Y + yDelta];
                    }
                }
            }

            return nineGrid;
        }
    }
}