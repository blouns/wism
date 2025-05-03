using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Services
{
    public class GameStateEvaluator
    {
        public List<Army> GetEnemyArmies(Player myPlayer)
        {
            var allArmies = Game.Current.GetAllArmies();
            var enemyArmies = new List<Army>();

            foreach (var army in allArmies)
            {
                if (army.Player != myPlayer)
                {
                    enemyArmies.Add(army);
                }
            }

            return enemyArmies;
        }
    }
}
