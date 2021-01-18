using Assets.Scripts.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

public class CityProduction : MonoBehaviour
{
    [SerializeField]
    private Button[] armyButtons;    

    [SerializeField] 
    private Button prodButton;
    [SerializeField] 
    private Button locButton;
    [SerializeField] 
    private Button stopButton;
    [SerializeField]
    private Button exitButton;

    
    private UnityManager unityManager;
    private int armySelectedIndex;
    private City productionCity;
    private ProductionInfo[] productionInfos;
    
    public void Initialize(UnityManager unityManager, City city)
    {
        if (unityManager is null)
        {
            throw new System.ArgumentNullException(nameof(unityManager));
        }

        if (city is null)
        {
            throw new System.ArgumentNullException(nameof(city));
        }

        this.productionCity = city;
        this.unityManager = unityManager;

        InitializeProduction();
    }

    private void InitializeProduction()
    {
        SetInitialButtonState();

        var barracks = productionCity.Barracks;
        this.productionInfos = barracks.GetProductionKinds().ToArray();

        // Unpack the army infos for each production slot
        for (int i = 0; i < productionInfos.Length; i++)
        {
            InitializeProductionSlot(i);
        }
    }

    private void SetInitialButtonState()
    {
        this.prodButton.interactable = false;
        this.locButton.interactable = false;
        this.stopButton.interactable = false;
        this.exitButton.interactable = true;
        ClearProduction();
    }

    private void InitializeProductionSlot(int index)
    {
        ArmyInfo armyInfo = ModFactory.FindArmyInfo(productionInfos[index].ArmyInfoName);
        Debug.Log($"({index + 1}) " +
            $"{armyInfo.DisplayName}\t" +
            $"Strength: {armyInfo.Strength + productionInfos[index].StrengthModifier}\t" +
            $"Moves: {armyInfo.Moves + productionInfos[index].MovesModifier}\t" +
            $"Turns: {productionInfos[index].TurnsToProduce}\t" +
            $"Upkeep: {productionInfos[index].Upkeep}");

        ArmyManager armyManager = GameObject.FindGameObjectWithTag("ArmyManager")
            .GetComponent<ArmyManager>();

        // Set image
        var clan = Game.Current.GetCurrentPlayer().Clan;
        var armyPrefab = armyManager.FindGameObjectKind(clan, armyInfo);                
        var image = gameObject.GetComponentInChildren<Image>();
        image.sprite = armyPrefab.GetComponent<Sprite>();

        // Set production info
        Text productionText = armyButtons[index].gameObject.transform.Find("ArmyKind")
            .GetComponent<Text>();
        productionText.text = $"{productionInfos[index].TurnsToProduce}t / {productionInfos[index].Upkeep}gp";
    }

    private void ClearProduction()
    {
        for (int i = 0; i < armyButtons.Length; i++)
        {
            this.armyButtons[i].gameObject.SetActive(false);
        }
    }

    public void OnArmy1Click()
    {
        armySelectedIndex = 0;
        EnableProduction();
    }

    public void OnArmy2Click()
    {
        armySelectedIndex = 1;
        EnableProduction();
    }

    public void OnArmy3Click()
    {
        armySelectedIndex = 2;
        EnableProduction();
    }

    public void OnArmy4Click()
    {
        armySelectedIndex = 3;
        EnableProduction();
    }

    public void OnProdClick()
    {
        StartProduction();
    }

    private void StartProduction(City destinationCity = null)
    {
        var armyName = productionInfos[armySelectedIndex].ArmyInfoName;
        var armyInfo = ModFactory.FindArmyInfo(armyName);

        Debug.Log($"Starting production of {armyInfo.DisplayName}" +
            $" on {this.productionCity}" +
            $" to {(destinationCity == null ? this.productionCity : destinationCity)}");
        this.unityManager.GameManager
            .StartProduction(this.productionCity, armyInfo, destinationCity);
    }

    public void OnLocClick()
    {
        // TODO: Need a way to pick the destination city
        City destinationCity = null;

        StartProduction(destinationCity);
    }

    public void OnStopClick()
    {
        Debug.Log($"Stopping production on {this.productionCity}");
        this.unityManager.GameManager
            .StopProduction(this.productionCity);

        DisableProduction();
    }

    public void OnExitClick()
    {
        this.gameObject.SetActive(false);
    }

    private void EnableProduction()
    {
        this.prodButton.interactable = true;

        if (Game.Current.GetCurrentPlayer()
            .GetCities().Count > 1)
        {
            this.locButton.interactable = true;
        }
    }

    private void DisableProduction()
    {
        this.prodButton.interactable = false;
        this.locButton.interactable = false;
    }
}
