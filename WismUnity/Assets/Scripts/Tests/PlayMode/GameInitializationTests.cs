using Assets.Scripts.Managers;
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

    #region Tests

    [UnityTest]
    public IEnumerator LoadTestScenePasses()
    {
        // Assign
        var unityManagerObject = UnityUtilities.GameObjectHardFind("UnityManager");

        // Act
        yield return new WaitForSeconds(0.1f);        

        // Assert
        Assert.IsNotNull(unityManagerObject, "Could not find the UnityManager");
    }
    #endregion
}
