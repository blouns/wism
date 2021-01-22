using Assets.Scripts.Managers;
using System;
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
    private ArmyManager armyManager;
    private int armySelectedIndex;
    private City productionCity;
    private ProductionInfo[] productionInfos;
    
    public void Initialize(UnityManager unityManager, City city)
    {
        if (unityManager is null)
        {
            throw new ArgumentNullException(nameof(unityManager));
        }

        if (city is null)
        {
            throw new ArgumentNullException(nameof(city));
        }

        this.productionCity = city;
        this.unityManager = unityManager;
        this.armyManager = GameObject.FindGameObjectWithTag("ArmyManager")
                .GetComponent<ArmyManager>();

        InitializeProduction();
    }

    private void InitializeProduction()
    {
        SetInitialButtonState();

        var barracks = productionCity.Barracks;

        // Unpack the army infos for each production slot
        this.productionInfos = barracks.GetProductionKinds().ToArray();
        for (int i = 0; i < productionInfos.Length; i++)
        {
            InitializeProductionSlot(i);
        }

        InitializeCurrentProduction();
        this.unityManager.SetAcceptingInput(false);
    }

    private void InitializeCurrentProduction()
    {
        string turnsRemainingString = "None";

        var barracks = this.productionCity.Barracks;
        if (barracks.ProducingArmy())
        {
            // Set image
            SetArmyImageOnGameObject(
                Game.Current.GetCurrentPlayer().Clan,
                barracks.ArmyInTraining.ArmyInfo,
                "CurrentArmyKind");
            transform.Find("CurrentArmyKind").gameObject.SetActive(true);

            turnsRemainingString = barracks.ArmyInTraining.TurnsToProduce + "t";
        }
        else
        {
            transform.Find("CurrentArmyKind").gameObject.SetActive(false);
        }

        // Set turns remaining text
        var turnsText = gameObject.transform.Find("TurnsRemainingText").GetComponent<Text>();
        turnsText.text = turnsRemainingString;
    }

    private void SetArmyImageOnGameObject(Clan clan, ArmyInfo info, string gameObjectName)
    {  
        var armyPrefab = armyManager.FindGameObjectKind(clan, info);
        SpriteRenderer spriteRenderer = armyPrefab.GetComponent<SpriteRenderer>();
        var image = gameObject.transform.Find(gameObjectName).GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;
    }

    private void SetInitialButtonState()
    {
        this.prodButton.interactable = false;
        this.locButton.interactable = false;
        this.stopButton.interactable = true;
        this.exitButton.interactable = true;
        ClearProduction();
    }

    private void InitializeProductionSlot(int index)
    {
        ArmyInfo armyInfo = ModFactory.FindArmyInfo(productionInfos[index].ArmyInfoName);

        // Set image
        var clan = Game.Current.GetCurrentPlayer().Clan;
        var armyPrefab = armyManager.FindGameObjectKind(clan, armyInfo);
        SpriteRenderer spriteRenderer = armyPrefab.GetComponent<SpriteRenderer>();
        var image = armyButtons[index].gameObject.transform.Find("ArmyKind")
            .GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;

        // Set production info
        Text productionText = armyButtons[index].gameObject.transform.Find("ArmyInfo")
            .GetComponent<Text>();
        productionText.text = $"{productionInfos[index].TurnsToProduce}t / {productionInfos[index].Upkeep}gp";

        this.armyButtons[index].gameObject.SetActive(true);
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

        // Close
        OnExitClick();
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
        this.unityManager.SetAcceptingInput(true);
        this.unityManager.SetProductionMode(ProductionMode.None);
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
