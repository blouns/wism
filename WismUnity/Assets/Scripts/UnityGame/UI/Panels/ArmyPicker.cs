using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class ArmyPicker : MonoBehaviour
    {
        public GameObject[] ArmyRows;

        private UnityManager unityGame;
        private GameManager gameManager;
        private ArmyManager armyManager;
        private List<Army> armies;

        private bool[] selected = new bool[GameManager.MaxArmies];
        [SerializeField]
        private Sprite SelectedSprite;
        [SerializeField]
        private Sprite UnselectedSprite;

        public void OkButton()
        {
            List<Army> selectedArmys = GetSelectedArmysFromPanel();
            if (selectedArmys.Count > 0)
            {
                this.gameManager.SelectArmies(selectedArmys);
            }
            else
            {
                this.gameManager.DeselectArmies();
            }

            Teardown();
        }

        private List<Army> GetSelectedArmysFromPanel()
        {
            List<Army> selectedArmys = new List<Army>();
            for (int i = 0; i < this.selected.Length; i++)
            {
                if (this.selected[i])
                {
                    selectedArmys.Add(this.armies[i]);
                }
            }

            return selectedArmys;
        }

        public void ToggleSelected(int index)
        {
            this.selected[index] = !this.selected[index];

            Button button = GetButton(index);
            ToggleImage(button.GetComponent<Button>(), this.selected[index]);
        }

        private Button GetButton(int index)
        {
            Button[] buttons = GameObject.FindObjectsOfType<Button>();
            foreach (Button button in buttons)
            {
                GameObject row = button.transform.parent.gameObject;
                if (row.name == $"ArmyRow ({index})")
                {
                    return button;
                }
            }

            throw new ArgumentOutOfRangeException("Could not find the button matching the index.");
        }

        public void Initialize(UnityManager unityGame, List<Army> armies)
        {
            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }

            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            this.unityGame = unityGame;
            this.gameManager = unityGame.GameManager;
            this.armyManager = unityGame.GetComponent<ArmyManager>();

            this.armies = new List<Army>();
            this.armies.AddRange(armies);

            RenderArmyRows(armies);

            this.unityGame.InputManager.SetInputMode(InputMode.UI);
            this.gameObject.SetActive(true);
        }

        private void RenderArmyRows(List<Army> armies)
        {
            if (armies is null)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            for (int i = 0; i < GameManager.MaxArmies; i++)
            {
                // Hide rows with no armies
                if (i >= armies.Count)
                {
                    this.ArmyRows[i].SetActive(false);
                    this.selected[i] = false;
                    continue;
                }

                // Render rows with correct army info
                foreach (Transform transform in this.ArmyRows[i].transform)
                {
                    // Select by default if currently selected in the Game
                    var currentlySelectedArmies = Game.Current.GetSelectedArmies();
                    this.selected[i] = currentlySelectedArmies.Contains(armies[i]);

                    GameObject widget = transform.gameObject;
                    if (widget.name == "NameDisplay")
                    {
                        widget.GetComponent<Text>().text = armies[i].DisplayName;
                        widget.GetComponent<Text>().text =
                            (armies[i] is Hero) ? armies[i].DisplayName : armies[i].KindName;
                    }
                    else if (widget.name == "BonusDisplay")
                    {
                        widget.GetComponent<Text>().text =
                            (armies[i] is Hero) ? ((Hero)armies[i]).GetCombatBonus().ToString() : "-";
                    }
                    else if (widget.name == "MoveDisplay")
                    {
                        widget.GetComponent<Text>().text = armies[i].MovesRemaining.ToString();
                    }
                    else if (widget.name == "StrengthDisplay")
                    {
                        widget.GetComponent<Text>().text = armies[i].Strength.ToString();
                    }
                    else if (widget.name == "SelectButton")
                    {
                        Button button = widget.GetComponent<Button>();
                        button.image.sprite =
                            (this.selected[i]) ? this.SelectedSprite : this.UnselectedSprite;
                    }
                    else if (widget.name == "Army")
                    {
                        ReplaceImage(armies[i], widget);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown widget type in the row.");
                    }
                }

                this.ArmyRows[i].SetActive(true);
            }

        }

        static public GameObject GetChildGameObject(GameObject fromGameObject, string withName)
        {
            Transform[] transforms = fromGameObject.transform.GetComponentsInChildren<Transform>();
            foreach (Transform t in transforms)
            {
                if (t.gameObject.name == withName)
                {
                    return t.gameObject;
                }
            }

            return null;
        }

        private void ReplaceImage(Army army, GameObject armyGo)
        {
            GameObject armyKind = this.armyManager.FindGameObjectKind(army);
            SpriteRenderer spriteRenderer = armyKind.GetComponent<SpriteRenderer>();
            Image image = armyGo.GetComponent<Image>();
            image.sprite = spriteRenderer.sprite;
        }

        private void ToggleImage(Button button, bool isSelected)
        {
            button.image.sprite = (isSelected) ? this.SelectedSprite : this.UnselectedSprite;
        }

        public void Teardown()
        {
            this.unityGame.InputManager.SetInputMode(InputMode.Game);
            this.gameObject.SetActive(false);
        }
    }
}