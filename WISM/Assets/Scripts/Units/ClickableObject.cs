using BranallyGames.Wism;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    private WorldTimemap tileMap;
    private ArmyGameObject armyGameObject;

    public WorldTimemap TileMap { get => tileMap; set => tileMap = value; }
    public ArmyGameObject ArmyGameObject { get => armyGameObject; set => armyGameObject = value; }

    public void OnMouseUpAsButton()
    {
        //if (ArmyGameObject == null)
        //{
        //    throw new InvalidOperationException("No ArmyGameObject assigned to the ClickableObject.");
        //}

        //if (tileMap == null)
        //{
        //    throw new InvalidOperationException("No TileMap assigned to the ClickableObject.");
        //}

        //Debug.Log("Selected: " + ArmyGameObject.Army.DisplayName);
        //Transition();
    }    

    private void Transition()
    {
        switch (this.TileMap.SelectedState)
        {
            case SelectedState.Unselected:
                this.TileMap.SelectedArmy = this.ArmyGameObject;
                this.TileMap.SelectedState = SelectedState.Selected;
                Debug.Log("Selected army: " + this.ArmyGameObject.Army.DisplayName);
                break;
            case SelectedState.Selected:
                //this.TileMap.MoveSelectedUnitTo(this.)
                break;
            case SelectedState.Moving:
                // Do nothing
                break;
            default:
                throw new InvalidOperationException("Transitioning click handler from unknown state.");
        }
    }

    public enum SelectedState
    {
        Unselected = 0,
        Selected,
        Moving
    }
}
