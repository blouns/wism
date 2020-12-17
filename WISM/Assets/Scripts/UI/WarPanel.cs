using Assets.Scripts.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WarPanel : MonoBehaviour
{
    /*
    public GameObject WarPanelGo;
    public GameObject KilledPrefab;
    public GameObject AttackerPrefab;
    public GameObject DefenderPrefab;

    private Dictionary<Unit, GameObject> attackerPanelObjects;
    private Dictionary<Unit, GameObject> defenderPanelObjects;
    private ArmyFactory armyFactory;
    private List<GameObject> armyKinds;

    private Army attacker;
    private Army defender;
    bool attackerWon;
    
    public void Initialize(Army attacker, Army defender, GameObject[] armyKinds)
    {
        if (armyKinds == null)
        {
            throw new ArgumentNullException(nameof(armyKinds));
        }
        this.attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        this.defender = defender ?? throw new ArgumentNullException(nameof(defender));
        
        this.armyFactory = ArmyFactory.Create(armyKinds);

        List<Unit> attackingUnits = attacker.SortByBattleOrder(defender.Tile);
        attackerPanelObjects = CreateUnitPanelObjects(attackingUnits, AttackerPrefab, AttackerPrefab.transform.position);

        List<Unit> defendingUnits = defender.SortByBattleOrder(defender.Tile);
        defenderPanelObjects = CreateUnitPanelObjects(defendingUnits, DefenderPrefab, DefenderPrefab.transform.position);

        this.WarPanelGo.SetActive(true);
    }

    private Dictionary<Unit, GameObject> CreateUnitPanelObjects(List<Unit> units, GameObject prefab, Vector3 position)
    {
        Dictionary<Unit, GameObject> unitObject = new Dictionary<Unit, GameObject>();        
        for (int i = 0; i < units.Count; i++)
        {
            // Create the GO for the panel            
            GameObject unitGo = Instantiate<GameObject>(prefab, position, Quaternion.identity, WarPanelGo.transform);

            // Replace image with army kind
            ReplaceImage(units[i], unitGo);

            // Calculate attacker rendering position based on number of units
            unitGo.transform.position = GetUnitPanelPosition(units, position, i, unitGo);

            unitGo.SetActive(true);
            unitObject.Add(units[i], unitGo);
        }

        return unitObject;
    }

    private Vector3 GetUnitPanelPosition(List<Unit> units, Vector3 position, int index, GameObject unitGo)
    {        
        const float xUnitSize = .5f;
        const float xOffset = -.25f;

        index = units.Count - index; // Reverse the order to draw left-to-right
        float xTotal = xUnitSize * units.Count;
        float xShifted = (xUnitSize * index) - (xTotal / 2) + xOffset;
        return new Vector3(position.x - xShifted, position.y);
    }

    private void ReplaceImage(Unit unit, GameObject unitGo)
    {
        GameObject unitKind = armyFactory.FindGameObjectKind(unit);
        SpriteRenderer spriteRenderer = unitKind.GetComponent<SpriteRenderer>();
        Image image = unitGo.GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;
    }

    public void Teardown()
    {
        this.WarPanelGo.SetActive(false);

        foreach (GameObject go in attackerPanelObjects.Values)
        {
            Destroy(go);
        }

        foreach (GameObject go in defenderPanelObjects.Values)
        {
            Destroy(go);
        }
    }

    public void UpdateBattle(bool didAttackerWin, Unit losingUnit)
    {
        Dictionary<Unit, GameObject> losingUnits = (didAttackerWin) ? defenderPanelObjects : attackerPanelObjects;

        if (losingUnits.Count == 0)
        {
            Debug.LogWarning("WarPanel: losing army had no units remaining.", this);
            return;
        }

        if (!losingUnits.ContainsKey(losingUnit))
        {
            Debug.LogWarningFormat("WarPanel: Losing unit not present in the units collection: Unit: {0}", losingUnit.ToString());
        }
        GameObject losingUnitPanelObject = losingUnits[losingUnit];        
        Vector3 position = losingUnitPanelObject.transform.position;

        // Draw killed sprite over defeated unit
        KilledPrefab.transform.SetPositionAndRotation(position, Quaternion.identity);
        GameObject killedPanelObject = Instantiate(KilledPrefab, position, Quaternion.identity, WarPanelGo.transform);
        killedPanelObject.SetActive(true);
        //AudioClip beep = killedPanelObject.GetComponent<AudioClip>();
        ////AudioSource.PlayClipAtPoint(beep, Camera.main.transform.position);

        // Remove killed sprite and unit from panel after an interval
        Destroy(killedPanelObject, GameManager.WarTime);
        Destroy(losingUnitPanelObject, GameManager.WarTime);
        losingUnits.Remove(losingUnit);
    }
    */
}
