using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public static class TileUtility
    {
        /// <summary>
        /// Return neighbors grid in positions:
        /// [2,5,8]
        /// [1,4,7]
        /// [0,3,6]
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3Int[] GetNeighbors(Vector3Int position)
        {
            Vector3Int[] grid = new Vector3Int[9];
            grid[0] = new Vector3Int(position.x - 1, position.y - 1, position.z);
            grid[1] = new Vector3Int(position.x - 1, position.y, position.z);
            grid[2] = new Vector3Int(position.x - 1, position.y + 1, position.z);
            grid[3] = new Vector3Int(position.x, position.y - 1, position.z);
            grid[4] = new Vector3Int(position.x, position.y, position.z);
            grid[5] = new Vector3Int(position.x, position.y + 1, position.z);
            grid[6] = new Vector3Int(position.x + 1, position.y - 1, position.z);
            grid[7] = new Vector3Int(position.x + 1, position.y, position.z);
            grid[8] = new Vector3Int(position.x + 1, position.y + 1, position.z);
            return grid;
        }

    }

    public class QuadNode
    {
        public bool TopLeft;
        public bool TopRight;
        public bool BottomLeft;
        public bool BottomRight;

        public QuadNode()
        {

        }

        public QuadNode(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public override bool Equals(object obj)
        {
            QuadNode other = obj as QuadNode;
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
