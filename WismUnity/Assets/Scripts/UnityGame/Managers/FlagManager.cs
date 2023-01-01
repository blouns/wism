using Assets.Scripts.Editors;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;

namespace Assets.Scripts.Managers
{
    public class FlagManager : MonoBehaviour
    {

        [SerializeField]
        private FlagPrefabArrayLayout flagsByClan;

        private Dictionary<string, GameObject> flagsByClanMap;

        public void Initialize()
        {
            if (this.flagsByClan == null)
            {
                throw new InvalidOperationException("Flag prefabs have not been mapped in armiesByClan");
            }

            this.flagsByClanMap = new Dictionary<string, GameObject>();
            for (int i = 0; i < this.flagsByClan.count; i++)
            {
                for (int j = 0; j < this.flagsByClan.rows[i].count; j++)
                {
                    this.flagsByClanMap.Add(
                        this.flagsByClan.rows[i].name.ToLowerInvariant() + "_Flag" + (j + 1),
                        this.flagsByClan.rows[i].row[j]);
                }
            }
        }

        public GameObject FindGameObjectKind(Clan clan, int flagSize)
        {
            return this.flagsByClanMap[$"{clan.ShortName.ToLowerInvariant()}_Flag{flagSize}"];
        }

        public GameObject Instantiate(Clan clan, int flagSize, Transform parent)
        {
            var flagPrefab = FindGameObjectKind(clan, flagSize);
            if (flagPrefab == null)
            {
                Debug.LogFormat($"GameObject not found: {clan.ShortName}_Flag{flagSize}");
            }

            var flagGO = Instantiate(flagPrefab, parent.position, Quaternion.identity, parent);
            flagGO.name = "Flag";
            flagGO.GetComponent<SpriteRenderer>().sortingOrder = 1;

            return flagGO;
        }

        internal bool IsInitialized()
        {
            return this.flagsByClanMap != null;
        }
    }
}
