using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Hero : Unit
    {
        public static Hero Create()
        {
            UnitInfo info = UnitInfo.GetHeroInfo();

            return new Hero(info);
        }

        private Hero(UnitInfo info)
            : base(info)
        {
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
