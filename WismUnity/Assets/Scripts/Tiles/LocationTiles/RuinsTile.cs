using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class RuinsTile : LocationTile
    {        
        [SerializeField]
        private Sprite ruinsSprite;

        public static new bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is RuinsTile;
        }

        protected override void GetTileDataInternal(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = ruinsSprite;
        }

        protected override GameObject GetPrefab(LocationContainer container)
        {
            return container.RuinsPrefab;
        }

#if UNITY_EDITOR        
        // Add tile type into Unity Editor
        [MenuItem("Assets/Create/Tiles/RuinsTile")]
        public static void CreateRuinsTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Ruins Tile", "New Ruins Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<RuinsTile>(), path);
        }
#endif
    }
}
