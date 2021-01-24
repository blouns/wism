using Assets.Scripts.Common;
using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using System.Collections.Generic;
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

        private static Dictionary<Vector3, GameObject> cityObjects = new Dictionary<Vector3, GameObject>();

        public void OnEnable()
        {
            BuildCityGameObjectCache();

        }

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

                    // TODO: Pull this out; cohesion issue 
                    // Create a new city GameObject for each Top-left city
                    var worldVector = tilemap.GetComponent<Tilemap>().CellToWorld(position);
                    CreateCityGameObject(new Vector3(worldVector.x + 1, worldVector.y, 0f));
                    
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

        private static void BuildCityGameObjectCache()
        {
            Debug.Log("Building city game object cache");
            var cityContainer = UnityUtilities.GameObjectHardFind("Cities");
            int count = cityContainer.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var cityGO = cityContainer.transform.GetChild(i).gameObject;
                if (!cityObjects.ContainsKey(cityGO.transform.position))
                {
                    cityObjects.Add(cityGO.transform.position, cityGO);
                }
            }
            Debug.Log($"City game object cache ready: {cityObjects.Count} cities");
        }

        public static void ClearCityCache()
        {
            cityObjects.Clear(); 
        }

        private void CreateCityGameObject(Vector3 worldVector)
        {            
            if (!cityObjects.ContainsKey(worldVector))
            {
                var cityContainer = UnityUtilities.GameObjectHardFind("Cities");
                var cityPrefab = cityContainer.GetComponent<CityContainer>().CityPrefab;
                var cityGO = Instantiate(cityPrefab, cityContainer.transform);
                cityGO.transform.position = worldVector;
                cityObjects.Add(worldVector, cityGO);
                Debug.Log($"New City game object created");
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
