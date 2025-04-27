using Assets.Scripts.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;

namespace Assets.Scripts.UI
{
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

        public void LateUpdate()
        {
            if (this.armySelectedIndex > 0)
            {
                this.armyButtons[this.armySelectedIndex].Select();
            }
        }

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
            this.armyManager = unityManager.GetComponent<ArmyManager>();

            InitializeProduction();
        }

        private void InitializeProduction()
        {
            SetInitialButtonState();

            var barracks = this.productionCity.Barracks;

            // Unpack the army infos for each production slot
            this.productionInfos = barracks.GetProductionKinds().ToArray();
            for (int i = 0; i < this.productionInfos.Length; i++)
            {
                InitializeProductionSlot(i);
            }

            InitializeCurrentProduction();
            this.unityManager.InputManager.SetInputMode(InputMode.UI);
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
                this.transform.Find("CurrentArmyKind").gameObject.SetActive(true);

                turnsRemainingString = barracks.ArmyInTraining.TurnsToProduce + "t";
            }
            else
            {
                this.transform.Find("CurrentArmyKind").gameObject.SetActive(false);
            }

            // Set turns remaining text
            var turnsText = this.gameObject.transform.Find("TurnsRemainingText").GetComponent<Text>();
            turnsText.text = turnsRemainingString;
        }

        private void SetArmyImageOnGameObject(Clan clan, ArmyInfo info, string gameObjectName)
        {
            var armyPrefab = this.armyManager.FindGameObjectKind(clan, info);
            SpriteRenderer spriteRenderer = armyPrefab.GetComponent<SpriteRenderer>();
            var image = this.gameObject.transform.Find(gameObjectName).GetComponent<Image>();
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
            ArmyInfo armyInfo = ModFactory.FindArmyInfo(this.productionInfos[index].ArmyInfoName);

            // Set image
            var clan = Game.Current.GetCurrentPlayer().Clan;
            var armyPrefab = this.armyManager.FindGameObjectKind(clan, armyInfo);
            SpriteRenderer spriteRenderer = armyPrefab.GetComponent<SpriteRenderer>();
            var image = this.armyButtons[index].gameObject.transform.Find("ArmyKind")
                .GetComponent<Image>();
            image.sprite = spriteRenderer.sprite;

            // Set production info
            Text productionText = this.armyButtons[index].gameObject.transform.Find("ArmyInfo")
                .GetComponent<Text>();
            productionText.text = $"{this.productionInfos[index].TurnsToProduce}t / {this.productionInfos[index].Upkeep}gp";

            this.armyButtons[index].gameObject.SetActive(true);
        }

        private void ClearProduction()
        {
            for (int i = 0; i < this.armyButtons.Length; i++)
            {
                this.armyButtons[i].gameObject.SetActive(false);
            }
        }

        public void OnArmy1Click()
        {
            this.armySelectedIndex = 0;
            EnableProduction();
        }

        public void OnArmy2Click()
        {
            this.armySelectedIndex = 1;
            EnableProduction();
        }

        public void OnArmy3Click()
        {
            this.armySelectedIndex = 2;
            EnableProduction();
        }

        public void OnArmy4Click()
        {
            this.armySelectedIndex = 3;
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
            var armyName = this.productionInfos[this.armySelectedIndex].ArmyInfoName;
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
            this.armySelectedIndex = -1;
            this.unityManager.InputManager.SetInputMode(InputMode.Game);
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
}