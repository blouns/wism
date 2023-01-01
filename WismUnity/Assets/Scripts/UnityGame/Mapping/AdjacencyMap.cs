using System;

namespace Assets.Scripts.Tilemaps
{
    public class AdjacencyMap
    {
        public bool TopLeft;
        public bool TopRight;
        public bool BottomLeft;
        public bool BottomRight;

        public AdjacencyMap()
        {

        }

        public AdjacencyMap(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            this.TopLeft = topLeft;
            this.TopRight = topRight;
            this.BottomLeft = bottomLeft;
            this.BottomRight = bottomRight;
        }

        public override bool Equals(object obj)
        {
            AdjacencyMap other = obj as AdjacencyMap;
            if (other == null)
                return false;

            return (this.BottomLeft == other.BottomLeft &&
                    this.BottomRight == other.BottomRight &&
                    this.TopLeft == other.TopLeft &&
                    this.TopRight == other.TopRight);
        }

        public override int GetHashCode()
        {
            return
                Convert.ToInt32(this.TopLeft) +
                Convert.ToInt32(this.TopRight) * 2 +
                Convert.ToInt32(this.BottomLeft) * 4 +
                Convert.ToInt32(this.BottomRight) * 8;
        }
    }
}
