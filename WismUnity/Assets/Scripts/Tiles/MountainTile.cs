using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class MountainTile : Tile
    {
        public Sprite[] mountainSprites;

        public Sprite preview;

        private const int DefaultTileIndex = 0;
        public HasTile hasTile = HasTile;

        public static bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return (
                (tilemap.GetTile(position) is MountainTile) ||
                (tilemap.GetTile(position) is SnowPeakTile) ||
                (tilemap.GetTile(position) is VolcanoTile));
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            TileUtility.RefreshTile(position, tilemap, hasTile);
        }
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            int index = TileUtility.FindOverlapping14SpriteIndex(position, tilemap, hasTile, DefaultTileIndex);
            tileData.sprite = mountainSprites[index];
        }

#if UNITY_EDITOR
        // Add tile type into Unity Editor

        [MenuItem("Assets/Create/Tiles/MountainTile")]
        public static void CreateMountainTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Mountain Tile", "New Mountain Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MountainTile>(), path);
        }

#endif
    }
}