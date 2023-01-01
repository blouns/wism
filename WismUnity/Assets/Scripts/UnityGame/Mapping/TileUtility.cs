using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tilemaps
{
    public delegate bool HasTile(ITilemap tilemap, Vector3Int position);

    public static class TileUtility
    {
        private static List<AdjacencyMap> overlappingKinds = TileUtility.BuildOverlapping14Kinds();

        public static void RefreshTile(Vector3Int position, ITilemap tilemap, HasTile hasTile)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                    if (hasTile(tilemap, adjacentPosition))
                    {
                        tilemap.RefreshTile(adjacentPosition);
                    }
                }
            }

            tilemap.RefreshTile(position);
        }

        /// <summary>
        /// Finds the index of a tile matching the overlapping "14" adjacent tile pattern.
        /// 
        /// Note: Tile pattern must contain 14 adjacency overlapping sprites. As defined
        /// by <c>BuildOverlapping14Kinds()</c>.
        /// </summary>
        /// <param name="position">Position for new tile</param>
        /// <param name="tilemap">Tilemap collection</param>
        /// <param name="hasTile">Delegate to check for adjacent tile matches</param>
        /// <param name="defaultIndex">Optional index to use if no match is found</param>
        /// <returns>Matching tile index or default if not found</returns>
        public static int FindOverlapping14SpriteIndex(Vector3Int position, ITilemap tilemap, HasTile hasTile, int defaultIndex = 0)
        {
            int index = defaultIndex;

            try
            {
                AdjacencyMap adjacencyMap = BuildAdjacencyMap(position, tilemap, hasTile);
                index = overlappingKinds.FindIndex(x => x.Equals(adjacencyMap));
                if (index < 0)
                {
                    index = defaultIndex;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            return index;
        }

        /// <summary>
        /// Return neighbors grid in positions:
        /// [2,5,8]
        /// [1,4,7]
        /// [0,3,6]
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private static Vector3Int[] GetNeighbors(Vector3Int position)
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

        private static AdjacencyMap BuildAdjacencyMap(Vector3Int position, ITilemap tilemap, HasTile hasTile)
        {
            Vector3Int[] grid = TileUtility.GetNeighbors(position);

            AdjacencyMap adjacentTiles = new AdjacencyMap();
            adjacentTiles.TopLeft =
                hasTile(tilemap, grid[1]) &&
                hasTile(tilemap, grid[2]) &&
                hasTile(tilemap, grid[5]);

            adjacentTiles.TopRight =
                hasTile(tilemap, grid[5]) &&
                hasTile(tilemap, grid[7]) &&
                hasTile(tilemap, grid[8]);

            adjacentTiles.BottomLeft =
                hasTile(tilemap, grid[0]) &&
                hasTile(tilemap, grid[1]) &&
                hasTile(tilemap, grid[3]);

            adjacentTiles.BottomRight =
                hasTile(tilemap, grid[3]) &&
                hasTile(tilemap, grid[6]) &&
                hasTile(tilemap, grid[7]);

            return adjacentTiles;
        }

        /// <summary>
        /// Build the overlapping kinds to map the corners which have adjacent tiles
        /// to the sprite number.
        /// </summary>
        /// <returns></returns>
        private static List<AdjacencyMap> BuildOverlapping14Kinds()
        {
            // Singleton
            List<AdjacencyMap> kinds = overlappingKinds;
            if (overlappingKinds == null)
            {
                kinds = new List<AdjacencyMap>
                {
                    new AdjacencyMap(false, true, false, false), // 0 Bottom-left
                    new AdjacencyMap(true, true, false, true),   // 1 Bottom-left inside corner
                    new AdjacencyMap(true, true, false, false),  // 2 Bottom-middle
                    new AdjacencyMap(true, false, false, false), // 3 Bottom-right
                    new AdjacencyMap(true, true, true, false),   // 4 Bottom-right inside corner
                    new AdjacencyMap(true, true, true, true),    // 5 Middle-middle
                    new AdjacencyMap(true, false, true, false),  // 6 Right-middle
                    new AdjacencyMap(false, false, false, true), // 7 Top-left
                    new AdjacencyMap(false, true, true, true),   // 8 Top-left inside corner
                    new AdjacencyMap(false, false, true, true),  // 9 Top-middle
                    new AdjacencyMap(false, false, true, false), // 10 Top-right
                    new AdjacencyMap(true, false, true, true),   // 11 Top-right inside corner
                    new AdjacencyMap(false, true, false, true),  // 12 Left-middle
                    new AdjacencyMap(false, false, false, false) // 13 Single
                };
            }

            return kinds;
        }
    }
}
