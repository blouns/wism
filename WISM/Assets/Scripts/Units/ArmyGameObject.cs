using BranallyGames.Wism;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmyGameObject : MonoBehaviour
{
    public Army army;

    // Update is called once per frame
    void Update()
    {
        if (army != null)
        {
            if (army.Affiliation.IsHuman)
            {
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    bool moved = army.TryMove(Direction.South);
                    if (moved)
                    {
                        Debug.Log(army.DisplayName + ": Moved south");
                    }
                    else
                    {
                        Debug.Log(army.DisplayName + ": Moved blocked");
                    }
                }
            }
        }
    }
}
