using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitPicker : MonoBehaviour
{
    /*
    public GameObject UnitPickerPanel;
    public GameObject[] ArmyRows;
    public WorldTilemap WorldTilemap;

    private ArmyFactory armyFactory;
    private List<GameObject> armyKinds;
    private Army army;
    private bool[] selected = new bool[GameManager.MaxUnitsPerArmy];
    [SerializeField]
    private Sprite SelectedSprite;
    [SerializeField]
    private Sprite UnselectedSprite;

    public void OkButton()
    {
        List<Unit> selectedUnits = GetSelectedUnits();
        if (selectedUnits.Count > 0)
        {
            SetSelectedArmy(selectedUnits);
        }
        else
        {
            this.WorldTilemap.DeselectObject();
        }

        Teardown();
    }

    private void SetSelectedArmy(List<Unit> selectedUnits)
    {
        this.WorldTilemap.SetSelectedArmy(selectedUnits);
    }

    private List<Unit> GetSelectedUnits()
    {
        List<Unit> selectedUnits = new List<Unit>();
        for (int i = 0; i < this.selected.Length; i++)
        {
            if (selected[i])
            {
                selectedUnits.Add(this.army[i]);
            }
        }

        return selectedUnits;
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

    public void Initialize(Army army, GameObject[] armyKinds)
    {
        if (armyKinds is null)
        {
            throw new ArgumentNullException(nameof(armyKinds));
        }

        this.army = army ?? throw new ArgumentNullException(nameof(army));      
        this.armyFactory = ArmyFactory.Create(armyKinds);

        List<Unit> armyUnits = army.SortByBattleOrder(army.Tile);
        RenderArmyRows(armyUnits);

        this.UnitPickerPanel.SetActive(true);
    }

    private void RenderArmyRows(List<Unit> units)
    {
        if (units is null)
        {
            throw new ArgumentNullException(nameof(units));
        }

        for (int i = 0; i < GameManager.MaxUnitsPerArmy; i++)
        {
            // Hide rows with no units
            if (i >= units.Count)
            {
                ArmyRows[i].SetActive(false);
                selected[i] = false;
                continue;
            }

            // Render rows with correct unit info
            foreach (Transform transform in ArmyRows[i].transform)
            {
                GameObject widget = transform.gameObject;
                if (widget.name == "NameDisplay")
                {
                    widget.GetComponent<Text>().text = units[i].DisplayName;
                }
                else if (widget.name == "BonusDisplay")
                {
                    string bonusText = "-";
                    if (units[i] is Hero)
                    {
                        bonusText = ((Hero)units[i]).GetCombatBonus().ToString();
                    }
                    widget.GetComponent<Text>().text = bonusText;
                }
                else if (widget.name == "MoveDisplay")
                {
                    widget.GetComponent<Text>().text = units[i].MovesRemaining.ToString();
                }
                else if (widget.name == "StrengthDisplay")
                {
                    widget.GetComponent<Text>().text = units[i].Strength.ToString();
                }
                else if (widget.name == "SelectButton")
                {
                    Button button = widget.GetComponent<Button>();
                    button.image.sprite = this.SelectedSprite;
                }
                else if (widget.name == "Army")
                {
                    ReplaceImage(units[i], widget);
                }
                else
                {
                    throw new InvalidOperationException("Unknown widget type in the row.");
                }

                this.selected[i] = true;
            }

            ArmyRows[i].SetActive(true);
        }

    }

    static public GameObject GetChildGameObject(GameObject fromGameObject, string withName)
    {
        //Author: Isaac Dart, June-13.
        Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
        return null;
    }

    private void ReplaceImage(Unit unit, GameObject unitGo)
    {
        GameObject unitKind = armyFactory.FindGameObjectKind(unit);
        SpriteRenderer spriteRenderer = unitKind.GetComponent<SpriteRenderer>();
        Image image = unitGo.GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;
    }

    private void ToggleImage(Button button, bool isSelected)
    {
        button.image.sprite = (isSelected) ? this.SelectedSprite : this.UnselectedSprite;
    }

    public void Teardown()
    {
        this.UnitPickerPanel.SetActive(false);
    }
    */
}
