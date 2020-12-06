using System.Text;
using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public abstract class MapObject : ICustomizable
    {
        public int Id { get; set; }
        public abstract string DisplayName { get; }
        public abstract string ShortName { get; }
        public Tile Tile { get; set; }
        public Player Player { get; set; }
        public int X { get => Tile.X; }
        public int Y { get => Tile.Y; }

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
