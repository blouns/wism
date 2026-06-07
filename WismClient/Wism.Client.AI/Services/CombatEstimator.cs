using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Comparers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Services
{
    public class CombatEstimator
    {
        public CombatEstimate EstimateAttack(List<Army> attackers, Tile targetTile)
        {
            if (attackers == null || attackers.Count == 0 || targetTile == null)
            {
                return CombatEstimate.NoAttack;
            }

            var attackerClan = attackers[0].Clan;
            var defenders = targetTile.MusterArmy()
                .Where(army => army.Clan != attackerClan)
                .ToList();

            if (defenders.Count == 0)
            {
                var emptyEnemyCity = targetTile.HasCity() && targetTile.City.Clan != attackerClan;
                return new CombatEstimate(emptyEnemyCity ? 1.0 : 0.0, attackers.Count, 0);
            }

            var orderedAttackers = attackers.ToList();
            var orderedDefenders = defenders.ToList();
            orderedAttackers.Sort(new ByArmyBattleOrder(targetTile));
            orderedDefenders.Sort(new ByArmyBattleOrder(targetTile));

            var attackerModifier = orderedAttackers.Sum(army => army.GetAttackModifier(targetTile));
            var defenderModifier = orderedDefenders.Sum(army => army.GetDefenseModifier());
            var winProbability = EstimateStackWinProbability(
                orderedAttackers,
                orderedDefenders,
                attackerModifier,
                defenderModifier,
                0,
                0);

            return new CombatEstimate(winProbability, orderedAttackers.Count, orderedDefenders.Count);
        }

        private static double EstimateStackWinProbability(
            IReadOnlyList<Army> attackers,
            IReadOnlyList<Army> defenders,
            int attackerModifier,
            int defenderModifier,
            int attackerIndex,
            int defenderIndex)
        {
            if (defenderIndex >= defenders.Count)
            {
                return 1.0;
            }

            if (attackerIndex >= attackers.Count)
            {
                return 0.0;
            }

            var attackStrength = ClampCombatStrength(attackers[attackerIndex].Strength + attackerModifier);
            var defenseStrength = ClampCombatStrength(defenders[defenderIndex].Strength + defenderModifier);
            var duelWin = EstimateDuelWinProbability(attackStrength, defenseStrength);

            return (duelWin * EstimateStackWinProbability(
                    attackers,
                    defenders,
                    attackerModifier,
                    defenderModifier,
                    attackerIndex,
                    defenderIndex + 1)) +
                ((1.0 - duelWin) * EstimateStackWinProbability(
                    attackers,
                    defenders,
                    attackerModifier,
                    defenderModifier,
                    attackerIndex + 1,
                    defenderIndex));
        }

        private static double EstimateDuelWinProbability(int attackStrength, int defenseStrength)
        {
            var attackerHitProbability = (1.0 - (defenseStrength / 10.0)) * (attackStrength / 10.0);
            var defenderHitProbability = (defenseStrength / 10.0) * (1.0 - (attackStrength / 10.0));
            var hitProbability = attackerHitProbability + defenderHitProbability;

            if (hitProbability <= 0.0)
            {
                return 0.5;
            }

            var attackerScoresHit = attackerHitProbability / hitProbability;
            return Math.Pow(attackerScoresHit, 2) * (3.0 - (2.0 * attackerScoresHit));
        }

        private static int ClampCombatStrength(int strength)
        {
            return Math.Min(Army.MaxStrength, Math.Max(0, strength));
        }
    }

    public class CombatEstimate
    {
        public static readonly CombatEstimate NoAttack = new CombatEstimate(0.0, 0, 0);

        public CombatEstimate(double winProbability, int attackerCount, int defenderCount)
        {
            this.WinProbability = winProbability;
            this.AttackerCount = attackerCount;
            this.DefenderCount = defenderCount;
        }

        public double WinProbability { get; }

        public int AttackerCount { get; }

        public int DefenderCount { get; }
    }
}
