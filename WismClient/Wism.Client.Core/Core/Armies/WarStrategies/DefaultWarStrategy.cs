using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.War
{
    /// <summary>
    /// Default Warlords combat rules.
    /// </summary>
    public class DefaultWarStrategy : IWarStrategy
    {
        /// <summary>
        /// Combat is resolved. Attacking and Defending armies are sorted on the display with 
        /// the most valuable armies on the right hand side.Combat is a series of one-on-one
        /// engagements between the left-most army of each side.Each combat is fought to the
        /// death with the survivor going on to fight his opponents's next army. The battle 
        /// ends when one side is eliminated.The battle mecanics work like this. Each army
        /// rolls a ten-sided die (1-10). The result is low if the die roll is less than or
        /// equal to his opponent's AS (or DS as the case may be). The result is high if the
        /// die roll is greater than his opponent's AS (or DS). 
        /// 
        /// 1) If both rolls are high or both rolls are low, then the step is repeated.
        /// 2) If one rolls low and the other rolls high, then the low roller takes 1 hit. 
        /// 3) If the defender rolls high and the attacker rolls low, the defender takes 1 hit.
        ///
        /// As soon as an army receives 2 hits it is destroyed.
        /// </summary>
        /// <param name="tile">Tile that is being attacked.</param>
        /// <returns>True if attacker wins; false otherwise.</returns>
        public bool Attack(List<Army> attackers, Tile tile)
        {
            List<Army> defenders = tile.MusterArmy();

            // Attack armys one-at-a-time to the death!
            while (attackers.Count > 0 && defenders.Count > 0)
            {
                if (AttackOnce(attackers, tile))
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
        /// Combat is begun. Attacking and Defending armies are sorted on the display with 
        /// the most valuable armies on the right hand side. Combat is a single one-on-one
        /// engagement between the left-most army of each side. Combat is fought to the
        /// death with the survivor going on to fight his opponents's next army. The battle 
        /// mecanics work like this. Each army rolls a ten-sided die (1-10). The result is 
        /// low if the die roll is less than or equal to his opponent's AS (or DS as the 
        /// case may be). The result is high if thedie roll is greater than his opponent's 
        /// AS (or DS). 
        /// 
        /// 1) If both rolls are high or both rolls are low, then the step is repeated.
        /// 2) If one rolls low and the other rolls high, then the low roller takes 1 hit. 
        /// 3) If the defender rolls high and the attacker rolls low, the defender takes 1 hit.
        ///
        /// As soon as an army receives 2 hits it is destroyed.
        /// </summary>
        /// <param name="target">Tile that is being attacked.</param>
        /// <returns>True if attacker wins; false otherwise.</returns>
        public bool AttackOnce(List<Army> attackers, Tile target)
        {
            AttackOnce(attackers, target, out bool wasSuccessful);

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

            PrepareArmiesForAttack(attackers, target, out List<Army> defenders, out int compositeAFCM, out int compositeDFCM);

            // Attacking a neutral city
            //if (target.HasCity() && target.City.Clan.ShortName == "Neutral")
            //{
            //    wasSuccessful = AttackNeutralCityOnce(attackers, compositeAFCM, target.City);
            //}
            // Attacking an empty city owned by a Player always succeeds
            if (defenders.Count == 0 && target.HasCity())
            {
                wasSuccessful = true;
            }
            // Attacking another army
            else
            {
                wasSuccessful = AttackOnceInternal(defenders, attackers, compositeAFCM, compositeDFCM);
            }
            
            if (BattleContinues(defenders, attackers))
            {
                // Keep fighting
                return true;
            }
            else
            {
                // Battle is over
                ResetHitPoints(defenders, attackers);
                return false;
            }
        }

        public bool BattleContinues(List<Army> defenders, List<Army> attacker)
        {
            return (attacker.Count > 0 && defenders.Count > 0);
        }

        private static void PrepareArmiesForAttack(List<Army> attackers, Tile target, 
            out List<Army> defenders, out int compositeAFCM, out int compositeDFCM)
        {
            // Muster all armys from composite tile (i.e. city) to defend
            defenders = target.MusterArmy();

            // Calculate composite modifieres
            compositeAFCM = attackers.Sum(a => a.GetAttackModifier(target));
            compositeDFCM = defenders.Sum(a => a.GetDefenseModifier());

            // Apply army-specific terrain modifiers (e.g. elves like forests)
            ApplyArmyTerrainModifiers(attackers, target);
            ApplyArmyTerrainModifiers(defenders, target);
        }

        private static void ResetHitPoints(List<Army> defenders, List<Army> attackers)
        {
            attackers.ForEach(u => u.Reset());
            defenders.ForEach(u => u.Reset());
        }

        private static bool AttackOnceInternal(List<Army> defenders, List<Army> attackers, int compositeAFCM, int compositeDFCM)
        {
            // Order attackers by strength from weakest to strongest
            Tile defendingTile = defenders[0].Tile;
            attackers.Sort(new ByArmyBattleOrder(defendingTile));
            defenders.Sort(new ByArmyBattleOrder(defendingTile));

            Army currentAttacker = attackers[0];
            Army currentDefender = defenders[0];

            // Max strength of 9 due to die of 10
            int attackStrength = Math.Min(compositeAFCM + currentAttacker.ModifiedStrength, 9);
            int defenseStrength = Math.Min(compositeDFCM + currentDefender.ModifiedStrength, 9);

            bool attackSucceeded = AttackRoll(currentAttacker, attackStrength, currentDefender, defenseStrength);
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

        private static void ApplyArmyTerrainModifiers(IList<Army> armys, Tile target)
        {
            foreach (Army army in armys)
            {
                // TODO: Apply army-specific modifiers; for now just raw stregth
                army.ModifiedStrength = army.Strength;
            }
            return;
        }

        private static bool AttackRoll(Army attacker, int attackStrength, Army defender, int defenseStrength)
        {
            Random random = Game.Current.Random;
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
            int attackerRoll = random.Next(1, 11);  // Roll 10 sided die
            int defenderRoll = random.Next(1, 11);  // Roll 10 sided die

            bool attackerRollLow = (attackerRoll <= defenseStrength);
            bool defenderRollLow = (defenderRoll <= attackStrength);

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
