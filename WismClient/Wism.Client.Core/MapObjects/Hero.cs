using Wism.Client.Core;
using Wism.Client.Modules;

namespace Wism.Client.MapObjects
{
    public class Hero : Army
    {
        internal Hero()
        {
            this.Info = ArmyInfo.GetHeroInfo();
        }

        public int GetCommandBonus()
        {
            // TODO: Implement items
            return 0;
        }

        public int GetCombatBonus()
        {
            // TODO: Implement items
            return 0;
        }
    }
}
