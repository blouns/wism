using Assets.Scripts.Editors;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class TempleTile : LocationTile
    {        
        [SerializeField]
        private Sprite templeSprite;

        public static new bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is TempleTile;
        }

        protected override void GetTileDataInternal(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = templeSprite;
        }

#if UNITY_EDITOR
        protected override GameObject GetPrefab(LocationContainer container)
        {
            return container.TemplePrefab;
        }

        // Add tile type into Unity Editor
        [MenuItem("Assets/Create/Tiles/TempleTile")]
        public static void CreateTempleTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Temple Tile", "New Temple Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TempleTile>(), path);
        }
#endif
    }
}
