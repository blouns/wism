using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class ForestTile : Tile
    {
        [SerializeField]
        private Sprite[] forestSprites;

        [SerializeField]
        private Sprite preview;

        private const int DefaultTileIndex = 13;
        public HasTile hasTile = HasTile;

        public static bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is ForestTile;
        }
        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            TileUtility.RefreshTile(position, tilemap, hasTile);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            int index = TileUtility.FindOverlapping14SpriteIndex(position, tilemap, hasTile, DefaultTileIndex);
            tileData.sprite = forestSprites[index];
        }

#if UNITY_EDITOR
        // Add tile type into Unity Editor

        [MenuItem("Assets/Create/Tiles/ForestTile")]
        public static void CreateForestTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Forest Tile", "New Forest Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ForestTile>(), path);
        }

#endif
    }
}