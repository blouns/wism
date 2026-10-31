using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
[NonParallelizable]
public class DisbandArmiesCommandTests
{
    private ArmyController controller;
    private Player player;
    private Tile tile;

    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        this.controller = TestUtilities.CreateArmyController();
        this.player = Game.Current.GetCurrentPlayer();
        this.tile = World.Current.Map[2, 2];
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OrdinaryStack_RemovesFromTileRosterSelectionAndUpkeep(bool selected)
    {
        var stack = new List<Army> { this.Conscript(), this.Conscript() };
        var survivor = this.Conscript(World.Current.Map[3, 3]);
        if (selected)
            Game.Current.SelectArmies(stack);
        var command = this.Command(stack);
        var upkeep = this.player.GetUpkeep();
        var removedUpkeep = stack.Sum(army => army.Upkeep);
        Assert.That(removedUpkeep, Is.GreaterThan(0));
        Assert.That(command.Preview().ArmiesToDisband, Is.EquivalentTo(stack));
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.Multiple(() =>
        {
            Assert.That(stack.All(army => army.IsDead), Is.True);
            Assert.That(this.tile.GetAllArmies(), Is.Empty);
            Assert.That(this.player.GetArmies(), Is.EquivalentTo(new[] { survivor }));
            Assert.That(Game.Current.GetSelectedArmies(), Is.Null);
            Assert.That(Game.Current.GameState, Is.EqualTo(GameState.Ready));
            Assert.That(this.player.GetUpkeep(), Is.EqualTo(upkeep - removedUpkeep));
        });
        Assert.That(Game.Current.SelectNextArmy(), Is.True);
        Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new[] { survivor }));
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(command.Preview().RejectionReason, Is.EqualTo(DisbandRejectionReason.AlreadyExecuted));
        Assert.That(this.player.GetUpkeep(), Is.EqualTo(survivor.Upkeep));
        Assert.That(this.Command(stack).Execute(), Is.EqualTo(ActionState.Failed));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MixedStack_PreservesLivingHeroInventoryAndSelection(bool selected)
    {
        var ordinary = this.Conscript();
        var hero = this.player.HireHero(this.tile);
        var item = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        this.tile.AddItem(item);
        hero.TakeAll();
        var strength = hero.Strength;
        var stack = new List<Army> { hero, ordinary };
        if (selected)
            Game.Current.SelectArmies(stack);
        var command = this.Command(stack);
        var before = this.Snapshot();
        var preview = command.Preview();
        Assert.Multiple(() =>
        {
            Assert.That(preview.CanExecute, Is.True);
            Assert.That(preview.ArmiesToDisband, Is.EquivalentTo(new[] { ordinary }));
            Assert.That(preview.HeroesToPreserve, Is.EquivalentTo(new[] { hero }));
            Assert.That(this.Snapshot(), Is.EqualTo(before), "Preview must not change the game.");
        });
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.Multiple(() =>
        {
            Assert.That(ordinary.IsDead, Is.True);
            Assert.That(hero.IsDead, Is.False);
            Assert.That(hero.Items, Is.EquivalentTo(new[] { item }));
            Assert.That(hero.Strength, Is.EqualTo(strength));
            Assert.That(World.Current.GetLooseItems(), Does.Not.Contain(item));
            Assert.That(this.tile.HasItems(), Is.False);
            Assert.That(this.tile.GetAllArmies(), Is.EquivalentTo(new[] { hero }));
            Assert.That(this.player.GetArmies(), Is.EquivalentTo(new[] { hero }));
            Assert.That(this.player.GetUpkeep(), Is.EqualTo(hero.Upkeep));
            Assert.That(Game.Current.GameState, Is.EqualTo(selected ? GameState.SelectedArmy : GameState.Ready));
            Assert.That(Game.Current.GetSelectedArmies(), selected ? Is.EquivalentTo(new[] { hero }) : Is.Null);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HeroesOnly_RejectsWithoutEffects(bool selected)
    {
        var hero = this.player.HireHero(this.tile);
        var stack = new List<Army> { hero };
        if (selected)
            Game.Current.SelectArmies(stack);
        var command = this.Command(stack);
        Assert.That(command.Preview().HeroesToPreserve, Is.EquivalentTo(new[] { hero }));
        this.AssertRejected(command, DisbandRejectionReason.HeroesOnly);
        Assert.That(hero.IsDead, Is.False);
    }

    [Test]
    public void SelectedSubset_RemovesOnlyRequestedOrdinaryArmy()
    {
        var first = this.Conscript();
        var second = this.Conscript();
        var hero = this.player.HireHero(this.tile);
        Game.Current.SelectArmies(new List<Army> { first, second, hero });
        Assert.That(this.Command(new List<Army> { first }).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new Army[] { second, hero }));
        Assert.That(this.tile.VisitingArmies, Is.EquivalentTo(new Army[] { second, hero }));
        Game.Current.DeselectArmies();
        Assert.That(this.tile.Armies, Is.EquivalentTo(new Army[] { second, hero }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BatchAcrossResidentAndSelectedStacks_UsesBothRemovalPaths(bool visitorFirst)
    {
        var resident = this.Conscript();
        var visitor = this.Conscript();
        var elsewhere = this.Conscript(World.Current.Map[3, 3]);
        var hero = this.player.HireHero(this.tile);
        Game.Current.SelectArmies(new List<Army> { visitor, hero });
        var stack = visitorFirst
            ? new List<Army> { visitor, resident, elsewhere, hero }
            : new List<Army> { resident, visitor, elsewhere, hero };
        Assert.That(this.Command(stack).Execute(), Is.EqualTo(ActionState.Succeeded));
        Assert.Multiple(() =>
        {
            Assert.That(new[] { resident, visitor, elsewhere }.All(army => army.IsDead), Is.True);
            Assert.That(this.player.GetArmies(), Is.EquivalentTo(new[] { hero }));
            Assert.That(this.tile.HasArmies(), Is.False);
            Assert.That(this.tile.VisitingArmies, Is.EquivalentTo(new[] { hero }));
            Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new[] { hero }));
            Assert.That(World.Current.Map[3, 3].GetAllArmies(), Is.Empty);
        });
    }

    [TestCase("null")]
    [TestCase("dead")]
    [TestCase("foreign")]
    [TestCase("unregistered")]
    [TestCase("detached")]
    [TestCase("duplicate")]
    [TestCase("duplicateMembership")]
    [TestCase("nullTile")]
    [TestCase("staleTile")]
    public void InvalidBatch_RejectsAtomicallyEvenAfterValidSelectedArmy(string invalidKind)
    {
        var valid = this.Conscript();
        Game.Current.SelectArmies(new List<Army> { valid });
        var otherTile = World.Current.Map[3, 3];
        var invalid = invalidKind == "foreign"
            ? Game.Current.Players[1].ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), otherTile)
            : this.Conscript(otherTile);
        var reason = DisbandRejectionReason.InvalidArmy;
        switch (invalidKind)
        {
            case "null": invalid = null; break;
            case "dead": invalid.Kill(); break;
            case "foreign": reason = DisbandRejectionReason.WrongPlayer; break;
            case "unregistered":
                invalid.Kill();
                invalid.IsDead = false;
                otherTile.AddArmies(new List<Army> { invalid });
                reason = DisbandRejectionReason.StaleArmy;
                break;
            case "detached":
                otherTile.RemoveArmies(new List<Army> { invalid });
                reason = DisbandRejectionReason.StaleArmy;
                break;
            case "duplicate": invalid = valid; reason = DisbandRejectionReason.DuplicateArmy; break;
            case "duplicateMembership":
                otherTile.AddVisitingArmies(new List<Army> { invalid });
                reason = DisbandRejectionReason.StaleArmy;
                break;
            case "nullTile": invalid.Tile = null; break;
            case "staleTile":
                World.CreateWorld(TestUtilities.DefaultTestWorld);
                reason = DisbandRejectionReason.StaleArmy;
                break;
        }
        this.AssertRejected(this.Command(new List<Army> { valid, invalid }), reason);
        Assert.That(valid.IsDead, Is.False);
        Assert.That(this.player.GetArmies(), Does.Contain(valid));
    }

    [TestCase(GameState.Ready)]
    [TestCase(GameState.SelectedArmy)]
    public void UnrelatedVisitors_AreRejected(GameState state)
    {
        var valid = this.Conscript(World.Current.Map[3, 3]);
        var visitor = this.Conscript();
        this.tile.RemoveArmies(new List<Army> { visitor });
        this.tile.AddVisitingArmies(new List<Army> { visitor });
        if (state == GameState.SelectedArmy)
            Game.Current.SelectArmies(new List<Army> { valid });
        this.AssertRejected(this.Command(new List<Army> { valid, visitor }),
            DisbandRejectionReason.UnrelatedVisitor);
    }

    [TestCase(GameState.MovingArmy)]
    [TestCase(GameState.AttackingArmy)]
    [TestCase(GameState.CompletedBattle)]
    [TestCase(GameState.EndingTurn)]
    [TestCase(GameState.StartingTurn)]
    [TestCase(GameState.GameOver)]
    public void UnsafeGameStates_RejectWithoutEffects(GameState state)
    {
        var army = this.Conscript();
        Game.Current.SelectArmies(new List<Army> { army });
        var command = this.Command(new List<Army> { army });
        Game.Current.Transition(state);
        this.AssertRejected(command, DisbandRejectionReason.InvalidGameState);
    }

    [Test]
    public void WrongTurn_RejectsPreviouslyValidPreview()
    {
        var command = this.Command(new List<Army> { this.Conscript() });
        Assert.That(command.Preview().CanExecute, Is.True);
        Game.Current.EndTurn();
        Game.Current.Transition(GameState.Ready);
        Assert.That(Game.Current.GetCurrentPlayer(), Is.Not.SameAs(this.player));
        this.AssertRejected(command, DisbandRejectionReason.WrongPlayer);
    }

    [Test]
    public void Load_RejectsCommandAndLivingArmyReferencesFromPreviousGame()
    {
        var army = this.Conscript();
        var command = this.Command(new List<Army> { army });
        GameFactory.Load(Game.Current.Snapshot());
        this.AssertRejected(command, DisbandRejectionReason.StaleGame);
        this.AssertRejected(this.Command(new List<Army> { army }), DisbandRejectionReason.WrongPlayer);
        army.Player = Game.Current.GetCurrentPlayer();
        army.Tile = World.Current.Map[2, 2];
        this.AssertRejected(this.Command(new List<Army> { army }), DisbandRejectionReason.StaleArmy);
    }

    [Test]
    public void DeadCurrentPlayer_RejectsWithoutEffects()
    {
        var command = this.Command(new List<Army> { this.Conscript() });
        this.player.IsDead = true;
        this.AssertRejected(command, DisbandRejectionReason.WrongPlayer);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyOrNullBatch_RejectsWithoutThrowing(bool nullBatch)
    {
        this.AssertRejected(this.Command(nullBatch ? null : new List<Army>()),
            DisbandRejectionReason.EmptySelection);
    }

    [Test]
    public void NullArmyOnly_RejectsWithoutThrowing()
    {
        this.AssertRejected(this.Command(new List<Army> { null }), DisbandRejectionReason.InvalidArmy);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnloadedGame_RejectsWithoutRemovingArmies(bool unloadBeforeConstruction)
    {
        var army = this.Conscript();
        var stack = new List<Army> { army };
        var command = unloadBeforeConstruction ? null : this.Command(stack);
        Game.Unload();
        command ??= this.Command(stack);
        Assert.That(command.Preview().RejectionReason, Is.EqualTo(DisbandRejectionReason.NoActiveGame));
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(army.IsDead, Is.False);
        Assert.That(this.player.GetArmies(), Does.Contain(army));
        Assert.That(this.tile.GetAllArmies(), Does.Contain(army));
    }

    [Test]
    public void PreviewDoesNotAuthorizeLaterInvalidBatch()
    {
        var first = this.Conscript();
        var second = this.Conscript();
        var input = new List<Army> { first, second };
        var command = this.Command(input);
        var preview = command.Preview();
        input.Clear();
        Assert.That(command.Armies, Has.Count.EqualTo(2), "The caller cannot rewrite the command batch.");
        Assert.Throws<NotSupportedException>(() => ((IList<Army>)preview.ArmiesToDisband).Clear());
        second.IsDead = true;
        this.AssertRejected(command, DisbandRejectionReason.InvalidArmy);
        Assert.That(first.IsDead, Is.False);
    }

    [Test]
    public void NextQueue_PrimedBeforeDisbandSkipsRemovedTile()
    {
        var first = this.Conscript();
        var secondTile = World.Current.Map[3, 3];
        var second = this.Conscript(secondTile);
        Game.Current.SelectNextArmy();
        var selected = Game.Current.GetSelectedArmies().Single();
        var queued = selected == first ? second : first;
        Assert.That(this.Command(new List<Army> { queued }).Execute(), Is.EqualTo(ActionState.Succeeded));
        Game.Current.DeselectArmies();
        Assert.That(Game.Current.SelectNextArmy(), Is.False, "The queued empty tile is skipped.");
        Assert.That(Game.Current.SelectNextArmy(), Is.True);
        Assert.That(Game.Current.GetSelectedArmies(), Is.EquivalentTo(new[] { selected }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SaveLoad_PreservesSurvivorsInventorySelectionAndUpkeep(bool selected)
    {
        var ordinary = this.Conscript();
        var hero = this.player.HireHero(this.tile);
        var survivor = this.Conscript(World.Current.Map[3, 3]);
        var item = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        this.tile.AddItem(item);
        hero.TakeAll();
        var stack = new List<Army> { ordinary, hero };
        if (selected)
            Game.Current.SelectArmies(stack);
        var oldCommand = this.Command(stack);
        Assert.That(oldCommand.Execute(), Is.EqualTo(ActionState.Succeeded));
        var upkeep = this.player.GetUpkeep();
        var strength = hero.Strength;
        var expectedCount = this.player.GetArmies().Count;
        var json = this.Snapshot();
        GameFactory.Load(JsonConvert.DeserializeObject<GameEntity>(json));
        var loadedPlayer = Game.Current.GetCurrentPlayer();
        var loadedHero = loadedPlayer.GetArmies().OfType<Hero>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(loadedPlayer.GetArmies(), Has.Count.EqualTo(expectedCount));
            Assert.That(loadedPlayer.GetArmies().All(army => !army.IsDead), Is.True);
            Assert.That(loadedPlayer.GetUpkeep(), Is.EqualTo(upkeep));
            Assert.That(loadedHero.Items.Select(artifact => artifact.ShortName), Is.EqualTo(new[] { "Firesword" }));
            Assert.That(loadedHero.Strength, Is.EqualTo(strength));
            Assert.That(World.Current.GetLooseItems(), Is.Empty);
            Assert.That(World.Current.Map[2, 2].GetAllArmies(), Is.EquivalentTo(new[] { loadedHero }));
            Assert.That(World.Current.Map[3, 3].GetAllArmies().Single().ShortName, Is.EqualTo(survivor.ShortName));
            Assert.That(Game.Current.GameState, Is.EqualTo(selected ? GameState.SelectedArmy : GameState.Ready));
            Assert.That(Game.Current.GetSelectedArmies(), selected ? Is.EquivalentTo(new[] { loadedHero }) : Is.Null);
        });
        var stale = this.Command(new List<Army> { ordinary });
        this.AssertRejected(stale, DisbandRejectionReason.InvalidArmy);
        Assert.That(oldCommand.Execute(), Is.EqualTo(ActionState.Failed));
    }

    private Army Conscript(Tile target = null)
    {
        var army = this.player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), target ?? this.tile);
        army.Upkeep = 4;
        return army;
    }

    private DisbandArmiesCommand Command(List<Army> armies)
    {
        return new DisbandArmiesCommand(this.controller, armies);
    }

    private string Snapshot()
    {
        var snapshot = Game.Current.Snapshot();
        snapshot.Timestamp = DateTime.UnixEpoch;
        return JsonConvert.SerializeObject(snapshot);
    }

    private void AssertRejected(DisbandArmiesCommand command, DisbandRejectionReason reason)
    {
        var before = this.Snapshot();
        var selected = Game.Current.GetSelectedArmies();
        var roster = Game.Current.Players.SelectMany(owner => owner.GetArmies()).ToArray();
        var upkeep = Game.Current.Players.Select(owner => owner.GetUpkeep()).ToArray();
        Assert.That(command.Preview().RejectionReason, Is.EqualTo(reason));
        Assert.That(command.Preview().CanExecute, Is.False);
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.Multiple(() =>
        {
            Assert.That(this.Snapshot(), Is.EqualTo(before));
            Assert.That(Game.Current.GetSelectedArmies(), selected == null ? Is.Null : Is.EquivalentTo(selected));
            Assert.That(Game.Current.Players.SelectMany(owner => owner.GetArmies()), Is.EqualTo(roster));
            Assert.That(Game.Current.Players.Select(owner => owner.GetUpkeep()), Is.EqualTo(upkeep));
        });
    }
}
