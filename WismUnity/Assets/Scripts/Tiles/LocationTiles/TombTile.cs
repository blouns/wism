using Assets.Scripts.Editors;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class TombTile : LocationTile
    {        
        [SerializeField]
        private Sprite tombSprite;

        public static new bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is TombTile;
        }

        protected override void GetTileDataInternal(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = tombSprite;
        }

#if UNITY_EDITOR
        protected override GameObject GetPrefab(LocationContainer container)
        {
            return container.TombPrefab;
        }

        // Add tile type into Unity Editor
        [MenuItem("Assets/Create/Tiles/TombTile")]
        public static void CreateTombTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Tomb Tile", "New Tomb Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TombTile>(), path);
        }
#endif
    }
}
