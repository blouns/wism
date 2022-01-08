using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class SnowPeakTile : Tile
    {
        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            HasTile hasTile = MountainTile.HasTile;
            TileUtility.RefreshTile(position, tilemap, hasTile);
        }

#if UNITY_EDITOR
        // Add tile type into Unity Editor

        [MenuItem("Assets/Create/Tiles/SnowPeakTile")]
        public static void CreateSnowPeakTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Snow Peak Tile", "New Snow Peak Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SnowPeakTile>(), path);
        }

#endif
    }
}