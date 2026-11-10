using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public enum StrategicReportKind { Cities, Armies, Gold, Winning, Production, Heroes, Ruins }

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
        public static Hero[] Heroes(Player player) => player.GetHeros()
            .Where(hero => !hero.IsDead && hero.Tile != null).OrderBy(hero => hero.DisplayName)
            .ThenBy(hero => hero.Id).ToArray();

        public static Location[] Ruins(World world) => world.GetLocations()
            .Where(location => location.Tile != null && !location.IsTowerSite &&
                (location.Kind == "Ruins" || location.Kind == "Tomb"))
            .OrderBy(location => location.DisplayName).ThenBy(location => location.X)
            .ThenBy(location => location.Y).ToArray();

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
            if (kind == StrategicReportKind.Production || kind == StrategicReportKind.Heroes || kind == StrategicReportKind.Ruins)
                throw new ArgumentException("This report uses map entities, not clan shares.", nameof(kind));
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
