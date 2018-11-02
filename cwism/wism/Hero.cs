using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public class Hero : Army
    {
        public static Hero Create()
        {
            UnitInfo info = UnitInfo.GetHeroInfo();

            return new Hero(info);
        }

        private Hero(UnitInfo info)
        {
            this.info = info;

            // TODO: This should come from the mod
            this.Symbol = 'H';
            this.Strength = 5;
            this.Moves = 14;    // TODO: random
        }

        public Hero()
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
