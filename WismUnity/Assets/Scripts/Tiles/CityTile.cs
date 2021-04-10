using Assets.Scripts.Tilemaps;
using System;
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

#if UNITY_EDITOR
        private static Dictionary<Vector3, GameObject> cityObjects = new Dictionary<Vector3, GameObject>();
        private bool isInitialized;

        public void OnEnable()
        {
            BuildCityGameObjectCache();
            isInitialized = true;
        }
#endif

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

#if UNITY_EDITOR
                    // TODO: Pull this out; cohesion issue 
                    // Create a new city GameObject for each Top-left city
                    var worldVector = tilemap.GetComponent<Tilemap>().CellToWorld(position);
                    CreateCityGameObject(new Vector3(worldVector.x + 1, worldVector.y, 0f));
#endif

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
        public static void ClearCityCache()
        {
            cityObjects.Clear(); 
        }

        private void CreateCityGameObject(Vector3 worldVector)
        {            
            if (!cityObjects.ContainsKey(worldVector) && 
                this.isInitialized &&
                ShouldImportCitiesFromTilemap())
            {
                var cityContainer = UnityUtilities.GameObjectHardFind("Cities");
                var cityPrefab = cityContainer.GetComponent<CityContainer>().CityPrefab;
                var cityGO = Instantiate(cityPrefab, cityContainer.transform);
                cityGO.transform.position = worldVector;
                cityObjects.Add(worldVector, cityGO);
                Debug.Log($"New City game object created");
            }
        }

        private bool ShouldImportCitiesFromTilemap()
        {
            bool shouldImport = false;

            var editorObjs = GameObject.FindGameObjectsWithTag("EditorOnly");
            for (int i = 0; i < editorObjs.Length; i++)
            {
                if (editorObjs[i].name == "Cities")
                {
                    var citiesContainer = editorObjs[i].GetComponent<CityContainer>();
                    shouldImport = citiesContainer.ImportCitesFromTilemap;
                }
            }

            return shouldImport;
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
