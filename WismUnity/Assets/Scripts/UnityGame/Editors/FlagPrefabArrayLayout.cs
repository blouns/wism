using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Modules;

namespace Assets.Scripts.Editors
{
    [Serializable]
    public class FlagPrefabArrayLayout
    {
        [Serializable]
        public struct ClanFlag
        {
            public string name;
            public int count;
            public string[] rowNames;
            public GameObject[] row;
        }

        private const int MaxFlagSize = 8; 

        public ClanFlag[] rows;
        public int count;

        public FlagPrefabArrayLayout()
        {
            Initialize();
        }

        public void Initialize()
        {
            var clanInfos = ModFactory.LoadClanInfos(GameManager.DefaultModPath);

            count = clanInfos.Count;
            rows = new ClanFlag[count];            
            for (int i = 0; i < count; i++)
            {
                rows[i].name = clanInfos[i].ShortName;
                rows[i].count = MaxFlagSize;
                rows[i].row = new GameObject[MaxFlagSize];
                rows[i].rowNames = new string[MaxFlagSize];

                for (int j = 0; j < rows[i].rowNames.Length; j++)
                {
                    rows[i].rowNames[j] = $"Flag size: {j + 1}";
                }
            }
        }
    }
}
