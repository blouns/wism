using System.Text;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public abstract class MapObject
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public abstract string ShortName { get; }
        public Tile Tile { get; set; }
        public Player Player { get; set; }
        public int X { get => (Tile == null) ? 0 : Tile.X; }
        public int Y { get => (Tile == null) ? 0 : Tile.Y; }

        public override bool Equals(object obj)
        {
            MapObject other = obj as MapObject;
            if (other == null)
                return false;

            return this.Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return $"{this.GetType()}({this.Id})".GetHashCode();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Player != null)
            {
                sb.Append(Player.Clan);
            }

            sb.Append($"{ShortName}({Id})");

            return sb.ToString();
        }
    }
}
