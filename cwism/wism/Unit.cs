using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Unit : MapObject
    {
        private Random random = new Random(1990);       // Warlords publishing date
        internal UnitInfo info;        
        private int moves = 1;
        private char symbol;
        private int strength;

        public int Moves { get => moves; set => moves = value; }

        public override string DisplayName { get => Info.DisplayName; }

        public override char Symbol { get => Info.Symbol; set => symbol = value; }
        internal UnitInfo Info
        {
            get
            {
                if (this.info == null)
                    this.info = MapBuilder.FindUnitInfo(this.symbol);
                return info;
            }
        }

        public bool CanWalk { get => info.CanWalk; }
        public bool CanFloat { get => info.CanFloat; }
        public bool CanFly { get => info.CanFly; }
        public int Strength { get => strength; set => strength = value; }

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

        public bool TryMove(Direction direction)
        {
            Coordinate coord = this.GetCoordinates();
            Tile targetTile;
            Tile[,] map = World.Current.Map;

            // Where are we going?
            switch (direction)
            {
                case Direction.North:
                    targetTile = map[coord.X, coord.Y - 1];
                    break;
                case Direction.East:
                    targetTile = map[coord.X + 1, coord.Y];
                    break;
                case Direction.South:
                    targetTile = map[coord.X, coord.Y + 1];
                    break;
                case Direction.West:
                    targetTile = map[coord.X - 1, coord.Y];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction", "Unexpected direction given to unit.");
            }

            // Can we traverse in that terrain?
            if (!CanMove(targetTile))
                return false;

            // Does the tile has room for the unit of the same team?
            if ((targetTile.Unit != null) &&
                (targetTile.Unit.Affiliation == this.Affiliation))
            {
                return false;
            }

            // Is it an enemy tile?
            if ((targetTile.Unit != null) &&
                (targetTile.Unit.Affiliation != this.Affiliation))
            {
                // WAR! ...in a senseless mind.
                if (!Attack(targetTile.Unit))
                {
                    // We have failed!
                    this.Kill();
                    return false;
                }
                else
                {
                    // We are victorious!
                    targetTile.Unit.Kill();
                }
            }

            // We are clear to advance!
            Move(targetTile);           

            return true;
        }

        private void Kill()
        {
            foreach (Player player in World.Current.Players)
            {
                if (player.IsMine(this))
                {
                    player.KillUnit(this);
                }
            }
        }

        private int GetAttackModifier()
        {
            ICombatModifier attackModifier = new AttackingForceCombatModifier();            
            int attackerModifier = attackModifier.Calculate(this);

            return attackerModifier;
        }

        private int GetDefenceModifier()
        {
            ICombatModifier defenceModifier = new DefendingForceCombatModifer();
            int defenderModifier = defenceModifier.Calculate(this);

            return defenderModifier;
        }

        /// <summary>
        /// Combat is resolved. Attacking and Defending armies are sorted on the display with 
        /// the most valuable armies on the right hand side.Combat is a series of one-on-one
        /// engagements between the left-most army of each side.Each combat is fought to the
        /// death with the survivor going on to fight his opponents's next army. The battle 
        /// ends when one side is eliminated.The battle mecanics work like this. Each army
        /// rolls a ten-sided die (1-10). The result is low if the die roll is less than or
        /// equal to his opponent's AS (or DS as the case may be). The result is high if the
        /// die roll is greater than his opponent's AS (or DS). 
        /// 
        /// 1) If both rolls are high or both rolls are low, then the step is repeated.
        /// 2) If one rolls low and the other rolls high, then the low roller takes 1 hit. 
        /// 3) If the defender rolls high and the attacker rolls low, the defender takes 1 hit.
        ///
        /// As soon as an army receives 2 hits it is destroyed.
        /// </summary>
        /// <param name="defender">Unit that is defending.</param>
        /// <returns>True if attacker wins; false otherwise.</returns>
        private bool Attack(Unit defender)
        {
            Unit attacker = this;

            // TODO: This will need a revamp when moving to composite armies

            // TODO: Encapsulate this combat strategy
            int attackStrength = Math.Min(attacker.GetAttackModifier() + attacker.Strength, 9);
            int defenceStrength = Math.Min(defender.GetDefenceModifier() + defender.Strength, 9);

            int attackerHits = 2;
            int defenderHits = 2;
            return AttackRoll(attackStrength, attackerHits, defenceStrength, defenderHits);
        }

        // TODO: Add logging to check on algorithm
        private bool AttackRoll(int attackStrength, int attackerHits, int defenceStrength, int defenderHits)
        {
            // Have we won?
            if (defenderHits == 0)
                return true;

            // Have we lost?
            if (attackerHits == 0)
                return false;

            // No? Then keep fighting!
            int attackerRoll = random.Next(1, 11);  // Roll 10 sided die
            int defenderRoll = random.Next(1, 11);  // Roll 10 sided die

            bool attackerRollLow = (attackerRoll <= defenceStrength);
            bool defenderRollLow = (defenderRoll <= attackStrength);

            // Attacker took a hit
            if (attackerRollLow && !defenderRollLow)    
            {
                attackerHits--;
            }
            // Defender took a hit
            else if (!attackerRollLow && defenderRollLow)
            {
                defenderHits--;
            }

            return AttackRoll(attackStrength, attackerHits, defenceStrength, defenderHits);
        }       

        private bool CanMove(Tile targetTile)
        {
            return targetTile.Terrain.CanTraverse(this.Info.CanWalk, this.Info.CanFloat, this.Info.CanFly);            
        }

        private void Move(Tile targetTile)
        {
            this.Tile.Unit = null;
            this.Tile = targetTile;
            this.Tile.Unit = this;
        }
    }

    public class UnitConverter : JsonConverter<Unit>
    {
        public override Unit ReadJson(JsonReader reader, Type objectType, Unit existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            do
            {
                string value = reader.Value as string;
                if (value == "Symbol")
                {
                    string symbol = (string)reader.ReadAsString();
                    UnitInfo ui = MapBuilder.FindUnitInfo(symbol[0]);
                    return Unit.Create(ui);
                }
            } while (reader.Read());

            throw new JsonReaderException("Expected a Unit symbol.");
        }

        public override void WriteJson(JsonWriter writer, Unit value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
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

