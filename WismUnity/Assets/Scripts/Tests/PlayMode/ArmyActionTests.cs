using Assets.Scripts.Managers;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wism.Client.Core;
using Wism.Client.MapObjects;

public class ArmyActionTests : IPrebuildSetup, IPostBuildCleanup
{
    #region Test scene setup

    public static string TestWorld = "TestWorld";
    public static string TestSceneFolder = @"Assets/Scenes/Test";
    private string scenePath = @"Scenes/Test/TestScene";
    private bool sceneLoaded;

    public void Setup()
    {
        TestSceneBuildManager.AddTestScenesToBuildSettings(TestSceneFolder);
    }

    [UnitySetUp]
    public IEnumerator UnitySetup()
    {
        GameManager.CurrentWorldName = TestWorld;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);

        yield return new WaitWhile(() => sceneLoaded == false);
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        this.sceneLoaded = true;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        yield return SceneManager.UnloadSceneAsync(scenePath);
    }

    public void Cleanup()
    {
        TestSceneBuildManager.RemoveTestScenesFromBuildSettings(TestSceneFolder);
    }

    #endregion

    [UnityTest]
    public IEnumerator Hero_HiredInCapitol_Pass()
    {
        // Assign            
        var player1 = Game.Current.GetCurrentPlayer();
        var armies = player1.GetArmies();
        var hero = armies[0] as Hero;

        // Act
        yield return new WaitForSeconds(0.1f);


        // Assert
        Assert.AreEqual("Marthos", player1.Capitol.ShortName);
        Assert.AreEqual("Sirians", player1.Capitol.Clan.ShortName);
        Assert.AreEqual(1, armies.Count);
        Assert.IsNotNull(hero);
        Assert.AreEqual("Marthos", hero.Tile.City.ShortName);
    }


    [UnityTest]
    public IEnumerator Hero_MoveNorth_Pass()
    {
        // Assign
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.1f;

        // Act
        gameManager.SelectArmies(armies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);
        gameManager.MoveSelectedArmies(hero.X, hero.Y + 1);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.AreEqual(originalX, hero.X, "Hero moved East/West.");
        Assert.AreEqual(originalY + 1, hero.Y, "Hero failed to move North.");
    }

    [UnityTest]
    public IEnumerator Hero_MoveSouth_Pass()
    {
        // Assign
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.1f;

        // Act
        gameManager.SelectArmies(armies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(hero.X, hero.Y - 1);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.AreEqual(originalX, hero.X, "Hero moved East/West.");
        Assert.AreEqual(originalY - 1, hero.Y, "Hero failed to move South.");
    }

    [UnityTest]
    public IEnumerator Hero_MoveEast_Pass()
    {
        // Assign
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.1f;

        // Act
        gameManager.SelectArmies(armies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(hero.X + 1, hero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.AreEqual(originalX + 1, hero.X, "Hero failed to move East.");
        Assert.AreEqual(originalY, hero.Y, "Hero moved North/South.");
    }

    [UnityTest]
    public IEnumerator Hero_MoveWest_Pass()
    {
        // Assign
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.1f;

        // Act
        gameManager.SelectArmies(armies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(hero.X - 1, hero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.AreEqual(originalX - 1, hero.X, "Hero failed to move West.");
        Assert.AreEqual(originalY, hero.Y, "Hero moved North/South.");
    }

    [UnityTest]
    public IEnumerator Hero_Attack_CaptureCity()
    {
        // Assign
        var siriansPlayer = Game.Current.Players[0];
        var siriansHero = FindFirstHero(siriansPlayer);
        siriansHero.Strength = 9;

        var banesPlayer = Game.Current.Players[1];
        var banesHero = FindFirstHero(banesPlayer);
        var banesCitadel = banesPlayer.Capitol;
        int originalY = siriansHero.Y;
        var armies = new List<Army>() { siriansHero };

        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.25f;
        gameManager.WarTime = 0.25f;

        // Act
        gameManager.SelectArmies(armies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(banesCitadel.X - 1, siriansHero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.AttackWithSelectedArmies(banesCitadel.X, siriansHero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.AreEqual(banesCitadel.X, siriansHero.X, "Hero failed to move to Bane's Citadel.");
        Assert.AreEqual(originalY, siriansHero.Y, "Hero moved North/South.");
        Assert.IsTrue(banesHero.IsDead, "Lord Bane's hero was not killed.");
        Assert.IsFalse(siriansHero.IsDead, "Sirians' hero died.");
        Assert.AreEqual(2, siriansPlayer.GetCities().Count, "Sirians did not capture Bane's Citadel");
        Assert.AreEqual("Sirians", banesCitadel.Clan.ShortName, "Sirians did not capture Bane's Citadel");
    }

    [UnityTest]
    public IEnumerator Hero_Attack_PlayerLoses()
    {
        // Assign
        var siriansPlayer = Game.Current.Players[0];
        var siriansHero = FindFirstHero(siriansPlayer);
        siriansHero.Strength = 9;
        var siriansArmies = new List<Army>() { siriansHero };

        var banesPlayer = Game.Current.Players[1];
        var banesHero = FindFirstHero(banesPlayer);
        banesHero.Strength = 3;
        var banesArmies = new List<Army>() { banesHero };
        var banesCitadel = banesPlayer.Capitol;

        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.25f;
        gameManager.WarTime = 0.25f;

        // Act 1 : Move Sirians' hero to Banes Citadel
        gameManager.SelectArmies(siriansArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(banesCitadel.X - 1, siriansHero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.EndTurn();
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Act 2: Attack Sirians' hero 
        gameManager.SelectArmies(banesArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.AttackWithSelectedArmies(siriansHero.X, siriansHero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.IsTrue(banesHero.IsDead, "Lord Bane's hero was not killed.");
        Assert.IsFalse(siriansHero.IsDead, "Sirians' hero was killed");        
    }

    [UnityTest]
    public IEnumerator Hero_SearchRuins_StaffOfMight()
    {
        // Assign
        var siriansPlayer = Game.Current.Players[0];
        var siriansHero = FindFirstHero(siriansPlayer);
        var siriansArmies = new List<Army>() { siriansHero };

        var ruins = World.Current.GetLocations().Find(l => l.ShortName == "CryptKeeper");

        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.25f;

        // Act 1 : Move Sirians' hero to Crypt Keeper's ruins
        gameManager.SelectArmies(siriansArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(ruins.X, ruins.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.SearchLocation();
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        if (siriansHero.Tile.HasItems())
        {
            gameManager.TakeItems(siriansHero, siriansHero.Tile.Items);
            yield return new WaitForLastCommand(gameManager.ControllerProvider);
        }
        
        // Assert
        Assert.IsTrue(ruins.Searched, "Ruins have not been searched");
        Assert.IsFalse(siriansHero.IsDead, "Sirians' hero was killed");
        Assert.IsFalse(siriansHero.Tile.HasItems(), "Hero did not pick up the Staff of Might");
        Assert.IsTrue(siriansHero.HasItems(), "Hero did not pick up the Staff of Might");
        Assert.AreEqual("StaffOfMight", ((Artifact)siriansHero.Items[0]).ShortName, "Hero did not pick up the Staff of Might");
    }

    #region Helper methods

    private Hero FindFirstHero(Player player)
    {
        if (player is null)
        {
            throw new ArgumentNullException(nameof(player));
        }
;
        var armies = player.GetArmies();
        foreach (Army army in armies)
        {
            if (army is Hero)
            {
                return (Hero)army;
            }
        }

        return null;
    }

    #endregion
}