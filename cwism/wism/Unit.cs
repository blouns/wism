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
        private const int DefaultHitPoints = 2;        
        internal UnitInfo info;

        private int moves = 1;
        private char symbol;
        private int strength;

        // Ephemeral fields only used during battle
        private int modifiedStrength;
        private int hitPoints = DefaultHitPoints;

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

        public virtual IList<Unit> Expand()
        {
            return new List<Unit>() { this };
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
                IWarStrategy war = World.Current.WarStrategy;
                
                // WAR! ...in a senseless mind.
                if (!war.Attack(this, targetTile))
                {
                    // We have lost!
                    return false;
                }
            }

            // We are clear to advance!
            MoveInternal(targetTile);           

            return true;
        }

        public void Kill()
        {
            foreach (Player player in World.Current.Players)
            {
                if (player.IsMine(this))
                {
                    player.KillUnit(this);
                }
            }
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

        public bool CanMove(Tile targetTile)
        {
            return targetTile.Terrain.CanTraverse(this.Info.CanWalk, this.Info.CanFloat, this.Info.CanFly);            
        }

        public void MoveInternal(Tile targetTile)
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

