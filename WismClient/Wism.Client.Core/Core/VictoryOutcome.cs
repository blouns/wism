using System;
using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.Core
{
    public enum VictoryOutcomeKind
    {
        None,
        Conquest,
        DominanceVictory,
        SurrenderOffered,
        AcceptedSurrender,
        RejectedSurrender,
        InspectionMode
    }

    public enum DominanceGoalMode
    {
        Readiness,
        FullConquest,
        NeutralExpansion,
        ProductionEconomy,
        ManualParitySurrender
    }

    public sealed class DominanceVictoryPolicy
    {
        public DominanceVictoryPolicy(
            bool enabled,
            double leaderCityShare,
            double leadOverRunnerUpShare,
            double maxUnclaimedCityShare,
            int minimumTurnsElapsed,
            double minimumArmyRatio,
            double minimumIncomeRatio,
            DominanceGoalMode goalMode,
            string policyId)
        {
            this.Enabled = enabled;
            this.LeaderCityShare = leaderCityShare;
            this.LeadOverRunnerUpShare = leadOverRunnerUpShare;
            this.MaxUnclaimedCityShare = maxUnclaimedCityShare;
            this.MinimumTurnsElapsed = minimumTurnsElapsed;
            this.MinimumArmyRatio = minimumArmyRatio;
            this.MinimumIncomeRatio = minimumIncomeRatio;
            this.GoalMode = goalMode;
            this.PolicyId = policyId;
        }

        public bool Enabled { get; }
        public double LeaderCityShare { get; }
        public double LeadOverRunnerUpShare { get; }
        public double MaxUnclaimedCityShare { get; }
        public int MinimumTurnsElapsed { get; }
        public double MinimumArmyRatio { get; }
        public double MinimumIncomeRatio { get; }
        public DominanceGoalMode GoalMode { get; }
        public string PolicyId { get; }

        public static DominanceVictoryPolicy Disabled { get; } = new DominanceVictoryPolicy(
            false,
            1.0,
            1.0,
            0.0,
            int.MaxValue,
            double.PositiveInfinity,
            double.PositiveInfinity,
            DominanceGoalMode.FullConquest,
            "disabled");

        public static DominanceVictoryPolicy ForEval(int activeClanCount, int totalCities, DominanceGoalMode goalMode)
        {
            var boundedClans = Math.Max(2, activeClanCount);
            var leaderShare = boundedClans <= 2
                ? 0.60
                : boundedClans <= 4
                    ? 0.55
                    : 0.52;
            var leadShare = boundedClans <= 2
                ? 0.20
                : boundedClans <= 4
                    ? 0.18
                    : 0.15;
            var minimumTurns = Math.Max(10, totalCities / boundedClans / 2);

            return new DominanceVictoryPolicy(
                true,
                leaderShare,
                leadShare,
                goalMode == DominanceGoalMode.NeutralExpansion ? 1.0 : 0.15,
                minimumTurns,
                goalMode == DominanceGoalMode.Readiness ? 1.15 : 0.0,
                goalMode == DominanceGoalMode.Readiness ? 1.15 : 0.0,
                goalMode,
                $"eval-{boundedClans}clans-{goalMode.ToString().ToLowerInvariant()}");
        }
    }

    public sealed class VictoryClanStanding
    {
        public VictoryClanStanding(
            string clanShortName,
            string clanDisplayName,
            int cityCount,
            int armyCount,
            int income,
            bool isHuman,
            bool isDead)
        {
            this.ClanShortName = clanShortName;
            this.ClanDisplayName = clanDisplayName;
            this.CityCount = cityCount;
            this.ArmyCount = armyCount;
            this.Income = income;
            this.IsHuman = isHuman;
            this.IsDead = isDead;
        }

        public string ClanShortName { get; }
        public string ClanDisplayName { get; }
        public int CityCount { get; }
        public int ArmyCount { get; }
        public int Income { get; }
        public bool IsHuman { get; }
        public bool IsDead { get; }
    }

    public sealed class VictoryOutcomeSnapshot
    {
        public VictoryOutcomeSnapshot(
            VictoryOutcomeKind outcomeKind,
            string winnerClanShortName,
            string winnerClanDisplayName,
            string runnerUpClanShortName,
            string runnerUpClanDisplayName,
            int totalCities,
            int leaderCities,
            double leaderCityShare,
            int runnerUpCities,
            double runnerUpCityShare,
            double leadOverRunnerUpShare,
            double unclaimedCityShare,
            int activeClanCount,
            int turns,
            bool isInferred,
            double leaderArmyRatio,
            double leaderIncomeRatio,
            bool dominanceEligible,
            string dominancePolicyId,
            bool surrenderEligible)
        {
            this.OutcomeKind = outcomeKind;
            this.WinnerClanShortName = winnerClanShortName;
            this.WinnerClanDisplayName = winnerClanDisplayName;
            this.RunnerUpClanShortName = runnerUpClanShortName;
            this.RunnerUpClanDisplayName = runnerUpClanDisplayName;
            this.TotalCities = totalCities;
            this.LeaderCities = leaderCities;
            this.LeaderCityShare = leaderCityShare;
            this.RunnerUpCities = runnerUpCities;
            this.RunnerUpCityShare = runnerUpCityShare;
            this.LeadOverRunnerUpShare = leadOverRunnerUpShare;
            this.UnclaimedCityShare = unclaimedCityShare;
            this.ActiveClanCount = activeClanCount;
            this.Turns = turns;
            this.IsInferred = isInferred;
            this.LeaderArmyRatio = leaderArmyRatio;
            this.LeaderIncomeRatio = leaderIncomeRatio;
            this.DominanceEligible = dominanceEligible;
            this.DominancePolicyId = dominancePolicyId;
            this.SurrenderEligible = surrenderEligible;
        }

        public VictoryOutcomeKind OutcomeKind { get; }
        public string WinnerClanShortName { get; }
        public string WinnerClanDisplayName { get; }
        public string RunnerUpClanShortName { get; }
        public string RunnerUpClanDisplayName { get; }
        public int TotalCities { get; }
        public int LeaderCities { get; }
        public double LeaderCityShare { get; }
        public int RunnerUpCities { get; }
        public double RunnerUpCityShare { get; }
        public double LeadOverRunnerUpShare { get; }
        public double UnclaimedCityShare { get; }
        public int ActiveClanCount { get; }
        public int Turns { get; }
        public bool IsInferred { get; }
        public double LeaderArmyRatio { get; }
        public double LeaderIncomeRatio { get; }
        public bool DominanceEligible { get; }
        public string DominancePolicyId { get; }
        public bool SurrenderEligible { get; }

        public VictoryOutcomeSnapshot WithOutcome(VictoryOutcomeKind outcomeKind, bool surrenderEligible)
        {
            return new VictoryOutcomeSnapshot(
                outcomeKind,
                this.WinnerClanShortName,
                this.WinnerClanDisplayName,
                this.RunnerUpClanShortName,
                this.RunnerUpClanDisplayName,
                this.TotalCities,
                this.LeaderCities,
                this.LeaderCityShare,
                this.RunnerUpCities,
                this.RunnerUpCityShare,
                this.LeadOverRunnerUpShare,
                this.UnclaimedCityShare,
                this.ActiveClanCount,
                this.Turns,
                this.IsInferred,
                this.LeaderArmyRatio,
                this.LeaderIncomeRatio,
                this.DominanceEligible,
                this.DominancePolicyId,
                surrenderEligible);
        }
    }

    public static class VictoryEvaluator
    {
        public static VictoryOutcomeSnapshot None(int turn = 0)
        {
            return new VictoryOutcomeSnapshot(
                VictoryOutcomeKind.None,
                null,
                null,
                null,
                null,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                turn,
                false,
                0,
                0,
                false,
                "none",
                false);
        }

        public static VictoryOutcomeSnapshot EvaluateClassicSurrender(World world, IReadOnlyList<Player> players, int turn)
        {
            var standings = BuildStandings(world, players);
            return EvaluateClassicSurrender(standings, world.GetCities().Count, turn);
        }

        public static VictoryOutcomeSnapshot EvaluateClassicSurrender(
            IReadOnlyList<VictoryClanStanding> standings,
            int totalCities,
            int turn)
        {
            var active = standings.Where(standing => !standing.IsDead).ToArray();
            var humans = active.Where(standing => standing.IsHuman).ToArray();
            var computers = active.Where(standing => !standing.IsHuman).ToArray();
            var ordered = OrderStandings(standings).ToArray();
            var leader = ordered.FirstOrDefault();
            var runnerUp = ordered.Skip(1).FirstOrDefault();
            var human = humans.Length == 1 ? humans[0] : null;
            var strongestComputerCities = computers.Length == 0 ? 0 : computers.Max(standing => standing.CityCount);
            var surrenderEligible = human != null &&
                                    humans.Length == 1 &&
                                    computers.Length == 7 &&
                                    human.CityCount > 40 &&
                                    strongestComputerCities < human.CityCount - 15;

            return CreateSnapshot(
                surrenderEligible ? VictoryOutcomeKind.SurrenderOffered : VictoryOutcomeKind.None,
                leader,
                runnerUp,
                totalCities,
                turn,
                false,
                Ratio(Math.Max(0, totalCities - standings.Sum(standing => standing.CityCount)), totalCities),
                active.Length,
                0,
                0,
                false,
                "classic-surrender",
                surrenderEligible);
        }

        public static VictoryOutcomeSnapshot EvaluateDominance(
            World world,
            IReadOnlyList<Player> players,
            int turn,
            DominanceVictoryPolicy policy)
        {
            var standings = BuildStandings(world, players);
            return EvaluateDominance(standings, world.GetCities().Count, turn, policy);
        }

        public static VictoryOutcomeSnapshot EvaluateDominance(
            IReadOnlyList<VictoryClanStanding> standings,
            int totalCities,
            int turn,
            DominanceVictoryPolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            var ordered = OrderStandings(standings).ToArray();
            var leader = ordered.FirstOrDefault();
            var runnerUp = ordered.Skip(1).FirstOrDefault();
            var activeClanCount = standings.Count(standing => !standing.IsDead && (standing.CityCount > 0 || standing.ArmyCount > 0));
            var leaderShare = Ratio(leader?.CityCount ?? 0, totalCities);
            var runnerShare = Ratio(runnerUp?.CityCount ?? 0, totalCities);
            var leadShare = leaderShare - runnerShare;
            var unclaimedShare = Ratio(Math.Max(0, totalCities - standings.Sum(standing => standing.CityCount)), totalCities);
            var leaderArmyRatio = Ratio(leader?.ArmyCount ?? 0, Math.Max(1, runnerUp?.ArmyCount ?? 0));
            var leaderIncomeRatio = Ratio(leader?.Income ?? 0, Math.Max(1, runnerUp?.Income ?? 0));
            var momentumSatisfied = policy.MinimumArmyRatio <= 0 &&
                                    policy.MinimumIncomeRatio <= 0 ||
                                    leaderArmyRatio >= policy.MinimumArmyRatio ||
                                    leaderIncomeRatio >= policy.MinimumIncomeRatio;
            var dominanceEligible = policy.Enabled &&
                                    policy.GoalMode != DominanceGoalMode.FullConquest &&
                                    leader != null &&
                                    activeClanCount > 1 &&
                                    turn >= policy.MinimumTurnsElapsed &&
                                    leaderShare >= policy.LeaderCityShare &&
                                    leadShare >= policy.LeadOverRunnerUpShare &&
                                    unclaimedShare <= policy.MaxUnclaimedCityShare &&
                                    momentumSatisfied;

            return CreateSnapshot(
                dominanceEligible ? VictoryOutcomeKind.DominanceVictory : VictoryOutcomeKind.None,
                leader,
                runnerUp,
                totalCities,
                turn,
                false,
                unclaimedShare,
                activeClanCount,
                leaderArmyRatio,
                leaderIncomeRatio,
                dominanceEligible,
                policy.PolicyId,
                false);
        }

        public static void AcceptSurrender(Game game, World world, VictoryOutcomeSnapshot offer)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (offer == null || !offer.SurrenderEligible || string.IsNullOrWhiteSpace(offer.WinnerClanShortName))
            {
                throw new InvalidOperationException("A surrender offer is required before surrender can be accepted.");
            }

            var winner = game.Players.Single(player => player.Clan.ShortName == offer.WinnerClanShortName);
            foreach (var player in game.Players.Where(player => player != winner).ToArray())
            {
                foreach (var army in player.GetArmies().ToArray())
                {
                    player.TransferArmyTo(army, winner);
                }
            }

            foreach (var city in world.GetCities().Where(city => city.Clan != winner.Clan).ToArray())
            {
                winner.ClaimCity(city);
            }

            game.SetVictoryOutcome(offer.WithOutcome(VictoryOutcomeKind.InspectionMode, false));
            game.Transition(GameState.GameOver);
        }

        public static void RejectSurrender(Game game, VictoryOutcomeSnapshot offer)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (offer == null || !offer.SurrenderEligible)
            {
                throw new InvalidOperationException("A surrender offer is required before surrender can be rejected.");
            }

            game.SetVictoryOutcome(offer.WithOutcome(VictoryOutcomeKind.RejectedSurrender, false));
        }

        private static IReadOnlyList<VictoryClanStanding> BuildStandings(World world, IReadOnlyList<Player> players)
        {
            return players.Select(player => new VictoryClanStanding(
                    player.Clan.ShortName,
                    player.Clan.DisplayName,
                    player.GetCities().Count,
                    player.GetArmies().Count(army => !army.IsDead),
                    player.GetIncome(),
                    player.IsHuman,
                    player.IsDead))
                .ToArray();
        }

        private static IEnumerable<VictoryClanStanding> OrderStandings(IReadOnlyList<VictoryClanStanding> standings)
        {
            return standings
                .Where(standing => !standing.IsDead)
                .OrderByDescending(standing => standing.CityCount)
                .ThenByDescending(standing => standing.ArmyCount)
                .ThenBy(standing => standing.ClanShortName, StringComparer.OrdinalIgnoreCase);
        }

        private static VictoryOutcomeSnapshot CreateSnapshot(
            VictoryOutcomeKind kind,
            VictoryClanStanding leader,
            VictoryClanStanding runnerUp,
            int totalCities,
            int turn,
            bool isInferred,
            double unclaimedCityShare,
            int activeClanCount,
            double leaderArmyRatio,
            double leaderIncomeRatio,
            bool dominanceEligible,
            string dominancePolicyId,
            bool surrenderEligible)
        {
            var leaderCities = leader?.CityCount ?? 0;
            var runnerUpCities = runnerUp?.CityCount ?? 0;
            var leaderShare = Ratio(leaderCities, totalCities);
            var runnerShare = Ratio(runnerUpCities, totalCities);
            return new VictoryOutcomeSnapshot(
                kind,
                leader?.ClanShortName,
                leader?.ClanDisplayName,
                runnerUp?.ClanShortName,
                runnerUp?.ClanDisplayName,
                totalCities,
                leaderCities,
                leaderShare,
                runnerUpCities,
                runnerShare,
                leaderShare - runnerShare,
                unclaimedCityShare,
                activeClanCount,
                turn,
                isInferred,
                leaderArmyRatio,
                leaderIncomeRatio,
                dominanceEligible,
                dominancePolicyId,
                surrenderEligible);
        }

        private static double Ratio(int numerator, int denominator)
        {
            return denominator <= 0 ? 0 : Math.Round(numerator / (double)denominator, 4);
        }
    }
}
