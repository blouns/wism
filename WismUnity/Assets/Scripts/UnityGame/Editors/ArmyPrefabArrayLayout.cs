using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Modules;

namespace Assets.Scripts.Editors
{
    [Serializable]
    public class ArmyPrefabArrayLayout
    {
        [Serializable]
        public struct ClanArmy
        {
            public string name;
            public int count;
            public string[] rowNames;
            public GameObject[] row;
        }

        public ClanArmy[] rows;
        public int count;

        public ArmyPrefabArrayLayout()
        {
            Initialize();
        }

        public void Initialize()
        {         
            var modPath = GameManager.DefaultModPath;
            var clanInfos = ModFactory.LoadClanInfos(modPath);
            var armyInfos = ModFactory.LoadArmyInfos(modPath);

            count = clanInfos.Count;
            rows = new ClanArmy[count];            
            for (int i = 0; i < count; i++)
            {
                rows[i].name = clanInfos[i].ShortName;
                rows[i].count = armyInfos.Count;
                rows[i].row = new GameObject[armyInfos.Count];
                rows[i].rowNames = new string[armyInfos.Count];
                for (int j = 0; j < armyInfos.Count; j++)
                {
                    rows[i].rowNames[j] = armyInfos[j].ShortName;
                }
            }
        }
    }
}
