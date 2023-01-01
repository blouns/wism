using Assets.Scripts.Editors;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class SageTile : LocationTile
    {
        [SerializeField]
        private Sprite sageSprite;

        public static new bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is SageTile;
        }

        protected override void GetTileDataInternal(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = this.sageSprite;
        }

#if UNITY_EDITOR
        protected override GameObject GetPrefab(LocationContainer container)
        {
            return container.SagePrefab;
        }

        // Add tile type into Unity Editor
        [MenuItem("Assets/Create/Tiles/SageTile")]
        public static void CreateSageTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Sage Tile", "New Sage Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SageTile>(), path);
        }
#endif
    }
}
