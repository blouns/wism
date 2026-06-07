using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Factories;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Armies.WarStrategies
{
    /// <summary>
    ///     Default Warlords combat rules.
    /// </summary>
    public class DefaultWarStrategy : IWarStrategy
    {
        private const int TowerDefenseBonus = 2;

        /// <summary>
        ///     Combat is resolved. Attacking and Defending armies are sorted on the display with
        ///     the most valuable armies on the right hand side.Combat is a series of one-on-one
        ///     engagements between the left-most army of each side.Each combat is fought to the
        ///     death with the survivor going on to fight his opponents' next army. The battle
        ///     ends when one side is eliminated.The battle mechanics work like this. Each army
        ///     rolls a ten-sided die (1-10). The result is low if the die roll is less than or
        ///     equal to his opponent's AS (or DS as the case may be). The result is high if the
        ///     die roll is greater than his opponent's AS (or DS).
        ///     1) If both rolls are high or both rolls are low, then the step is repeated.
        ///     2) If one rolls low and the other rolls high, then the low roller takes 1 hit.
        ///     3) If the defender rolls high and the attacker rolls low, the defender takes 1 hit.
        ///     As soon as an army receives 2 hits it is destroyed.
        /// </summary>
        /// <param name="tile">Tile that is being attacked.</param>
        /// <returns>True if attacker wins; false otherwise.</returns>
        public bool Attack(List<Army> attackers, Tile tile)
        {
            // Neutral city: fight city defense as phantom guard force
            if (tile.HasCity() && tile.City.Clan.ShortName == "Neutral")
            {
                return this.AttackNeutralCity(attackers, tile);
            }

            var defenders = tile.MusterArmy();

            // Attack armys one-at-a-time to the death!
            while (attackers.Count > 0 && defenders.Count > 0)
            {
                if (this.AttackOnce(attackers, tile))
                {
                    Log.WriteLine(Log.TraceLevel.Information, "Attacker killed one army.");
                }
                else
                {
                    Log.WriteLine(Log.TraceLevel.Information, "Defender killed one army.");
                }

                // Refresh the list
                defenders = tile.MusterArmy();
            }

            return attackers.Count > 0;
        }

        /// <summary>
        ///     Combat is begun. Attacking and Defending armies are sorted on the display with
        ///     the most valuable armies on the right hand side. Combat is a single one-on-one
        ///     engagement between the left-most army of each side. Combat is fought to the
        ///     death with the survivor going on to fight his opponents's next army. The battle
        ///     mecanics work like this. Each army rolls a ten-sided die (1-10). The result is
        ///     low if the die roll is less than or equal to his opponent's AS (or DS as the
        ///     case may be). The result is high if thedie roll is greater than his opponent's
        ///     AS (or DS).
        ///     1) If both rolls are high or both rolls are low, then the step is repeated.
        ///     2) If one rolls low and the other rolls high, then the low roller takes 1 hit.
        ///     3) If the defender rolls high and the attacker rolls low, the defender takes 1 hit.
        ///     As soon as an army receives 2 hits it is destroyed.
        /// </summary>
        /// <param name="target">Tile that is being attacked.</param>
        /// <returns>True if attacker wins; false otherwise.</returns>
        public bool AttackOnce(List<Army> attackers, Tile target)
        {
            this.AttackOnce(attackers, target, out var wasSuccessful);

            return wasSuccessful;
        }

        public bool AttackOnce(List<Army> attackers, Tile target, out bool wasSuccessful)
        {
            if (attackers is null)
            {
                throw new ArgumentNullException(nameof(attackers));
            }

            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            PrepareArmiesForAttack(attackers, target, out var defenders, out var compositeAFCM, out var compositeDFCM);

            // Attacking an empty city owned by a player always succeeds
            if (defenders.Count == 0 && target.HasCity())
            {
                wasSuccessful = true;
            }
            // Attacking another army
            else
            {
                wasSuccessful = AttackOnceInternal(defenders, attackers, compositeAFCM, compositeDFCM);
            }

            if (this.BattleContinues(defenders, attackers))
            {
                // Keep fighting
                return true;
            }

            // Battle is over
            ResetHitPoints(defenders, attackers);
            return false;
        }

        public bool BattleContinues(List<Army> defenders, List<Army> attacker)
        {
            return attacker.Count > 0 && defenders.Count > 0;
        }

        private static void PrepareArmiesForAttack(List<Army> attackers, Tile target,
            out List<Army> defenders, out int compositeAFCM, out int compositeDFCM)
        {
            // Muster all armys from composite tile (i.e. city) to defend
            defenders = target.MusterArmy();

            // Calculate composite modifiers
            compositeAFCM = attackers.Sum(a => a.GetAttackModifier(target));
            compositeDFCM = defenders.Sum(a => a.GetDefenseModifier());

            // City defense bonus added once for the whole defending force (Warlords DFCM rule)
            if (target.HasCity())
            {
                compositeDFCM += target.City.Defense;
            }

            // Tower defense bonus: fixed +2 when defenders occupy a tower tile
            if (target.Terrain.ShortName == "Tower")
            {
                compositeDFCM += TowerDefenseBonus;
            }

            // Apply army-specific terrain modifiers (e.g. elves like forests)
            ApplyArmyTerrainModifiers(attackers, target);
            ApplyArmyTerrainModifiers(defenders, target);
        }

        private static void ResetHitPoints(List<Army> defenders, List<Army> attackers)
        {
            attackers.ForEach(u => u.Reset());
            defenders.ForEach(u => u.Reset());
        }

        private static bool AttackOnceInternal(List<Army> defenders, List<Army> attackers, int compositeAFCM,
            int compositeDFCM)
        {
            // Order attackers by strength from weakest to strongest
            var defendingTile = defenders[0].Tile;
            attackers.Sort(new ByArmyBattleOrder(defendingTile));
            defenders.Sort(new ByArmyBattleOrder(defendingTile));

            var currentAttacker = attackers[0];
            var currentDefender = defenders[0];

            // Max strength of 9 due to die of 10
            var attackStrength = Math.Min(compositeAFCM + currentAttacker.ModifiedStrength, 9);
            var defenseStrength = Math.Min(compositeDFCM + currentDefender.ModifiedStrength, 9);

            var attackSucceeded = AttackRoll(currentAttacker, attackStrength, currentDefender, defenseStrength);
            if (attackSucceeded)
            {
                // Current attacker won
                Log.WriteLine(Log.TraceLevel.Information, "Current attacker won.");
                currentDefender.Kill();
                defenders.Remove(currentDefender);
            }
            else
            {
                // Current attacker lost
                Log.WriteLine(Log.TraceLevel.Information, "Current attacker lost.");
                currentAttacker.Kill();
                attackers.Remove(currentAttacker);
            }

            return attackSucceeded;
        }

        /// <summary>
        ///     Apply each army's clan terrain affinity to its combat strength.
        ///     Armies fighting in their preferred terrain get a bonus; in disfavored terrain, a penalty.
        /// </summary>
        private static void ApplyArmyTerrainModifiers(IList<Army> armies, Tile target)
        {
            foreach (var army in armies)
            {
                var terrainBonus = army.Clan.GetTerrainModifier(target);
                army.ModifiedStrength = Math.Max(1, army.Strength + terrainBonus);
            }
        }

        /// <summary>
        ///     Neutral city combat. Each defense point is a "phantom guard" of strength equal
        ///     to the city's defense rating. Attackers must defeat all guard points to capture.
        /// </summary>
        private bool AttackNeutralCity(List<Army> attackers, Tile tile)
        {
            var city = tile.City;
            ApplyArmyTerrainModifiers(attackers, tile);
            var compositeAFCM = attackers.Sum(a => a.GetAttackModifier(tile));

            while (attackers.Count > 0 && city.Defense > 0)
            {
                attackers.Sort(new ByArmyBattleOrder(tile));
                var currentAttacker = attackers[0];
                var attackStrength = Math.Min(compositeAFCM + currentAttacker.ModifiedStrength, 9);
                var cityStrength = Math.Min(city.Defense, 9);

                if (NeutralCityRoll(currentAttacker, attackStrength, cityStrength))
                {
                    city.Defense--;
                    currentAttacker.Reset();
                }
                else
                {
                    attackers.Remove(currentAttacker);
                }
            }

            return attackers.Count > 0;
        }

        /// <summary>
        ///     Resolve one phantom-guard combat round against a neutral city.
        ///     Returns true if attacker won; kills the army and returns false if city won.
        /// </summary>
        private static bool NeutralCityRoll(Army attacker, int attackStrength, int cityStrength)
        {
            var random = Game.Current.Random;
            var attackerHp = ArmyFactory.DefaultHitPoints;
            var cityHp = ArmyFactory.DefaultHitPoints;

            while (attackerHp > 0 && cityHp > 0)
            {
                var attackerRoll = random.Next(1, 11);
                var cityRoll = random.Next(1, 11);

                var attackerRollLow = attackerRoll <= cityStrength;
                var cityRollLow = cityRoll <= attackStrength;

                if (attackerRollLow && !cityRollLow)
                {
                    attackerHp--;
                }
                else if (!attackerRollLow && cityRollLow)
                {
                    cityHp--;
                }
            }

            if (cityHp <= 0)
            {
                return true;
            }

            attacker.Kill();
            return false;
        }

        private static bool AttackRoll(Army attacker, int attackStrength, Army defender, int defenseStrength)
        {
            var random = Game.Current.Random;
            // Have we won?
            if (defender.HitPoints == 0)
            {
                return true;
            }

            // Have we lost?
            if (attacker.HitPoints == 0)
            {
                return false;
            }

            // No? Then keep fighting!
            var attackerRoll = random.Next(1, 11); // Roll 10 sided die
            var defenderRoll = random.Next(1, 11); // Roll 10 sided die

            var attackerRollLow = attackerRoll <= defenseStrength;
            var defenderRollLow = defenderRoll <= attackStrength;

            // Attacker took a hit
            if (attackerRollLow && !defenderRollLow)
            {
                attacker.HitPoints--;
            }
            // Defender took a hit
            else if (!attackerRollLow && defenderRollLow)
            {
                defender.HitPoints--;
            }

            return AttackRoll(attacker, attackStrength, defender, defenseStrength);
        }
    }
}
