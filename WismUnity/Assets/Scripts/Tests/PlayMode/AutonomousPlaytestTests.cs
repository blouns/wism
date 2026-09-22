using System.Linq;
using Assets.Scripts.Telemetry;
using NUnit.Framework;

public sealed class AutonomousPlaytestTests
{
    [Test]
    public void SettingsUseEightDistinctAiClansAndNormalIlluriaSetup()
    {
        var settings = AutonomousPlaytest.Settings();
        Assert.That(settings.Players.Length, Is.EqualTo(8));
        Assert.That(settings.Players.Select(p => p.ClanName).Distinct().Count(), Is.EqualTo(8));
        Assert.That(settings.Players.All(p => !p.IsHuman), Is.True);
        Assert.That(settings.WorldName, Is.EqualTo("Illuria"));
        Assert.That(settings.RandomSeed, Is.EqualTo(1990));
        Assert.That(settings.IsNewGame && settings.InteractiveUI, Is.True);
        Assert.That(settings.ShowAiCombat, Is.False);
    }
}
