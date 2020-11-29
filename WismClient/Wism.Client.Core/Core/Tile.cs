using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.MapObjects;

namespace Wism.Client.Core
{
    public class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }

        public List<Army> Armies { get; set; }

        public List<Army> VisitingArmies { get; set; }

        public Terrain Terrain { get; set; }

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

        public bool HasArmies()
        {
            return (Armies != null && Armies.Count > 0);
        }

        public bool HasVisitingArmies()
        {
            return (VisitingArmies != null && VisitingArmies.Count > 0);
        }

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

        public bool HasRoom(int newArmyCount)
        {
            return 
                (!HasArmies() || 
                ((this.Armies.Count + newArmyCount) <= Army.MaxUnits));
        }

        public bool CanTraverseHere(Army army)
        {
            return this.Terrain.CanTraverse(army.CanWalk, army.CanFloat, army.CanFly);
        }

        public bool CanTraverseHere(List<Army> armies)
        {
            return armies.TrueForAll(
                army => this.CanTraverseHere(army));
        }

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

        public List<Army> MusterArmy()
        {
            // TODO: Get surrounding armies in castle
            return this.Armies;
        }

        public void CommitVisitingArmies()
        {
            if (this.VisitingArmies == null)
            {
                throw new InvalidOperationException("There are no visiting armies to commit on this tile.");
            }

            this.Armies = new List<Army>(this.VisitingArmies);
            this.Armies.Sort(new ByArmyViewingOrder());
            this.VisitingArmies = null;
        }
    }
}