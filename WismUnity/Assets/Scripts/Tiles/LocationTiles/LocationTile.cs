using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public abstract class LocationTile : Tile
    {
        [SerializeField]
        private Sprite preview;

        public HasTile hasTile = HasTile;

        private static Dictionary<Vector3, GameObject> locationObjects = new Dictionary<Vector3, GameObject>();
        private bool isInitialized;

        public void OnEnable()
        {
            BuildLocationGameObjectCache();
            isInitialized = true;
        }

        public static bool HasTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) is LocationTile;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            TileUtility.RefreshTile(position, tilemap, hasTile);
        }
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            GetTileDataInternal(position, tilemap, ref tileData);

            // TODO: Pull this out; cohesion issue 
            // Create a new location GameObject for each location
            var worldVector = tilemap.GetComponent<Tilemap>().CellToWorld(position);
            CreateLocationGameObject(new Vector3(worldVector.x + 0.5f, worldVector.y + 0.5f, 0f));
        }

        protected abstract void GetTileDataInternal(Vector3Int position, ITilemap tilemap, ref TileData tileData);

        public static void ClearLocationCache()
        {
            locationObjects.Clear();
        }

        private void CreateLocationGameObject(Vector3 worldVector)
        {            
            if (!locationObjects.ContainsKey(worldVector) && 
                this.isInitialized &&
                ShouldImportLocationsFromTilemap())
            {
                var locationContainer = UnityUtilities.GameObjectHardFind("Locations");
                var locationPrefab = GetPrefab(locationContainer.GetComponent<LocationContainer>());
                var locationGO = Instantiate(locationPrefab, locationContainer.transform);
                locationGO.transform.position = worldVector;
                locationObjects.Add(worldVector, locationGO);
                Debug.Log($"New Location game object created");
            }
        }

        protected abstract GameObject GetPrefab(LocationContainer container);

        private bool ShouldImportLocationsFromTilemap()
        {
            bool shouldImport = false;

            var editorObjs = GameObject.FindGameObjectsWithTag("EditorOnly");
            for (int i = 0; i < editorObjs.Length; i++)
            {
                if (editorObjs[i].name == "Locations")
                {
                    var locationsContainer = editorObjs[i].GetComponent<LocationContainer>();
                    shouldImport = locationsContainer.ImportLocationsFromTilemap;
                }
            }

            return shouldImport; 
        }

        private static void BuildLocationGameObjectCache()
        {
            Debug.Log("Building location game object cache");
            var locationContainer = UnityUtilities.GameObjectHardFind("Locations");
            int count = locationContainer.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var locationGO = locationContainer.transform.GetChild(i).gameObject;
                if (!locationObjects.ContainsKey(locationGO.transform.position))
                {
                    locationObjects.Add(locationGO.transform.position, locationGO);
                }
            }
            Debug.Log($"Location game object cache ready: {locationObjects.Count} locations");
        }
    }
}
