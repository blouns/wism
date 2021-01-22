using Assets.Scripts.Editors;
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
            return armiesByClanMap != null;
        }
    }
}
