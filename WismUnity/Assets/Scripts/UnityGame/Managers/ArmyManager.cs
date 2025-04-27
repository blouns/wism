using Assets.Scripts.Armies;
using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Assets.Scripts.Managers
{
    public class ArmyManager : MonoBehaviour
    {
        [SerializeField]
        private ArmyPrefabArrayLayout armiesByClan;

        private Dictionary<string, GameObject> armiesByClanMap;
        private FlagManager flagManager;
        private readonly Dictionary<int, ArmyGameObject> armyDictionary = new Dictionary<int, ArmyGameObject>();
        private UnityManager unityManager;
        private WorldTilemap worldTilemap;
        private InputManager inputManager;

        public Dictionary<int, ArmyGameObject> ArmyDictionary => this.armyDictionary;

        public WorldTilemap WorldTilemap { get => this.worldTilemap; set => this.worldTilemap = value; }
        public UnityManager UnityManager { get => this.unityManager; set => this.unityManager = value; }

        private bool isInitialized;

        public void Start()
        {
            Initialize();
        }

        public void FixedUpdate()
        {
            if (!IsInitialized())
            {
                return;
            }

            DrawArmyGameObjects();
        }

        public void Initialize()
        {
            if (this.armiesByClan == null)
            {
                throw new InvalidOperationException("Army prefabs have not been mapped in armiesByClan");
            }

            this.armiesByClanMap = new Dictionary<string, GameObject>();
            for (int i = 0; i < this.armiesByClan.count; i++)
            {
                for (int j = 0; j < this.armiesByClan.rows[i].count; j++)
                {
                    this.armiesByClanMap.Add(
                        this.armiesByClan.rows[i].name + "_" + this.armiesByClan.rows[i].rowNames[j],
                        this.armiesByClan.rows[i].row[j]);
                }
            }

            this.flagManager = this.gameObject.GetComponent<FlagManager>();
            this.flagManager.Initialize();
            this.unityManager = GetComponentInParent<UnityManager>();
            this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                .GetComponent<WorldTilemap>();
            this.inputManager = this.unityManager.GetComponent<InputManager>();

            this.isInitialized = true;
        }

        public void Reset()
        {
            foreach (var ago in this.armyDictionary.Values)
            {
                Destroy(ago.GameObject);
            }
            this.armyDictionary.Clear();
        }

        public GameObject FindGameObjectKind(Army army)
        {
            if (!IsInitialized())
            {
                Initialize();
            }

            return this.armiesByClanMap[$"{army.Clan.ShortName}_{army.ShortName}"];
        }

        public GameObject FindGameObjectKind(Clan clan, ArmyInfo armyInfo)
        {
            if (!IsInitialized())
            {
                Initialize();
            }

            return this.armiesByClanMap[$"{clan.ShortName}_{armyInfo.ShortName}"];
        }

        public GameObject Instantiate(Army army, Vector3 worldVector, Transform parent)
        {
            var armyPrefab = FindGameObjectKind(army);
            if (armyPrefab == null)
            {
                Debug.LogFormat($"GameObject not found: {army.Clan.ShortName}_{army.ShortName}");
            }

            var armyGO = Instantiate(armyPrefab, worldVector, Quaternion.identity, parent);
            armyGO.GetComponent<SpriteRenderer>().sortingOrder = 1;

            // Create flag objects for army GameObjects
            this.flagManager.Instantiate(army.Clan, 1, armyGO.transform);

            return armyGO;
        }

        internal bool IsInitialized()
        {
            return this.isInitialized;
        }

        public void CleanupArmies()
        {
            // Find and cleanup stale game objects
            var toDelete = new List<int>(this.ArmyDictionary.Keys);
            foreach (Player player in Game.Current.Players)
            {
                IList<Army> armies = player.GetArmies();
                foreach (Army army in armies)
                {
                    if (this.ArmyDictionary.ContainsKey(army.Id))
                    {
                        // Found the army so don't remove it
                        toDelete.Remove(army.Id);
                    }
                }
            }

            // Remove objects missing from the game
            toDelete.ForEach(id =>
            {
                Destroy(this.ArmyDictionary[id].GameObject);
                this.ArmyDictionary.Remove(id);
            });
        }

        public void DrawArmyGameObjects()
        {
            foreach (Player player in Game.Current.Players)
            {
                // Create all the objects if not already present
                foreach (Army army in player.GetArmies())
                {
                    // Find or create and set up the GameObject connected to WISM MapObject
                    if (!this.ArmyDictionary.ContainsKey(army.Id))
                    {
                        Vector3 worldVector = this.WorldTilemap.ConvertGameToUnityVector(army.X, army.Y);
                        InstantiateArmy(army, worldVector);
                    }
                    else
                    {
                        this.ArmyDictionary[army.Id].GameObject.SetActive(false);
                    }
                }

                if (Game.Current.GameState == GameState.MovingArmy ||
                    Game.Current.GameState == GameState.SelectedArmy)
                {
                    // Reset the current tile for information panel 
                    var inputHandler = this.inputManager.InputHandler;
                    inputHandler.SetCurrentTile(null);
                }
                // Draw only the "top" army for each army stack on the map
                // TODO: Iterate through armies rather than every tile for perf
                foreach (Tile tile in World.Current.Map)
                {
                    int armyId;
                    // Draw visiting armies over stationed armies
                    if (tile.HasVisitingArmies() && this.ArmyDictionary.ContainsKey(tile.VisitingArmies[0].Id))
                    {
                        armyId = tile.VisitingArmies[0].Id;
                        this.unityManager.SetCameraTarget(this.ArmyDictionary[armyId].GameObject.transform);

                        // Update the current tile for info panel
                        //var coords = WorldTilemap.ConvertUnityToGameVector(this.selectedArmyBox.transform.position);
                        //this.currentTile = World.Current.Map[coords.x, coords.y];
                    }
                    else if (tile.HasArmies() && this.ArmyDictionary.ContainsKey(tile.Armies[0].Id))
                    {
                        armyId = tile.Armies[0].Id;
                    }
                    else
                    {
                        // Not a "top" army                        
                        continue;
                    }

                    ArmyGameObject ago = this.ArmyDictionary[armyId];
                    Vector3 vector = this.WorldTilemap.ConvertGameToUnityVector(ago.Army.X, ago.Army.Y);
                    ago.GameObject.transform.position = vector;
                    ago.GameObject.SetActive(true);
                    var flagGO = ago.GameObject.GetComponentInChildren<ArmyFlagSize>();
                    flagGO.UpdateFlagSize();
                }
            }
        }

        internal void InstantiateArmy(Army army, Vector3 worldVector)
        {
            var armyGO = Instantiate(army, worldVector, this.WorldTilemap.transform);
            armyGO.SetActive(false);

            // Add to the instantiated army to dictionary for tracking
            ArmyGameObject ago = new ArmyGameObject(army, armyGO);
            this.ArmyDictionary.Add(army.Id, ago);
        }
    }
}
