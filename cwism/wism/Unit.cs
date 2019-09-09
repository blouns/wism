using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Unit : MapObject
    {        
        private const int DefaultHitPoints = 2;        
        internal UnitInfo info;

        private int movesRemaining;
        private int strength;

        // Ephemeral fields only used during battle
        private int modifiedStrength;
        private int hitPoints = DefaultHitPoints;

        internal Unit()
        {

        }

        internal Unit(UnitInfo info)
        {
            this.info = info;
            this.strength = info.Strength;
            this.movesRemaining = info.Moves;
        }

        public override string DisplayName { get => Info.DisplayName; set => Info.DisplayName = value;  }

        public override string ID { get => Info.ID; }

        public UnitInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindUnitInfo(this.ID);
                return info;
            }
        }

        public virtual void ResetMoves()
        {
            this.movesRemaining = info.Moves;
        }

        public virtual int MovesRemaining { get => movesRemaining; set => movesRemaining = value; }
        public virtual bool CanWalk { get => info.CanWalk; }
        public virtual bool CanFloat { get => info.CanFloat; }
        public virtual bool CanFly { get => info.CanFly; }
        public int Strength { get => strength; set => strength = value; }
        public int ModifiedStrength { get => modifiedStrength; set => modifiedStrength = value; }
        public int HitPoints { get => hitPoints; set => hitPoints = value; }

        public static Unit Create(UnitInfo info)
        {
            return new Unit(info);
        }

        public virtual bool IsSpecial()
        {
            return this.info.IsSpecial;
        }        

        public void Reset()
        {
            this.hitPoints = DefaultHitPoints;
            this.ModifiedStrength = this.Strength;
        }

        public virtual int GetAttackModifier(Tile target)
        {
            ICombatModifier attackModifier = new AttackingForceCombatModifier();
            int attackerModifier = attackModifier.Calculate(this, target);

            return attackerModifier;
        }

        public virtual int GetDefenseModifier()
        {
            ICombatModifier defenseModifier = new DefendingForceCombatModifer();
            int defenderModifier = defenseModifier.Calculate(this, this.Tile);

            return defenderModifier;
        }
    }

    public class ByUnitViewingOrder : Comparer<Unit>
    {
        public override int Compare(Unit x, Unit y)
        {
            int compare = 0;

            // Heros stack to top
            if ((x is Hero) && !(y is Hero))
            {
                compare = -1;
            }
            else if (y is Hero)
            {
                compare = 1;
            }
            // Specials are next
            else if (x.IsSpecial() && !y.IsSpecial())
            {
                compare = -1;
            }
            else if (y.IsSpecial())
            {
                compare = 1;
            }
            // Flying is next
            else if (x.CanFly && !y.CanFly)
            {
                compare = -1;
            }
            else if (y.CanFly)
            {
                compare = 1;
            }

            // Tie-breakers
            if (compare == 0)
            {
                // Differentiate on Flying (e.g. Dragons are Special and Flying)
                if (x.CanFly && !y.CanFly)
                {
                    compare = -1;
                }
                else if (y.CanFly)
                {
                    compare = 1;
                }
            }

            if (compare == 0)
            {
                // Differentiate on Strength
                compare = y.Strength.CompareTo(x.Strength);
            }

            if (compare == 0)
            {
                // Differentiate on Moves
                compare = y.MovesRemaining.CompareTo(x.MovesRemaining);
            }

            if (compare == 0)
            {
                // Differentiate on GUID for consistency
                compare = y.Guid.CompareTo(x.Guid);
            }

            return compare;
        }
    }

    public class ByUnitBattleOrder : Comparer<Unit>
    {
        private readonly Tile battlefieldTile;

        public ByUnitBattleOrder(Tile target)
        {
            this.battlefieldTile = target;
        }

        public override int Compare(Unit x, Unit y)
        {
            int compare = 0;

            // Heros stack to bottom
            if ((x is Hero) && !(y is Hero))
            {
                compare = 1;
            }
            else if (y is Hero)
            {
                compare = -1;
            }
            // Specials are next to last
            else if (x.IsSpecial() && !y.IsSpecial())
            {
                compare = 1;
            }
            else if (y.IsSpecial())
            {
                compare = -1;
            }
            // Flying is next
            else if (x.CanFly && !y.CanFly)
            {
                compare = 1;
            }
            else if (y.CanFly)
            {
                compare = -1;
            }

            // Tie-breakers
            if (compare == 0)
            {
                // Differentiate on Flying (e.g. Dragons are Special and Flying)
                if (x.CanFly && !y.CanFly)
                {
                    compare = 1;
                }
                else if (y.CanFly)
                {
                    compare = -1;
                }
            }

            if (compare == 0)
            {
                // Differentiate on modified battlefield strength
                int modifiedStrengthX = x.GetAttackModifier(this.battlefieldTile) + x.Strength;
                int modifiedStrengthY = y.GetAttackModifier(this.battlefieldTile) + y.Strength;

                // Stack in reverse order of strength
                compare = modifiedStrengthX.CompareTo(modifiedStrengthY);
            }

            if (compare == 0)
            {
                // Differentiate on Moves
                compare = x.MovesRemaining.CompareTo(y.MovesRemaining);
            }

            if (compare == 0)
            {
                // Differentiate on GUID for consistency
                compare = x.Guid.CompareTo(y.Guid);
            }

            return compare;
        }
    }
}

