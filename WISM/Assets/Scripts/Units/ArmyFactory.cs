using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Units
{
    public class ArmyFactory
    {
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


        internal GameObject FindGameObjectKind(Army army)
        {
            if ((army == null) || (army.Size == 0))
            {
                throw new ArgumentNullException(nameof(army));
            }

            foreach (GameObject go in unitGameObjectKinds)
            {
                if (go.name == String.Format("{0}_{1}", army.ID, army.Affiliation.ID))
                {
                    return go;
                }
            }

            return null;
        }
    }
}
