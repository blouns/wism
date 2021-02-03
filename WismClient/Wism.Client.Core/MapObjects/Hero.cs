using System.Collections.Generic;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Hero : Army
    {
        public List<IItem> Items { get; internal set; }

        internal Hero()
        {
            this.Info = ArmyInfo.GetHeroInfo();
            this.DisplayName = Info.DisplayName;
        }

        public int GetCommandBonus()
        {
            int bonus = 0;

            if (!HasItems())
            {
                return bonus;
            }
            
            foreach (var item in Items)
            {
                Artifact artifact = item as Artifact;
                if (artifact == null)
                {
                    continue;
                }    

                bonus += artifact.CommandBonus;
            }
            
            return bonus;
        }

        public int GetCombatBonus()
        {
            int bonus = 0;

            if (!HasItems())
            {
                return bonus;
            }

            foreach (var item in Items)
            {
                Artifact artifact = item as Artifact;
                if (artifact == null)
                {
                    continue;
                }

                bonus += artifact.CombatBonus;
            }

            return bonus;
        }

        public bool HasItems()
        {
            return this.Items != null && this.Items.Count > 0;
        }
    }
}
