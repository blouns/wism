using Assets.Scripts.Units;
using BranallyGames.Wism;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WarPanel : MonoBehaviour
{
    public GameObject WarPanelGo;
    public GameObject KilledPrefab;
    public GameObject AttackerPrefab;
    public GameObject DefenderPrefab;

    private List<GameObject> attackerObjects;
    private List<GameObject> defenderObjects;
    private GameObject killedObject;
    private ArmyFactory armyFactory;
    private List<GameObject> armyKinds;

    private Army attacker;
    private Army defender;
    bool attackerWon;

    private void LateUpdate()
    {
        if (WarPanelGo.activeSelf)
        {
            IList<GameObject> losingUnits = (attackerWon) ? attackerObjects : defenderObjects;
            //DrawArmyKilled(losingUnits);
        }
    }

    private void DrawArmyKilled(IList<GameObject> losingUnits)
    {
        if (losingUnits.Count == 0)
            return;

        GameObject killedArmyGo = losingUnits[0];        
        Vector3 position = killedArmyGo.transform.position;

        // Draw killed sprite over defeated unit
        killedObject.transform.SetPositionAndRotation(position, Quaternion.identity);
        GameObject tempKilledGo = Instantiate(KilledPrefab, position, Quaternion.identity, WarPanelGo.transform);
        Destroy(tempKilledGo, GameManager.WarTime);
        Destroy(killedArmyGo, GameManager.WarTime);
        losingUnits.RemoveAt(0);

        // TODO: Beep        
    }

    public void Initialize(Army attacker, Army defender, GameObject[] armyKinds)
    {
        if (armyKinds == null)
        {
            throw new ArgumentNullException(nameof(armyKinds));
        }
        this.attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        this.defender = defender ?? throw new ArgumentNullException(nameof(defender));
        
        this.armyFactory = ArmyFactory.Create(armyKinds);
                
        attackerObjects = new List<GameObject>();
        for (int i = 0; i < attacker.Size; i++)
        {
            // Calculate attacker rendering position based on number of units
            // HACK: Just experimenting with positions for now
            
            GameObject attackerKind = armyFactory.FindGameObjectKind(attacker);

            // Create the GO for the panel
            Vector3 position = AttackerPrefab.transform.position;
            GameObject attackerGo = Instantiate<GameObject>(
                AttackerPrefab, position, Quaternion.identity, WarPanelGo.transform);

            // Replace image with army kind
            Image image = attackerGo.GetComponent<Image>();
            SpriteRenderer spriteRenderer = attackerKind.GetComponent<SpriteRenderer>();
            image.sprite = spriteRenderer.sprite;

            RectTransform rectTransform = WarPanelGo.GetComponent<RectTransform>();
            int attackerCount = attacker.Size;
            float xUnitSize = .75f; //spriteRenderer.size.x;
            const float xSwag = -.25f;
            const float xOffset = 0f;
            //float xTotal = (xUnitSize * attackerCount) + (xOffset * (attackerCount - 1));
            //float xShifted = ((xUnitSize * (i + 1)) + (xOffset * i)) - (xTotal / 2) + xSwag;
            float xTotal = xUnitSize * attackerCount;
            float xShifted = (xUnitSize * (i + 1)) - (xTotal / 2) + xOffset;

            attackerGo.transform.position = new Vector3(position.x - xShifted, position.y);
            attackerGo.SetActive(true);
            attackerObjects.Add(attackerGo);
        }

        //defenderObjects = new List<GameObject>();
        //for (int i = 0; i < defender.Size; i++)
        //{
        //    // Calculate attacker rendering position based on number of units
        //    // HACK: Just experimenting with positions for now
        //    Vector3 positionOffset = new Vector3(position.x + .5f, position.y + .5f);
        //    GameObject defenderPrefab = armyFactory.FindGameObjectKind(defender);
        //    GameObject defenderGo = Instantiate<GameObject>(
        //        defenderPrefab, positionOffset, Quaternion.identity, WarPanelGo.transform);
        //    attackerObjects.Add(defenderGo);
        //}

        this.WarPanelGo.SetActive(true);
    }

    public void Teardown()
    {
        this.WarPanelGo.SetActive(false);
        //this.killedObject.SetActive(false);
        this.attackerObjects.ForEach(go => Destroy(go));
        //this.defenderObjects.ForEach(go => Destroy(go));        
    }

    public void UpdateBattle(bool attackerWon)
    {
        this.attackerWon = attackerWon;
    }
}
