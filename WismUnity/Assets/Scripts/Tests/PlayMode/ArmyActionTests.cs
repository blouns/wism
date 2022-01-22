using Assets.Scripts.Managers;
using Assets.Scripts.Tests.PlayMode.Common;
using Assets.Scripts.UnityGame.Persistance.Entities;
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
using Wism.Client.Modules;

public class ArmyActionTests : IPrebuildSetup, IPostBuildCleanup
{
    #region Test scene setup

    public static string TestWorld = "TestWorld";
    public static string TestSceneFolder = @"Assets/Scenes/Test";
    private string scenePath = @"Scenes/Test/TestWorld";
    private bool sceneLoaded;

    public void Setup()
    {
        TestSceneBuildManager.AddTestScenesToBuildSettings(TestSceneFolder);
    }

    [UnitySetUp]
    public IEnumerator UnitySetup()
    {
        UnityNewGameEntity settings = new UnityNewGameEntity()
        {
            InteractiveUI = false,
            IsNewGame = true,
            Players = GetTestPlayers(),
            RandomSeed = 1990,
            RandomStartLocations = false,
            WorldName = TestWorld
        };
        UnityManager.SetNewGameSettings(settings);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);

        yield return new WaitWhile(() => sceneLoaded == false);
        yield return new WaitForSeconds(2f);
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {        
        SceneManager.UnloadSceneAsync(scenePath);

        yield return new WaitWhile(() => sceneLoaded == true);
        Game.Unload();        
    }


    private UnityPlayerEntity[] GetTestPlayers()
    {
        return new UnityPlayerEntity[]
        {
            new UnityPlayerEntity()
            {
                ClanName = "Sirians",
                IsHuman = true
            },
            new UnityPlayerEntity()
            {
                ClanName = "LordBane",
                IsHuman = false
            }
        };
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        this.sceneLoaded = true;
    }

    private void OnSceneUnloaded(Scene arg0)
    {
        this.sceneLoaded = false;
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

        // Act
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var hero = FindFirstHero(player1);

        // Assert
        Assert.AreEqual("Marthos", player1.Capitol.ShortName);
        Assert.AreEqual("Sirians", player1.Capitol.Clan.ShortName);
        Assert.AreEqual(1, player1.GetArmies().Count);
        Assert.IsNotNull(hero);

        if (hero != null)
        {
            Assert.AreEqual("Marthos", hero.Tile.City.ShortName);
            Assert.AreEqual("Lowenbrau", hero.DisplayName, "Lowenbrau has not returned!");
        }
    }

    [UnityTest]
    public IEnumerator Hero_MoveNorth_Pass()
    {
        // Assign
        var inputManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<InputManager>();
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");

        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
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
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
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
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
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
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var hero = FindFirstHero(Game.Current.GetCurrentPlayer());
        int originalX = hero.X;
        int originalY = hero.Y;
        var armies = new List<Army>() { hero };
        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
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
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var siriansHero = FindFirstHero(siriansPlayer);
        siriansHero.Strength = 9;

        var banesPlayer = Game.Current.Players[1];        
        var banesCitadel = banesPlayer.Capitol;
        banesPlayer.HireHero(banesCitadel.Tile);
        var banesHero = FindFirstHero(banesPlayer);
        int originalY = siriansHero.Y;
        var armies = new List<Army>() { siriansHero };

        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
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

        // Get new hero
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");        
        var siriansHero = FindFirstHero(siriansPlayer);
        siriansHero.Strength = 9;
        var siriansArmies = new List<Army>() { siriansHero };

        // Dismiss production panel
        yield return new WaitForInteractivePanel(
            UnityUtilities.GameObjectHardFind("CityProductionPanel"));
        yield return WismTestAction.DismissProductionPanel();

        var banesPlayer = Game.Current.Players[1];
        var banesCitadel = banesPlayer.Capitol;                    

        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.25f;
        gameManager.WarTime = 0.25f;

        // Act 1 : Sirians: Move Sirians' hero to Banes Citadel
        gameManager.SelectArmies(siriansArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(banesCitadel.X - 1, siriansHero.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Set ID for 'last seen command' after the final Sirians command for Lord Bane wait
        int lastId = gameManager.ControllerProvider.CommandController.GetLastCommand().Id + 1;
        gameManager.EndTurn();
        //yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Act 2: Lord Bane: Attack Sirians' hero

        // Get new hero        
        ///////////////////////////////////////////////////////////////////////////////////////
        // TODO: IDEA: Switch WismTestAction to non-static and keep track of lastId internally
        ///////////////////////////////////////////////////////////////////////////////////////
        yield return WismTestAction.WaitForNewHeroOffer(lastId);
        yield return WismTestAction.AcceptNewHeroOffer("Lord Bane", lastId);
        var banesHero = FindFirstHero(banesPlayer);
        banesHero.Strength = 3;
        var banesArmies = new List<Army>() { banesHero };

        // Dismiss production panel
        yield return new WaitForInteractivePanel(
            UnityUtilities.GameObjectHardFind("CityProductionPanel"));
        yield return WismTestAction.DismissProductionPanel();

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
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var siriansHero = FindFirstHero(siriansPlayer);
        var siriansArmies = new List<Army>() { siriansHero };

        // Dismiss production panel
        yield return new WaitForInteractivePanel(
            UnityUtilities.GameObjectHardFind("CityProductionPanel"));
        yield return WismTestAction.DismissProductionPanel();

        // Override boon
        var ruins = World.Current.GetLocations().Find(l => l.ShortName == "CryptKeeper");
        var artifact = Artifact.Create(
            ModFactory.FindArtifactInfo("StaffOfMight"));
        ruins.Boon = new ArtifactBoon(artifact);

        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.25f;

        // Act 1 : Move Sirians' hero to Crypt Keeper's ruins
        gameManager.SelectArmies(siriansArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(ruins.X, ruins.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.SearchLocation();
        var inputManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<InputManager>();
        yield return new WaitForSeconds(2f);

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

    [UnityTest]
    public IEnumerator Hero_PetCompanion_HasCompanion()
    {
        // Assign
        var siriansPlayer = Game.Current.Players[0];
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var siriansHero = FindFirstHero(siriansPlayer);
        var siriansArmies = new List<Army>() { siriansHero };

        // Dismiss production panel
        yield return new WaitForInteractivePanel(
            UnityUtilities.GameObjectHardFind("CityProductionPanel"));
        yield return WismTestAction.DismissProductionPanel();

        // Override boon
        var ruins = World.Current.GetLocations().Find(l => l.ShortName == "CryptKeeper");
        var artifact = Artifact.Create(
            ModFactory.FindArtifactInfo("Cooper"));
        ruins.Boon = new ArtifactBoon(artifact);

        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.25f;

        // Act 1 : Move Sirians' hero to Crypt Keeper's ruins
        gameManager.SelectArmies(siriansArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.MoveSelectedArmies(ruins.X, ruins.Y);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.SearchLocation();
        var inputManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<InputManager>();
        yield return new WaitForSeconds(10f);

        if (siriansHero.Tile.HasItems())
        {
            gameManager.TakeItems(siriansHero, siriansHero.Tile.Items);
            yield return new WaitForLastCommand(gameManager.ControllerProvider);
        }

        gameManager.SelectArmies(siriansArmies);
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        inputManager.UnityManager.HandlePetCompanion();
        yield return new WaitForSeconds(10f);

        // Assert
        Assert.IsTrue(ruins.Searched, "Ruins have not been searched");
        Assert.IsFalse(siriansHero.IsDead, "Sirians' hero was killed");
        Assert.IsFalse(siriansHero.Tile.HasItems(), "Hero did not find Cooper");
        Assert.IsTrue(siriansHero.HasItems(), "Hero did not find Cooper");
        Assert.AreEqual("Cooper", ((Artifact)siriansHero.Items[0]).ShortName, "Hero did not find Cooper");        
        Assert.AreEqual("Cooper layed down and got fur everywhere.",
            siriansHero.GetCompanionInteraction(), "Cooper did not have the expected interaction.");
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