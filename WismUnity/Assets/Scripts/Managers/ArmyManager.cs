using Assets.Scripts.Armies;
using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

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

        public Dictionary<int, ArmyGameObject> ArmyDictionary => armyDictionary;

        public WorldTilemap WorldTilemap { get => worldTilemap; set => worldTilemap = value; }
        public UnityManager UnityManager { get => unityManager; set => unityManager = value; }

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
            if (armiesByClan == null)
            {
                throw new InvalidOperationException("Army prefabs have not been mapped in armiesByClan");
            }

            armiesByClanMap = new Dictionary<string, GameObject>();
            for (int i = 0; i < armiesByClan.count; i++)
            {
                for (int j = 0; j < armiesByClan.rows[i].count; j++)
                {
                    armiesByClanMap.Add(
                        armiesByClan.rows[i].name + "_" + armiesByClan.rows[i].rowNames[j],
                        armiesByClan.rows[i].row[j]);
                }
            }

            this.flagManager = gameObject.GetComponent<FlagManager>();
            this.flagManager.Initialize();
            this.unityManager = this.GetComponentInParent<UnityManager>();
            this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                .GetComponent<WorldTilemap>();
            this.inputManager = this.unityManager.GetComponent<InputManager>();

            this.isInitialized = true;
        }

        public GameObject FindGameObjectKind(Army army)
        {
            if (!IsInitialized())
            {
                Initialize();
            }

            return armiesByClanMap[$"{army.Clan.ShortName}_{army.ShortName}"];
        }

        public GameObject FindGameObjectKind(Clan clan, ArmyInfo armyInfo)
        {
            if (!IsInitialized())
            {
                Initialize();
            }

            return armiesByClanMap[$"{clan.ShortName}_{armyInfo.ShortName}"];
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
            return isInitialized;
        }

        public void CleanupArmies()
        {
            // Find and cleanup stale game objects
            var toDelete = new List<int>(ArmyDictionary.Keys);
            foreach (Player player in Game.Current.Players)
            {
                IList<Army> armies = player.GetArmies();
                foreach (Army army in armies)
                {
                    if (ArmyDictionary.ContainsKey(army.Id))
                    {
                        // Found the army so don't remove it
                        toDelete.Remove(army.Id);
                    }
                }
            }

            // Remove objects missing from the game
            toDelete.ForEach(id =>
            {
                Destroy(ArmyDictionary[id].GameObject);
                ArmyDictionary.Remove(id);
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
                    if (!ArmyDictionary.ContainsKey(army.Id))
                    {
                        Vector3 worldVector = WorldTilemap.ConvertGameToUnityVector(army.X, army.Y);
                        InstantiateArmy(army, worldVector);
                    }
                    else
                    {
                        ArmyDictionary[army.Id].GameObject.SetActive(false);
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
                    if (tile.HasVisitingArmies() && ArmyDictionary.ContainsKey(tile.VisitingArmies[0].Id))
                    {
                        armyId = tile.VisitingArmies[0].Id;
                        this.unityManager.SetCameraTarget(ArmyDictionary[armyId].GameObject.transform);

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
                    Vector3 vector = WorldTilemap.ConvertGameToUnityVector(ago.Army.X, ago.Army.Y);
                    ago.GameObject.transform.position = vector;
                    ago.GameObject.SetActive(true);
                    var flagGO = ago.GameObject.GetComponentInChildren<ArmyFlagSize>();
                    flagGO.UpdateFlagSize();
                }
            }
        }

        internal void InstantiateArmy(Army army, Vector3 worldVector)
        {
            var armyGO = Instantiate(army, worldVector, WorldTilemap.transform);
            armyGO.SetActive(false);

            // Add to the instantiated army to dictionary for tracking
            ArmyGameObject ago = new ArmyGameObject(army, armyGO);
            ArmyDictionary.Add(army.Id, ago);
        }
    }
}
