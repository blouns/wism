using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Wism.Client.Modules;
using Toggle = UnityEngine.UI.Toggle;

public class GameSetup : MonoBehaviour
{
    [SerializeField]
    private Toggle[] playerToggles;
    [SerializeField]
    private int nextScene;

    private string worldName;

    public void Start()
    {
        if (this.playerToggles == null || this.playerToggles.Length == 0)
        {
            throw new InvalidOperationException("Must have at least one player.");
        }

        // Default to all players
        for (int i = 0; i < this.playerToggles.Length; i++)
        {
            this.playerToggles[i].isOn = true;
            this.playerToggles[i].interactable = true;
        }

        // Default world
        this.worldName = GetWorldNameFromPanel();
    }

    public void LoadButton()
    {
        LoadGame();
    }

    public void StartButton()
    {
        UnityNewGameEntity settings = GetGameSettings();

        if (!AreValidGameSettings(settings))
        {
            // TODO: Notify user?
            return;
        }

        StartNewGame(settings);
    }

    public void OnWorldChange()
    {
        this.worldName = GetWorldNameFromPanel();
    }

    private void LoadGame()
    {
        UnityNewGameEntity settings = new UnityNewGameEntity();
        settings.IsNewGame = false;
        UnityManager.SetNewGameSettings(settings);

        LoadScene(this.worldName);
    }

    private void StartNewGame(UnityNewGameEntity settings)
    {
        UnityManager.SetNewGameSettings(settings);

        LoadScene(settings.WorldName);
    }

    private void LoadScene(string worldName)
    {
        string scenePath = "Scenes/";

#if DEBUG
        if (worldName.Contains("Test"))
        {
            scenePath += "Test/";
        }
#endif

        SceneManager.LoadScene(scenePath + worldName);
        SceneManager.UnloadSceneAsync(1);
    }

    private bool AreValidGameSettings(UnityNewGameEntity settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings.Players.Length < 2)
        {
            Debug.LogError("Must have at least two players to start a new game.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.WorldName))
        {
            Debug.LogError("World name cannot be null.");
            return false;
        }

        // Load Mod cities for world and compare to number of players
        // Must have enough cities for all the players
        IList<CityInfo> cityInfos = null;
        try
        {
            cityInfos = ModFactory.LoadCityInfos(
            @$"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{settings.WorldName}");
        }
        catch
        {
            Debug.LogError("Could not load the world: " + settings.WorldName);
        }

        if (cityInfos != null && cityInfos.Count < settings.Players.Length)
        {
            Debug.LogError("Must have at least enough cities for each player.");
            return false;
        }

        return true;
    }

    private UnityNewGameEntity GetGameSettings()
    {
        UnityNewGameEntity settings = new UnityNewGameEntity();
        settings.Players = GetSelectedPlayersFromPanel();
        settings.WorldName = this.worldName;
        settings.RandomStartLocations = false;
        settings.InteractiveUI = true;
        settings.IsNewGame = true;

        return settings;
    }

    private UnityPlayerEntity[] GetSelectedPlayersFromPanel()
    {
        var playerEntities = new List<UnityPlayerEntity>();
        for (int i = 0; i < this.playerToggles.Length; i++)
        {
            if (this.playerToggles[i].isOn)
            {
                var playerEntity = new UnityPlayerEntity();
                playerEntity.IsHuman = true;
                playerEntity.ClanName = GetClanName(i);
                playerEntities.Add(playerEntity);
            }
        }

        return playerEntities.ToArray();
    }

    private string GetClanName(int i)
    {
        // TODO: This will come from a mod; for now hardcode
        string[] clans =
        {
            "Sirians",
            "StormGiants",
            "GreyDwarves",
            "OrcsOfKor",
            "Elvallie",
            "Selentines",
            "HorseLords",
            "LordBane"
        };

        return clans[i];
    }

    private static string GetWorldNameFromPanel()
    {
        var dropdown = GameObject.Find("WorldDropdown")
            .GetComponent<Dropdown>();
        var index = dropdown.value;
        return dropdown.options[index].text;
    }

    private void SetWorldName(string worldName)
    {
        if (string.IsNullOrWhiteSpace(worldName))
        {
            throw new ArgumentException($"'{nameof(worldName)}' cannot be null or whitespace.", nameof(worldName));
        }

        this.worldName = GetWorldNameFromPanel();
    }
}
