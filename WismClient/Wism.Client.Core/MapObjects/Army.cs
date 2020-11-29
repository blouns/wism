using System;
using System.Text;
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
        public bool IsDead { get; set; }

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
            this.IsDead = true;
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

        public string ToStringVerbose()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Type: {GetType()}");
            sb.AppendLine($"ID: {Id}");
            sb.AppendLine($"Name: {ShortName}");
            sb.AppendLine($"Clan: {Clan}");

            if (this.IsDead)
            {
                sb.AppendLine("Status: Dead");
            }
            else
            {                
                sb.AppendLine($"Moves Remaining: {MovesRemaining}");
                sb.AppendLine($"Location: ({X},{Y})");
                sb.AppendLine("Status: Alive");
            }

            return sb.ToString();
        }
    }
}