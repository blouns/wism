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
}

