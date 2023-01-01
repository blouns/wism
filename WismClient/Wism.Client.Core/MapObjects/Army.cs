using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.MapObjects
{
    public class Army : MapObject
    {
        public const int MaxArmies = 8;
        public const int MaxStrength = 9;

        private ArmyInfo info;

        internal ArmyInfo Info { get => this.info; set => this.info = value; }

        public int Upkeep { get; set; }
        public int Strength { get; set; }
        public int MovesRemaining { get; set; }
        public Clan Clan { get => this.Player.Clan; }
        public bool IsDead { get; set; }
        public int Moves { get; internal set; }
        public List<Location> BlessedAt { get; set; } = new List<Location>();

        public string KindName { get => this.Info.DisplayName; }
        public override string ShortName => this.Info.ShortName;

        // Traversal info
        public virtual bool CanWalk { get => this.Info.CanWalk; }
        public virtual bool CanFloat { get => this.Info.CanFloat; }
        public virtual bool CanFly { get => this.Info.CanFly; }

        // Ephemeral properties used during combat only
        public int HitPoints { get; set; }
        public int ModifiedStrength { get; set; }
        public bool IsDefending { get; internal set; }

        internal Army()
        {
        }

        public void Defend()
        {
            this.IsDefending = true;
        }

        public virtual void Kill()
        {
            this.IsDead = true;
            this.Player.KillArmy(this);
        }

        public bool IsSpecial()
        {
            return this.info.IsSpecial;
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
            sb.AppendLine($"ID: {this.Id}");
            sb.AppendLine($"Name: {this.ShortName}");
            sb.AppendLine($"Clan: {this.Clan}");

            if (this.IsDead)
            {
                sb.AppendLine("Status: Dead");
            }
            else
            {
                sb.AppendLine($"Moves Remaining: {this.MovesRemaining}");
                sb.AppendLine($"Location: ({this.X},{this.Y})");
                sb.AppendLine("Status: Alive");
            }

            return sb.ToString();
        }
    }
}