using Assets.Scripts.Tilemaps;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.Managers
{
    public class ItemManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject itemPrefab;

        private WorldTilemap worldTilemap;
        private readonly Dictionary<Artifact, GameObject> itemGameObjects = new Dictionary<Artifact, GameObject>();
        private bool isInitialized;

        public void Start()
        {
            Initialize();
        }        

        public void FixedUpdate()
        {
            if (!IsInitalized())
            {
                return;
            }

            // TODO: Add an items list to the Game to reduce refresh latency
            RefreshItemGameObjects();
        }

        private void RefreshItemGameObjects()
        {
            var originalItems = new Dictionary<Artifact, GameObject>.KeyCollection(itemGameObjects);

            // Find items sitting on the ground (not in Locations or with Heros)
            var items = GetItemsOnTiles();
            foreach (var item in items)
            {
                GameObject itemGO;
                if (!itemGameObjects.ContainsKey(item))
                {
                    itemGO = InstantiateItemGo(item);
                    itemGameObjects.Add(item, itemGO);
                }
            }

            CleanupItems(originalItems, items);
        }

        /// <summary>
        /// Remove items that are obsolete
        /// </summary>
        /// <param name="originalItems">Original list</param>
        /// <param name="updatedItems">Updated list</param>
        private void CleanupItems(
            Dictionary<Artifact, GameObject>.KeyCollection originalItems, 
            List<Artifact> itemsOnTiles)
        {
            var obsoleteItems = originalItems.Except(itemsOnTiles);
            foreach (var itemToRemove in obsoleteItems)
            {
                Destroy(itemGameObjects[itemToRemove]);
                itemGameObjects.Remove(itemToRemove);
            }
        }

        private GameObject InstantiateItemGo(Artifact item)
        {
            Vector3 worldVector = worldTilemap.ConvertGameToUnityVector(item.X, item.Y);
            return Instantiate<GameObject>(itemPrefab, worldVector, Quaternion.identity, worldTilemap.transform);
        }

        private void Initialize()
        {
            this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                .GetComponent<WorldTilemap>();
            this.isInitialized = true;
        }

        private bool IsInitalized()
        {
            return isInitialized;
        }

        private List<Artifact> GetItemsOnTiles()
        {
            List<Artifact> items = new List<Artifact>();
            var map = World.Current.Map;
            for (int i = 0; i <= map.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= map.GetUpperBound(1); j++)
                {
                    if (map[i, j].HasItems())
                    {
                        // TODO: Consider swapping Tile from IItem to Artifact to avoid this
                        items.AddRange(map[i, j].Items
                            .FindAll(item => item is Artifact)
                            .ConvertAll<Artifact>(
                                new Converter<IItem, Artifact>(ItemToArtifact)));
                    }
                }
            }

            return items;
        }

        private Artifact ItemToArtifact(IItem input)
        {
            return (Artifact)input;
        }
    }
}
