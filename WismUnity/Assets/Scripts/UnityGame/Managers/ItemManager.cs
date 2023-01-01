using Assets.Scripts.Tilemaps;
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
        [SerializeField]
        private GameObject companionPrefab;

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
            var originalItems = new Dictionary<Artifact, GameObject>.KeyCollection(this.itemGameObjects);

            // Find items sitting on the ground (not in Locations or with Heros)
            var items = GetItemsOnTiles();
            foreach (var item in items)
            {
                GameObject itemGO;
                if (!this.itemGameObjects.ContainsKey(item))
                {
                    itemGO = InstantiateItemGo(item);
                    this.itemGameObjects.Add(item, itemGO);
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
            var items = new List<Artifact>(obsoleteItems);
            for (int i = 0; i < items.Count; i++)
            {
                Destroy(this.itemGameObjects[items[i]]);
                this.itemGameObjects.Remove(items[i]);
            }
        }

        private GameObject InstantiateItemGo(Artifact item)
        {
            Vector3 worldVector = this.worldTilemap.ConvertGameToUnityVector(item.X, item.Y);
            GameObject go = null;

            if (item.CompanionInteraction != null)
            {
                // The "Item" is a Companion!
                go = Instantiate<GameObject>(this.companionPrefab, worldVector, Quaternion.identity, this.worldTilemap.transform);
            }
            else
            {
                // Normal Item
                go = Instantiate<GameObject>(this.itemPrefab, worldVector, Quaternion.identity, this.worldTilemap.transform);
            }

            return go;
        }

        private void Initialize()
        {
            this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                .GetComponent<WorldTilemap>();
            this.isInitialized = true;
        }

        public void Reset()
        {
            this.itemGameObjects.Clear();
        }

        private bool IsInitalized()
        {
            return this.isInitialized && Game.IsInitialized();
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
                        items.AddRange(map[i, j].Items
                            .FindAll(item => item is Artifact));
                    }
                }
            }

            return items;
        }
    }
}
