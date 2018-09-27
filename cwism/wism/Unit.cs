using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public class Unit : MapObject
    {
        private UnitInfo info;

        private int moves = 1;
        private char symbol;

        public int Moves { get => moves; }

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

        public bool TryMove(Direction direction)
        {
            Coordinate coord = this.GetCoordinates();
            Tile targetTile;
            Tile[,] map = World.Current.Map;
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

            if (!CanMove(targetTile))
                return false;

            // Tile has room for the unit
            if (targetTile.Unit != null)
            {
                return false;
            }

            // TODO: War! ...in a senseless mind.

            Move(targetTile);           

            return true;
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

