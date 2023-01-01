using System;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public static class ArmyFactory
    {
        public const int DefaultHitPoints = 2;

        private static int lastId = 0;

        public static int LastId { get => lastId; set => lastId = value; }

        public static Army CreateArmy(Player player, ArmyInfo info)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Army newArmy = (info.ShortName == "Hero") ? new Hero() : new Army();
            newArmy.Id = ++LastId;
            newArmy.Info = info;
            newArmy.Strength = info.Strength;
            newArmy.MovesRemaining = info.Moves;
            newArmy.Player = player;
            newArmy.HitPoints = DefaultHitPoints;
            newArmy.DisplayName = info.DisplayName;
            newArmy.Moves = info.Moves;

            return newArmy;
        }

        public static Army CreateArmy(ArmyInfo info)
        {
            return CreateArmy(null, info);
        }

        public static Army Load(Player player, ArmyEntity armyEntity)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (armyEntity is null)
            {
                throw new ArgumentNullException(nameof(armyEntity));
            }

            var info = ModFactory.FindArmyInfo(armyEntity.ArmyShortName);
            var army = CreateArmy(player, info);
            army.Id = armyEntity.Id;
            army.IsDead = armyEntity.IsDead;
            army.DisplayName = armyEntity.DisplayName;
            army.MovesRemaining = armyEntity.MovesRemaining;
            army.Strength = armyEntity.Strength;
            if (armyEntity.BlessedAtShortNames != null)
            {
                foreach (var templeName in armyEntity.BlessedAtShortNames)
                {
                    army.BlessedAt.Add(World.Current.GetLocations().Find(l => l.ShortName == templeName));
                }
            }

            return army;
        }
    }
}
