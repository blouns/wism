using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class CityTile : Tile
    {
        // City quadrants
        private const int CityTileDefault = 3;
        private const int TopLeftQuadrantIndex = 0;
        private const int TopRightQuadrantIndex = 1;
        private const int BottomLeftQuadrantIndex = 2;
        private const int BottomRightQuadrantIndex = 3;

        // Adjacency indices
        private const int TopLeftAdjacencyIndex = 7;
        private const int TopRightAdjacencyIndex = 10;
        private const int BottomLeftAdjacencyIndex = 0;
        private const int BottomRightAdjacencyIndex = 3;

        [SerializeField]
        private Sprite[] citySprites;

        [SerializeField]
        private Sprite preview;

        public HasTile hasTile = HasTile;

        public static bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is CityTile;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            TileUtility.RefreshTile(position, tilemap, hasTile);
        }
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            int index = TileUtility.FindOverlapping14SpriteIndex(position, tilemap, hasTile, CityTileDefault);

            switch (index)
            {
                case TopLeftAdjacencyIndex:
                    tileData.sprite = citySprites[TopLeftQuadrantIndex];
                    break;
                case TopRightAdjacencyIndex:
                    tileData.sprite = citySprites[TopRightQuadrantIndex];
                    break;
                case BottomLeftAdjacencyIndex:
                    tileData.sprite = citySprites[BottomLeftQuadrantIndex];
                    break;
                case BottomRightAdjacencyIndex:
                    tileData.sprite = citySprites[BottomRightQuadrantIndex];
                    break;
                default:
                    tileData.sprite = citySprites[CityTileDefault];
                    break;
            }            
        }

#if UNITY_EDITOR
        // Add tile type into Unity Editor

        [MenuItem("Assets/Create/Tiles/CityTile")]
        public static void CreateCityTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save City Tile", "New City Tile", "asset", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CityTile>(), path);
        }
#endif
    }
}
