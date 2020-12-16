using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Units
{
    public class ArmyFactory
    {
        /*
        private static ArmyFactory factory = null;

        private IList<AffiliationInfo> affiliationInfos;
        private IList<UnitInfo> unitInfos;
        private IList<GameObject> unitGameObjectKinds;

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
            this.affiliationInfos = ModFactory.GetAffiliationInfos();
            this.unitInfos = ModFactory.GetUnitInfos();
            this.unitGameObjectKinds = new List<GameObject>(unitKinds);
        }

        internal GameObject FindGameObjectKind(Unit unit)
        {
            foreach (GameObject go in unitGameObjectKinds)
            {
                if (go.name == String.Format("{0}_{1}", unit.ID, unit.Affiliation.ID))
                {
                    return go;
                }
            }

            return null;
        }

        internal GameObject FindGameObjectKind(Army army)
        {
            if ((army == null) || (army.Size == 0))
            {
                throw new ArgumentNullException(nameof(army));
            }

            return FindGameObjectKind(army[0]);
        }
        */
    }
}
