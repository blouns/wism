using Assets.Scripts.Managers;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
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
        if (playerToggles == null || playerToggles.Length == 0)
        {
            throw new InvalidOperationException("Must have at least one player.");
        }

        // Default to all players
        for (int i = 0; i < playerToggles.Length; i++)
        {
            playerToggles[i].isOn = true;
            playerToggles[i].interactable = true;
        }

        // Default world
        this.worldName = GetWorldNameFromPanel();
    }

    public void LoadButton()
    {
        if (String.IsNullOrEmpty(this.worldName))
        {
            // TODO: Notify user?
            return;
        }

        LoadGame(this.worldName);
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

    private void LoadGame(string worldName)
    {
        throw new NotImplementedException();
    }

    private void StartNewGame(UnityNewGameEntity settings)
    {
        UnityManager.SetNewGameSettings(settings);
        LoadScene(settings.WorldName);
    }

    private void LoadScene(string worldName)
    {
        SceneManager.LoadScene(nextScene);
        SceneManager.UnloadSceneAsync(0);
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

        // TODO: Load Mod cities for world and compare to number of players
        //if (settings.World.Cities.Length < settings.Players.Length)
        //{
        //    Debug.LogError("Must have at least enough cities for each player.");
        //    return false;
        //}

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
        var playerEntities = new UnityPlayerEntity[playerToggles.Length];
        for (int i = 0; i < playerEntities.Length; i++)
        {
            playerEntities[i] = new UnityPlayerEntity();
            playerEntities[i].IsHuman = true;
            playerEntities[i].ClanName = GetClanName(i);
        }

        return playerEntities;
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
