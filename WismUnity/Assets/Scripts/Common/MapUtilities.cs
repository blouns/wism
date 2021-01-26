using Assets.Scripts.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Common
{
    public static class MapUtilities
    {
        internal static Vector3 ConvertGameToUnityVector(int gameX, int gameY, WorldTilemap worldTilemap)
        {
            var tileMap = worldTilemap.GetComponent<Tilemap>();
            Vector3 worldVector = tileMap.CellToWorld(new Vector3Int(gameX + 1, gameY + 1, 0));
            worldVector.x += tileMap.cellBounds.xMin - tileMap.tileAnchor.x;
            worldVector.y += tileMap.cellBounds.yMin - tileMap.tileAnchor.y;
            return worldVector;
        }

        internal static Vector2Int ConvertUnityToGameVector(Vector3 worldVector)
        {
            // TODO: Add support to adjust to tilemap coordinates in case
            //       the tilemap is translated to another location. 
            return new Vector2Int(Mathf.FloorToInt(worldVector.x), Mathf.FloorToInt(worldVector.y));
        }
    }
}
