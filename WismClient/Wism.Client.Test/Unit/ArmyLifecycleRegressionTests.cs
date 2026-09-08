using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class ArmyLifecycleRegressionTests
{
    [Test]
    public void InsufficientTerrainMoves_SkipsArmyUntilExplicitReselection()
    {
        Game.CreateDefaultGame();
        var provider = TestUtilities.CreateControllerProvider();
        var player = Game.Current.GetCurrentPlayer();
        var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 1]);
        var stack = new List<Army> { army };
        var target = World.Current.Map[2, 1];
        target.Terrain = MapBuilder.TerrainKinds["Forest"];
        army.MovesRemaining = 1;
        TestUtilities.Select(provider, stack);
        Assert.That(provider.ArmyController.TryMove(stack, target), Is.EqualTo(Wism.Client.Controllers.MoveResult.InsuffientMoves));
        Assert.That(army.MovesRemaining, Is.EqualTo(1));
        for (var i = 0; i < 4; i++)
        {
            Game.Current.SelectNextArmy();
            Assert.That(Game.Current.GetSelectedArmies() ?? new List<Army>(), Does.Not.Contain(army));
        }
        TestUtilities.Select(provider, stack);
        Assert.That(Game.Current.GetSelectedArmies(), Does.Contain(army), "Manual selection still overrides Quit for this turn.");
    }

    [Test]
    public void ClanDefeat_WithdrawsLivingHeroWithoutDroppingItems()
    {
        var provider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(provider, TestUtilities.DefaultTestWorld);
        var loser = Game.Current.Players[1];
        var hero = loser.HireHero(World.Current.Map[2, 2]);
        var artifact = ModFactory.LoadArtifacts(ModFactory.ModPath).First();
        hero.Items = new List<Artifact> { artifact };
        var tile = hero.Tile;
        Game.Current.Players[0].ClaimCity(loser.Capitol);
        typeof(Player).GetMethod("Eliminate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(loser, null);
        Assert.Multiple(() =>
        {
            Assert.That(loser.IsDead, Is.True);
            Assert.That(hero.IsDead, Is.False, "Faction elimination is withdrawal, not a combat death.");
            Assert.That(hero.Items, Does.Contain(artifact));
            Assert.That(tile.Items ?? new List<Artifact>(), Does.Not.Contain(artifact));
            Assert.That(World.Current.GetLooseItems(), Does.Not.Contain(artifact));
            Assert.That(tile.MusterArmy(), Does.Not.Contain(hero));
            Assert.That(loser.GetArmies(), Does.Not.Contain(hero));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HeroDeathOrExplicitDrop_StillDropsExactlyOnce(bool death)
    {
        var provider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(provider, TestUtilities.DefaultTestWorld);
        var hero = Game.Current.Players[0].HireHero(Game.Current.Players[0].Capitol.Tile);
        var artifact = ModFactory.LoadArtifacts(ModFactory.ModPath).First();
        hero.Items = new List<Artifact> { artifact };
        if (death) { hero.Kill(); hero.Kill(); } else { hero.DropAll(); hero.DropAll(); }
        Assert.That(hero.Items, Is.Empty);
        Assert.That(World.Current.GetLooseItems().Count(item => item == artifact), Is.EqualTo(1));
    }
}
