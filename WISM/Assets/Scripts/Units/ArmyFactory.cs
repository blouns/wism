using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Assets.Scripts.Units
{
    public class ArmyFactory
    {
        private static ArmyFactory factory = null;

        private IList<ClanInfo> clanInfos;
        private IList<ArmyInfo> armyInfos;
        private IList<GameObject> armyGameObjectKinds;

        public static ArmyFactory Create(GameObject[] unitKinds)
        {
            if (factory == null)
            {
                factory = new ArmyFactory(unitKinds);
            }

            return factory;
        }

        private ArmyFactory(GameObject[] unitKinds)
        {
            this.clanInfos = ModFactory.GetClanInfos();
            this.armyInfos = ModFactory.GetArmyInfos();
            this.armyGameObjectKinds = new List<GameObject>(unitKinds);
        }

        internal GameObject FindGameObjectKind(Army army)
        {
            foreach (GameObject go in armyGameObjectKinds)
            {
                if (go.name == String.Format("{0}_{1}", army.ShortName, army.Clan.ShortName))
                {
                    return go;
                }
            }

            return null;
        }

        internal GameObject FindGameObjectKind(List<Army> armies)
        {
            if ((armies == null) || (armies.Count == 0))
            {
                throw new ArgumentNullException(nameof(armies));
            }

            return FindGameObjectKind(armies[0]);
        }
    }
}
