using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Agent.Factories
{
    public static class ArmyFactory
    {
        public const int DefaultHitPoints = 2;

        // TODO: Pull last ID from database for game loads and remotes
        private static int lastId = 0;

        public static Army CreateArmy(Player player, ArmyInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Army newArmy = (info.ShortName == "Hero") ? new Hero() : new Army();
            newArmy.Id = ++lastId;
            newArmy.Info = info;
            newArmy.Strength = info.Strength;
            newArmy.MovesRemaining = info.Moves;
            newArmy.Player = player;
            newArmy.HitPoints = DefaultHitPoints;

            return newArmy;
        }

        public static Army CreateArmy(ArmyInfo info)
        {
            return CreateArmy(null, info);
        }
    }
}
