using System;
using System.Collections.Generic;
using Wism.Client.Agent.Factories;
using Wism.Client.Core;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.MapObjects
{
    public class Army : MapObject
    {
        public const int MaxUnits = 8;

        private ArmyInfo info;

        internal ArmyInfo Info { get => info; set => info = value; }

        public int Strength { get; set; }
        public int MovesRemaining { get; set; }
        public Clan Clan { get => Player.Clan; }

        // Static info
        public int Moves { get => Info.Moves; }
        public override string DisplayName => Info.DisplayName;
        public override string ShortName => Info.ShortName;

        // Traversal info
        public virtual bool CanWalk { get => Info.CanWalk; }
        public virtual bool CanFloat { get => Info.CanFloat; }
        public virtual bool CanFly { get => Info.CanFly; }

        // Ephemeral properties used during combat only
        public int HitPoints { get; set; }
        public int ModifiedStrength { get; set; }        

        internal Army()
        {            
        }

        public void Kill()
        {
            Player.KillArmy(this);
        }

        public bool IsSpecial()
        {
            return info.IsSpecial;
        }

        public void Reset()
        {
            this.HitPoints = ArmyFactory.DefaultHitPoints;
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

    public class ByArmyViewingOrder : Comparer<Army>
    {
        public override int Compare(Army x, Army y)
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
                // Differentiate on Strength
                compare = x.Strength.CompareTo(y.Strength);
            }

            if (compare == 0)
            {
                // Differentiate on Moves
                compare = x.MovesRemaining.CompareTo(y.MovesRemaining);
            }

            if (compare == 0)
            {
                // Differentiate on ID for consistency
                compare = x.Id.CompareTo(y.Id);
            }

            return compare;
        }
    }

    public class ByArmyBattleOrder : Comparer<Army>
    {
        private readonly Tile battlefieldTile;

        public ByArmyBattleOrder(Tile target)
        {
            this.battlefieldTile = target;
        }

        public override int Compare(Army x, Army y)
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
                // Differentiate on modified battlefield strength
                int modifiedStrengthX = x.GetAttackModifier(this.battlefieldTile) + x.Strength;
                int modifiedStrengthY = y.GetAttackModifier(this.battlefieldTile) + y.Strength;

                // Stack in reverse order of strength
                compare = modifiedStrengthY.CompareTo(modifiedStrengthX);
            }

            if (compare == 0)
            {
                // Differentiate on Moves
                compare = x.MovesRemaining.CompareTo(y.MovesRemaining);
            }

            if (compare == 0)
            {
                // Differentiate on ID for consistency
                compare = x.Id.CompareTo(y.Id);
            }

            return compare;
        }
    }
}