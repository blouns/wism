using Assets.Scripts.Units;
using Assets.Scripts.Wism;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;

public class ArmyPicker : MonoBehaviour
{
    public GameObject[] ArmyRows;

    private UnityGame unityGame;
    private GameManager gameManager;
    private ArmyFactory armyFactory;
    private List<Army> armies;
        
    private bool[] selected = new bool[GameManager.MaxArmysPerArmy];
    [SerializeField]
    private Sprite SelectedSprite;
    [SerializeField]
    private Sprite UnselectedSprite;

    public void OkButton()
    {
        List<Army> selectedArmys = GetSelectedArmysFromPanel();
        if (selectedArmys.Count > 0)
        {
            gameManager.SelectArmies(selectedArmys);
        }
        else
        {
            gameManager.DeselectArmies();
        }

        Teardown();
    }

    private List<Army> GetSelectedArmysFromPanel()
    {
        List<Army> selectedArmys = new List<Army>();
        for (int i = 0; i < this.selected.Length; i++)
        {
            if (selected[i])
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

    public void Initialize(UnityGame unityGame, List<Army> armies, ArmyFactory armyFactory)
    {
        if (unityGame is null)
        {
            throw new ArgumentNullException(nameof(unityGame));
        }

        if (armies is null)
        {
            throw new ArgumentNullException(nameof(armies));
        }

        if (armyFactory is null)
        {
            throw new ArgumentNullException(nameof(armyFactory));
        }

        this.unityGame = unityGame;
        this.gameManager = unityGame.GameManager;
        this.armyFactory = armyFactory;

        this.armies = new List<Army>();        
        this.armies.AddRange(armies);

        RenderArmyRows(armies);

        this.unityGame.SelectingArmies = true;
        this.gameObject.SetActive(true);
    }

    private void RenderArmyRows(List<Army> armies)
    {
        if (armies is null)
        {
            throw new ArgumentNullException(nameof(armies));
        }

        for (int i = 0; i < GameManager.MaxArmysPerArmy; i++)
        {
            // Hide rows with no armies
            if (i >= armies.Count)
            {
                ArmyRows[i].SetActive(false);
                selected[i] = false;
                continue;
            }

            // Render rows with correct army info
            foreach (Transform transform in ArmyRows[i].transform)
            {
                // Select by default if currently selected in the Game
                var currentlySelectedArmies = Game.Current.GetSelectedArmies();
                this.selected[i] = currentlySelectedArmies.Contains(armies[i]);

                GameObject widget = transform.gameObject;
                if (widget.name == "NameDisplay")
                {
                    widget.GetComponent<Text>().text = armies[i].DisplayName;
                }
                else if (widget.name == "BonusDisplay")
                {
                    string bonusText = "-";
                    if (armies[i] is Hero)
                    {
                        bonusText = ((Hero)armies[i]).GetCombatBonus().ToString();
                    }
                    widget.GetComponent<Text>().text = bonusText;
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
                    button.image.sprite = (this.selected[i]) ? this.SelectedSprite : this.UnselectedSprite;
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

            ArmyRows[i].SetActive(true);
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
        GameObject armyKind = armyFactory.FindGameObjectKind(army);
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
        this.unityGame.SelectingArmies = false;
        this.gameObject.SetActive(false);
    }
}
