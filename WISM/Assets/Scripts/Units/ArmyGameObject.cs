using BranallyGames.Wism;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmyGameObject
{
    private Army army;
    private GameObject gameObject;
    private bool isAlive;

    public ArmyGameObject(Army army, GameObject gameObject)
    {
        this.army = army ?? throw new System.ArgumentNullException(nameof(army));
        this.gameObject = gameObject ?? throw new System.ArgumentNullException(nameof(gameObject));
    }

    public GameObject GameObject { get => gameObject; set => gameObject = value; }
    public Army Army { get => army; set => army = value; }
}