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
        /// Combat strategy for attacking a given tile with a unit.
        /// </summary>
        /// <param name="attacker">Unit attacking</param>
        /// <param name="tile">Tile being defended</param>
        /// <returns>True if attaker wins; false otherwise</returns>
        bool Attack(Unit attacker, Tile tile);
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
        public bool Attack(Unit attacker, Tile tile)
        {
            Unit defender = tile.Unit;

            // Muster all units from composite tile (i.e. city) to defend
            IList<Unit> defenders = tile.GetDefenders();

            // Expand composite unit (i.e. stack)
            IList<Unit> attackers = attacker.Expand();

            // Calculate composite modifieres
            int compositeAFCM = attacker.GetAttackModifier();
            int compositeDFCM = defender.GetDefenseModifier();

            // Apply unit-specific terrain modifiers (e.g. elves like forests)
            ApplyUnitTerrainModifiers(attackers);
            ApplyUnitTerrainModifiers(defenders);

            // Order attackers by strength from weakest to strongest
            attackers.OrderBy(v => v.ModifiedStrength);
            defenders.OrderBy(v => v.ModifiedStrength);

            // Attack units one-at-a-time to the death!
            return AttackAllDefenders(defenders, attackers, compositeAFCM, compositeDFCM);
        }

        private bool AttackAllDefenders(IList<Unit> defenders, IList<Unit> attackers, int compositeAFCM, int compositeDFCM)
        {
            while (attackers.Count > 0 && defenders.Count > 0)
            {
                Unit currentAttacker = attackers.First();
                Unit currentDefender = defenders.First();

                // Max strength of 9 due to die of 10
                int attackStrength = Math.Min(compositeAFCM + currentAttacker.ModifiedStrength, 9);
                int defenseStrength = Math.Min(compositeDFCM + currentDefender.ModifiedStrength, 9);

                bool attackSucceeded = AttackRoll(currentAttacker, attackStrength, currentDefender, defenseStrength);
                if (attackSucceeded)
                {
                    // Current attacker won
                    Log.WriteLine(Log.TraceLevel.Information, "Current attacker won.");
                    defenders.RemoveAt(0);
                }
                else
                {
                    // Current attacker lost
                    Log.WriteLine(Log.TraceLevel.Information, "Current attacker lost.");
                    attackers.RemoveAt(0);
                }
            }

            // Reset hit points for victor
            foreach (Unit unit in attackers)
                unit.Reset();
            foreach (Unit unit in defenders)
                unit.Reset();

            // Determine winner
            bool attackerWon = (attackers.Count > 0);

            if ((attackers.Count > 0 && defenders.Count > 0) ||
                (attackers.Count == 0 && defenders.Count == 0))
            {
                throw new Exception(
                    String.Format("Attacker and defender count incorrect after battle: Attackers: {0}, Defenders: {1}",
                    attackers.Count, defenders.Count));
            }

            return attackerWon;
        }

        private void ApplyUnitTerrainModifiers(IList<Unit> units)
        {            
            foreach (Unit unit in units)
            {
                // TODO: Apply unit-specific modifiers; for now just raw strength
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
                defender.Kill();
                return true;
            }

            // Have we lost?
            if (attacker.HitPoints == 0)
            {
                attacker.Kill();
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
