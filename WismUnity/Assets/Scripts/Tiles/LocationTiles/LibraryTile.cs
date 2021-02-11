using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class LibraryTile : LocationTile
    {        
        [SerializeField]
        private Sprite librarySprite;

        public static new bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is LibraryTile;
        }

        protected override void GetTileDataInternal(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = librarySprite;
        }

        protected override GameObject GetPrefab(LocationContainer container)
        {
            return container.LibraryPrefab;
        }

#if UNITY_EDITOR        
        // Add tile type into Unity Editor
        [MenuItem("Assets/Create/Tiles/LibraryTile")]
        public static void CreateLibraryTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Library Tile", "New Library Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LibraryTile>(), path);
        }
#endif
    }
}
