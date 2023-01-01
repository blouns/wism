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
            var modPath = GameManager.DefaultModPath;
            var clanInfos = ModFactory.LoadClanInfos(modPath);

            this.count = clanInfos.Count;
            this.rows = new ClanFlag[this.count];
            for (int i = 0; i < this.count; i++)
            {
                this.rows[i].name = clanInfos[i].ShortName;
                this.rows[i].count = MaxFlagSize;
                this.rows[i].row = new GameObject[MaxFlagSize];
                this.rows[i].rowNames = new string[MaxFlagSize];

                for (int j = 0; j < this.rows[i].rowNames.Length; j++)
                {
                    this.rows[i].rowNames[j] = $"Flag size: {j + 1}";
                }
            }
        }
    }
}
