﻿using Assets.Scripts.Managers;
using Assets.Scripts.Tests.PlayMode.Common;
using Assets.Scripts.UnityGame.Persistance.Entities;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

public class CityActionTests : IPrebuildSetup, IPostBuildCleanup
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
        SceneManager.LoadScene(this.scenePath, LoadSceneMode.Additive);

        yield return new WaitWhile(() => this.sceneLoaded == false);
        yield return new WaitForSeconds(2f);
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        SceneManager.UnloadSceneAsync(this.scenePath);

        yield return new WaitWhile(() => this.sceneLoaded == true);
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
    public IEnumerator City_ProduceLightInfantry_2Armies()
    {
        // Assign
        var siriansPlayer = Game.Current.Players[0];
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lowenbrau");
        var siriansHero = FindFirstHero(siriansPlayer);
        var marthos = siriansPlayer.Capitol;

        // Dismiss production panel
        yield return new WaitForInteractivePanel(
            UnityUtilities.GameObjectHardFind("CityProductionPanel"));
        yield return WismTestAction.DismissProductionPanel();

        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.1f;

        // Act 1: Sirians produce an army
        gameManager.StartProduction(marthos, ModFactory.FindArmyInfo("LightInfantry"));
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.EndTurn();

        // Act 2: Bane do nothing
        yield return WismTestAction.WaitForNewHeroOffer();
        yield return WismTestAction.AcceptNewHeroOffer("Lord Bane");

        // Dismiss production panel
        yield return new WaitForInteractivePanel(
            UnityUtilities.GameObjectHardFind("CityProductionPanel"));
        yield return WismTestAction.DismissProductionPanel();

        gameManager.EndTurn();
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Act 3: Sirians produce an army
        gameManager.StartProduction(marthos, ModFactory.FindArmyInfo("LightInfantry"));
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.EndTurn();
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Act 4: Bane do nothing
        gameManager.EndTurn();
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Assert
        Assert.AreEqual(siriansPlayer.Clan.ShortName, Game.Current.GetCurrentPlayer().Clan.ShortName, "Not the Sirian's turn");
        Assert.AreEqual(3, siriansPlayer.GetArmies().Count, "Did not produce two light infantry");
        Assert.AreEqual("Hero", siriansPlayer.GetArmies()[0].ShortName, "First army was not the hero");
        Assert.AreEqual("LightInfantry", siriansPlayer.GetArmies()[1].ShortName, "Did not produce light infantry");
        Assert.AreEqual("LightInfantry", siriansPlayer.GetArmies()[2].ShortName, "Did not produce light infantry");
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