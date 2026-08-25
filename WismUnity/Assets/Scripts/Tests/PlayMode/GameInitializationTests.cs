using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.ModKit;
using Assets.Scripts.UnityGame.Persistance.Entities;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
public class GameInitializationTests : IPrebuildSetup, IPostBuildCleanup
{
    #region Test scene setup
    public static string TestWorld = "TestWorld";
    public static string TestSceneFolder = @"Assets/Scenes/Test";
    private string scenePath = @"Scenes/Test/TestWorld";

    public void Setup()
    {
        TestSceneBuildManager.AddTestScenesToBuildSettings(TestSceneFolder);
    }

    [UnitySetUp]
    public IEnumerator UnitySetup()
    {
        Wism.Client.Core.Game.Unload();
        UnityManager.SetNewGameSettings(null);
        UnityModKitRuntimeSelection.Clear();
        Wism.Client.Modules.ModFactory.ModPath = GameManager.DefaultModPath;
        Wism.Client.Modules.ModFactory.WorldPath = TestWorld;
        Wism.Client.Modules.ModFactory.ActiveFeaturePackIds = new System.Collections.Generic.List<string>();
        Wism.Client.Modules.ModFactory.ResetCache();
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
        SceneManager.LoadScene(this.scenePath, LoadSceneMode.Single);

        yield return new WaitUntil(() =>
            SceneManager.GetActiveScene().name == TestWorld &&
            Wism.Client.Core.Game.IsInitialized());
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

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        var cleanup = SceneManager.CreateScene("GameInitializationCleanup");
        SceneManager.SetActiveScene(cleanup);
        var testScene = SceneManager.GetSceneByName(TestWorld);
        if (testScene.IsValid() && testScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(testScene);
        }
        Wism.Client.Core.Game.Unload();
        UnityManager.SetNewGameSettings(null);
    }

    public void Cleanup()
    {
        TestSceneBuildManager.RemoveTestScenesFromBuildSettings(TestSceneFolder);
    }
    #endregion

    #region Tests

    [UnityTest]
    public IEnumerator LoadTestScenePasses()
    {
        // Assign
        var unityManagerObject = GameObject.FindGameObjectWithTag("UnityManager");

        // Act
        yield return new WaitForSeconds(0.1f);

        // Assert
        Assert.IsNotNull(unityManagerObject, "Could not find the UnityManager");
    }

    [UnityTest]
    public IEnumerator DeselectArmies_NoSelectedArmies_DoesNotThrow()
    {
        var gameManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<GameManager>();

        yield return new WaitForSeconds(0.1f);

        Assert.DoesNotThrow(() => gameManager.DeselectArmies());
    }

    #endregion
}
