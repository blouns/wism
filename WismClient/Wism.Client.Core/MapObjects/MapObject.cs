using System.Text;
using Wism.Client.Core;

namespace Wism.Client.MapObjects
{
    public abstract class MapObject
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public abstract string ShortName { get; }
        public Tile Tile { get; set; }
        public Player Player { get; set; }
        public int X => this.Tile == null ? 0 : this.Tile.X;
        public int Y => this.Tile == null ? 0 : this.Tile.Y;

        public override bool Equals(object obj)
        {
            var other = obj as MapObject;
            if (other == null)
            {
                return false;
            }

            return this.Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return $"{this.GetType()}({this.Id})".GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.Player != null)
            {
                sb.Append(this.Player.Clan);
            }

            sb.Append($"{this.ShortName}({this.Id})");

            return sb.ToString();
        }
    }
}