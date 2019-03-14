using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{
    public interface IWarStrategy
    {
        /// <summary>
        /// Combat strategy for attacking a given tile with a unit (entire stack)
        /// </summary>
        /// <param name="attacker">Unit attacking</param>
        /// <param name="tile">Tile being defended</param>
        /// <returns>True if attaker wins; false otherwise</returns>
        bool Attack(Army attacker, Tile tile);

        /// <summary>
        /// Combat strategy for attacking a given tile with a unit (one unit in stack)
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        bool AttackOnce(Army attacker, Tile tile);

        /// <summary>
        /// Combat strategy for attacking a given tile with a unit (one unit in stack)
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="tile"></param>
        /// <param name="wasSuccessful">True if attack succeeded; else false</param>
        /// <returns>True if the fight continues; else, the battle is over.</returns>
        bool AttackOnce(Army attacker, Tile tile, out bool wasSuccessful);
    }

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
        public bool Attack(Army attacker, Tile tile)
        {            
            Army defender = tile.Army;

            // Attack units one-at-a-time to the death!
            while (attacker.Size > 0 && defender.Size > 0)
            {
                if (AttackOnce(attacker, tile))
                {
                    Log.WriteLine(Log.TraceLevel.Information, "Attacker killed one army.");
                }
                else
                {
                    Log.WriteLine(Log.TraceLevel.Information, "Defender killed one army.");
                }
            }

            return attacker.Size > 0;
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
        public bool AttackOnce(Army attacker, Tile target)
        {
            AttackOnce(attacker, target, out bool wasSuccessful);

            return wasSuccessful;
        }

        public bool AttackOnce(Army attacker, Tile target, out bool wasSuccessful)
        {
            PrepareArmiesForAttack(attacker, target, out Army defender, out int compositeAFCM, out int compositeDFCM);

            wasSuccessful = AttackOnceInternal(defender, attacker, compositeAFCM, compositeDFCM);
            if (BattleContinues(defender, attacker))
            {
                // Keep fighting
                return true;
            }
            else
            {
                // Battle is over
                ResetHitPoints(defender, attacker);
                return false;
            }
        }

        private bool BattleContinues(Army defender, Army attacker)
        {
            return (attacker.Size > 0 && defender.Size > 0);
        }

        private void PrepareArmiesForAttack(Army attacker, Tile target, out Army defender, out int compositeAFCM, out int compositeDFCM)
        {
            // Muster all units from composite tile (i.e. city) to defend
            defender = target.MusterArmy();
            List<Unit> defenders = defender.GetUnits();
            List<Unit> attackers = attacker.GetUnits();

            // Calculate composite modifieres
            compositeAFCM = attacker.GetCompositeAttackModifier(target);
            compositeDFCM = defender.GetCompositeDefenseModifier();

            // Apply unit-specific terrain modifiers (e.g. elves like forests)
            ApplyUnitTerrainModifiers(attackers, target);
            ApplyUnitTerrainModifiers(defenders, target);

            // Order attackers by strength from weakest to strongest
            attackers.Sort(new ByUnitBattleOrder(target));
            attackers.OrderBy(v => v.ModifiedStrength);
            defenders.OrderBy(v => v.ModifiedStrength);
        }

        private static void ResetHitPoints(Army defender, Army attacker)
        {
            attacker.GetUnits().ForEach(u => u.Reset());
            defender.GetUnits().ForEach(u => u.Reset());
        }

        private bool AttackOnceInternal(Army defender, Army attacker, int compositeAFCM, int compositeDFCM)
        {
            Unit currentAttacker = attacker[0];
            Unit currentDefender = defender[0];

            // Max strength of 9 due to die of 10
            int attackStrength = Math.Min(compositeAFCM + currentAttacker.ModifiedStrength, 9);
            int defenseStrength = Math.Min(compositeDFCM + currentDefender.ModifiedStrength, 9);

            bool attackSucceeded = AttackRoll(currentAttacker, attackStrength, currentDefender, defenseStrength);
            if (attackSucceeded)
            {
                // Current attacker won
                Log.WriteLine(Log.TraceLevel.Information, "Current attacker won.");
                defender.Kill(currentDefender);
            }
            else
            {
                // Current attacker lost
                Log.WriteLine(Log.TraceLevel.Information, "Current attacker lost.");
                attacker.Kill(currentAttacker);
            }

            return attackSucceeded;
        }

        private void ApplyUnitTerrainModifiers(IList<Unit> units, Tile target)
        {            
            foreach (Unit unit in units)
            {
                // TODO: Apply unit-specific modifiers; for now just raw strgth
                unit.ModifiedStrength = unit.Strength; 
            }
            return;
        }

        private bool AttackRoll(Unit attacker, int attackStrength, Unit defender, int defenseStrength)
        {
            Random random = World.Current.Random;
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
