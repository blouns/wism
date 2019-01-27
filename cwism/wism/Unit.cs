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

        private int moves = 1;
        private string id;
        private int strength;

        // Ephemeral fields only used during battle
        private int modifiedStrength;
        private int hitPoints = DefaultHitPoints;

        public int Moves { get => moves; set => moves = value; }

        public override string DisplayName { get => Info.DisplayName; }

        public override string ID { get => Info.ID; set => id = value; }
        internal UnitInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindUnitInfo(this.id);
                return info;
            }
        }

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

        private Unit(UnitInfo info)
        {
            this.info = info;
        }

        public Unit()
        {
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

        public virtual int GetAttackModifier()
        {
            ICombatModifier attackModifier = new AttackingForceCombatModifier();            
            int attackerModifier = attackModifier.Calculate(this);

            return attackerModifier;
        }

        public virtual int GetDefenseModifier()
        {
            ICombatModifier defenseModifier = new DefendingForceCombatModifer();
            int defenderModifier = defenseModifier.Calculate(this);

            return defenderModifier;
        }            
    }
}

