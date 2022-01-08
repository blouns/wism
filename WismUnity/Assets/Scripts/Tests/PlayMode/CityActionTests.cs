using Assets.Scripts.Managers;
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
    public IEnumerator City_ProduceLightInfantry_2Armies()
    {
        // Assign
        var siriansPlayer = Game.Current.Players[0];
        var siriansHero = FindFirstHero(siriansPlayer);
        var marthos = siriansPlayer.Capitol;       

        var gameManager = UnityUtilities.GameObjectHardFind("UnityManager")
            .GetComponent<GameManager>();
        gameManager.StandardTime = 0.1f;

        // Act 1: Sirians produce an army
        gameManager.StartProduction(marthos, ModFactory.FindArmyInfo("LightInfantry"));
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        gameManager.EndTurn();
        yield return new WaitForLastCommand(gameManager.ControllerProvider);

        // Act 2: Bane do nothing
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