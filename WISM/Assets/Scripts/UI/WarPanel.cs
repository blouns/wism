using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.MapObjects;

public class WarPanel : MonoBehaviour
{
    public GameObject KilledPrefab;
    public GameObject AttackerPrefab;
    public GameObject DefenderPrefab;

    private Dictionary<Army, GameObject> attackerPanelObjects;
    private Dictionary<Army, GameObject> defenderPanelObjects;
    private ArmyFactory armyFactory;

    private int currentAttackerIndex;
    private int currentDefenderIndex;
    
    public void Initialize(List<Army> attackers, List<Army> defenders, GameObject[] armyKinds)
    {
        if (attackers is null)
        {
            throw new ArgumentNullException(nameof(attackers));
        }

        if (defenders is null || defenders.Count == 0)
        {
            throw new ArgumentNullException(nameof(defenders));
        }

        if (armyKinds == null || attackers.Count == 0)
        {
            throw new ArgumentNullException(nameof(armyKinds));
        }
        
        this.armyFactory = ArmyFactory.Create(armyKinds);
        var targetTile = defenders[0].Tile;

        currentAttackerIndex = 0;
        currentDefenderIndex = 0;

        List<Army> attackingArmies = new List<Army>(attackers);
        attackingArmies.Sort(new ByArmyBattleOrder(targetTile));
        attackerPanelObjects = CreateArmyPanelObjects(attackingArmies, AttackerPrefab, AttackerPrefab.transform.position);        

        if (defenders.Count > 0)
        {
            List<Army> defendingArmies = new List<Army>(defenders);
            defendingArmies.Sort(new ByArmyBattleOrder(targetTile));
            defenderPanelObjects = CreateArmyPanelObjects(defendingArmies, DefenderPrefab, DefenderPrefab.transform.position);
        }

        this.gameObject.SetActive(true);
    }

    private Dictionary<Army, GameObject> CreateArmyPanelObjects(List<Army> armies, GameObject prefab, Vector3 position)
    {
        Dictionary<Army, GameObject> armyObject = new Dictionary<Army, GameObject>();        
        for (int i = 0; i < armies.Count; i++)
        {
            // Create the GO for the panel            
            GameObject armyGo = Instantiate<GameObject>(prefab, position, Quaternion.identity, gameObject.transform);

            // Replace image with army kind
            ReplaceImage(armies[i], armyGo);

            // Calculate attacker rendering position based on number of armies
            armyGo.transform.position = GetArmyPanelPosition(armies, position, i, armyGo);

            armyGo.SetActive(true);
            armyObject.Add(armies[i], armyGo);
        }

        return armyObject;
    }

    private Vector3 GetArmyPanelPosition(List<Army> armies, Vector3 position, int index, GameObject armyGo)
    {        
        const float xArmySize = .5f;
        const float xOffset = -.25f;

        index = armies.Count - index; // Reverse the order to draw left-to-right
        float xTotal = xArmySize * armies.Count;
        float xShifted = (xArmySize * index) - (xTotal / 2) + xOffset;
        return new Vector3(position.x - xShifted, position.y);
    }

    private void ReplaceImage(Army army, GameObject armyGo)
    {
        GameObject armyKind = armyFactory.FindGameObjectKind(army);
        SpriteRenderer spriteRenderer = armyKind.GetComponent<SpriteRenderer>();
        Image image = armyGo.GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;
    }

    public void Teardown()
    {
        this.gameObject.SetActive(false);

        foreach (GameObject go in attackerPanelObjects.Values)
        {
            Destroy(go);
        }

        foreach (GameObject go in defenderPanelObjects.Values)
        {
            Destroy(go);
        }
    }

    public void UpdateBattle(List<Army> attackers, List<Army> defenders)
    {
        var attacker = attackers[currentAttackerIndex];
        var defender = (defenders.Count > 0) ? defenders[currentDefenderIndex] : null;
        bool didAttackerWin;
        Army losingArmy;
        
        if (attacker.IsDead)
        {
            didAttackerWin = false;
            losingArmy = attacker;
            currentAttackerIndex++;
        }
        else
        {
            didAttackerWin = true;
            losingArmy = defender;
            currentDefenderIndex++;
        }

        Dictionary<Army, GameObject> losingArmies = (didAttackerWin) ? defenderPanelObjects : attackerPanelObjects;

        if (losingArmies.Count == 0)
        {
            Debug.Log("WarPanel: Losing player had no armies.", this);
            return;
        }

        if (!losingArmies.ContainsKey(losingArmy))
        {
            Debug.LogWarningFormat("WarPanel: Losing army not present in the armies collection: Army: {0}", losingArmy.ToString());
        }
        GameObject losingArmyPanelObject = losingArmies[losingArmy];        
        Vector3 position = losingArmyPanelObject.transform.position;

        // Draw killed sprite over defeated army
        KilledPrefab.transform.SetPositionAndRotation(position, Quaternion.identity);
        GameObject killedPanelObject = Instantiate(KilledPrefab, position, Quaternion.identity, gameObject.transform);
        killedPanelObject.SetActive(true);
        //AudioClip beep = killedPanelObject.GetComponent<AudioClip>();
        ////AudioSource.PlayClipAtPoint(beep, Camera.main.transform.position);

        // Remove killed sprite and army from panel after an interval
        Destroy(killedPanelObject, GameManager.WarTime);
        Destroy(losingArmyPanelObject, GameManager.WarTime);
        losingArmies.Remove(losingArmy);
    }
}
