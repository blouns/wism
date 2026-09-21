using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public enum StrategicReportKind { Cities, Armies, Gold, Winning, Production }

    public sealed class ClanReportRow
    {
        public ClanReportRow(string name, string color, double cities, double armies, double strength, double gold, bool eliminated)
        {
            Name = name;
            Color = color;
            Cities = Math.Max(0, cities);
            Armies = Math.Max(0, armies);
            Strength = Math.Max(0, strength);
            Gold = Math.Max(0, gold);
            Eliminated = eliminated;
        }
        public string Name { get; }
        public string Color { get; }
        public double Cities { get; }
        public double Armies { get; }
        public double Strength { get; }
        public double Gold { get; }
        public bool Eliminated { get; }
    }

    public static class StrategicReportData
    {
        public static double ProducingPercent(IReadOnlyList<ProductionCityViewModel> cities) =>
            cities.Count == 0 ? 0 : 100.0 * cities.Count(city => !city.IsIdle) / cities.Count;

        public static ClanReportRow[] Capture(Game game) => game.Players.Select(player =>
        {
            var armies = player.GetArmies().Where(army => !army.IsDead).ToArray();
            return new ClanReportRow(player.Clan.DisplayName, player.Clan.Info.PrimaryColor ?? player.Clan.Info.Color,
                player.GetCities().Count, armies.Length, armies.Sum(army => (double)Math.Max(0, army.Strength)), player.Gold, player.IsDead);
        }).ToArray();

        public static double[] Values(IReadOnlyList<ClanReportRow> rows, StrategicReportKind kind)
        {
            if (kind == StrategicReportKind.Production)
                throw new ArgumentException("Production uses owned-city data, not clan shares.", nameof(kind));
            double cities = rows.Where(row => !row.Eliminated).Sum(row => row.Cities);
            double strength = rows.Where(row => !row.Eliminated).Sum(row => row.Strength);
            double gold = rows.Where(row => !row.Eliminated).Sum(row => row.Gold);
            return rows.Select(row => kind == StrategicReportKind.Cities ? row.Cities :
                kind == StrategicReportKind.Armies ? row.Armies : kind == StrategicReportKind.Gold ? row.Gold :
                row.Eliminated ? 0 : 50 * Share(row.Cities, cities) + 35 * Share(row.Strength, strength) + 15 * Share(row.Gold, gold)).ToArray();
        }

        private static double Share(double value, double total) => total > 0 ? value / total : 0;
    }
}
