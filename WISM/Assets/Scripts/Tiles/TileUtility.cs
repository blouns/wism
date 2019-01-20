using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

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

        /// <summary>
        /// Lookup the kind that matches the pattern of adjacent, overlapping tiles
        /// </summary>
        /// <param name="kinds">List of all overlapping adjacency map kinds</param>
        /// <param name="overlappingTiles">Tile adjacency map to find</param>
        /// <returns></returns>
        public static int FindOverlappingTileIndex(List<AdjacencyMap> kinds, AdjacencyMap overlappingTiles)
        {            
            int index = kinds.FindIndex(x => x.Equals(overlappingTiles));

            //Debug.Log(String.Format("Id: {0}, Overlap: ({1}, {2}, {3}, {4})",
            //    index, overlappingBlendedTiles<T>.TopLeft, overlappingBlendeds.TopRight, overlappingBlendeds.BottomLeft, overlappingBlendeds.BottomRight));

            return index;
        }

        public static AdjacencyMap FindOverlappingTiles<T>(Vector3Int position, ITilemap tilemap)
        {
            Vector3Int[] grid = TileUtility.GetNeighbors(position);

            AdjacencyMap adjacentBlendedTiles = new AdjacencyMap();
            adjacentBlendedTiles.TopLeft =
                HasTile<T>(tilemap, grid[1]) &&
                HasTile<T>(tilemap, grid[2]) &&
                HasTile<T>(tilemap, grid[5]);

            adjacentBlendedTiles.TopRight =
                HasTile<T>(tilemap, grid[5]) &&
                HasTile<T>(tilemap, grid[7]) &&
                HasTile<T>(tilemap, grid[8]);

            adjacentBlendedTiles.BottomLeft =
                HasTile<T>(tilemap, grid[0]) &&
                HasTile<T>(tilemap, grid[1]) &&
                HasTile<T>(tilemap, grid[3]);

            adjacentBlendedTiles.BottomRight =
                HasTile<T>(tilemap, grid[3]) &&
                HasTile<T>(tilemap, grid[6]) &&
                HasTile<T>(tilemap, grid[7]);

            return adjacentBlendedTiles;
        }

        /// <summary>
        /// Build the overlapping kinds to map the corners which have adjacent tiles
        /// to the sprite number.
        /// </summary>
        /// <returns></returns>
        public static List<AdjacencyMap> BuildOverlappingKinds()
        {
            List<AdjacencyMap> kinds = new List<AdjacencyMap>();
            kinds.Add(new AdjacencyMap(false, true, false, false)); // 0 Bottom-left
            kinds.Add(new AdjacencyMap(true, true, false, true));   // 1 Bottom-left inside corner
            kinds.Add(new AdjacencyMap(true, true, false, false));  // 2 Bottom-middle
            kinds.Add(new AdjacencyMap(true, false, false, false)); // 3 Bottom-right
            kinds.Add(new AdjacencyMap(true, true, true, false));   // 4 Bottom-right inside corner
            kinds.Add(new AdjacencyMap(true, true, true, true));    // 5 Middle-middle
            kinds.Add(new AdjacencyMap(true, false, true, false));  // 6 Right-middle
            kinds.Add(new AdjacencyMap(false, false, false, true)); // 7 Top-left
            kinds.Add(new AdjacencyMap(false, true, true, true));   // 8 Top-left inside corner
            kinds.Add(new AdjacencyMap(false, false, true, true));  // 9 Top-middle
            kinds.Add(new AdjacencyMap(false, false, true, false)); // 10 Top-right
            kinds.Add(new AdjacencyMap(true, false, true, true));   // 11 Top-right inside corner
            kinds.Add(new AdjacencyMap(false, true, false, true));  // 12 Left-middle

            return kinds;
        }

        public static bool HasTile<T>(ITilemap tilemap, Vector3Int position)
        {
            return (tilemap.GetTile(position) is T);
        }

        public static void RefreshTile<T>(Vector3Int position, ITilemap tilemap)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                    if (HasTile<T>(tilemap, adjacentPosition))
                    {
                        tilemap.RefreshTile(adjacentPosition);
                    }
                }
            }

            tilemap.RefreshTile(position);
        }
    }

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
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
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
