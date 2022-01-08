using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class WarPanel : MonoBehaviour
    {
        public GameObject KilledPrefab;
        public GameObject AttackerPrefab;
        public GameObject DefenderPrefab;

        private Dictionary<Army, GameObject> attackerPanelObjects = new Dictionary<Army, GameObject>();
        private Dictionary<Army, GameObject> defenderPanelObjects = new Dictionary<Army, GameObject>();
        private ArmyManager armyManager;

        private int currentAttackerIndex;
        private int currentDefenderIndex;

        public void Initialize(List<Army> attackers, List<Army> defenders, Tile targetTile)
        {
            if (attackers is null)
            {
                throw new ArgumentNullException(nameof(attackers));
            }

            if (defenders is null)
            {
                throw new ArgumentNullException(nameof(defenders));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            if (defenders.Count == 0 &&
                targetTile.HasCity())
            {
                // Defenseless city
                if (defenderPanelObjects != null)
                {
                    defenderPanelObjects.Clear();
                }
            }

            this.armyManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<ArmyManager>();

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
            const float xArmySize = 60f;
            const float xOffset = 0.0f;

            index = armies.Count - index; // Reverse the order to draw left-to-right
            float xTotal = xArmySize * armies.Count;
            float xShifted = (xArmySize * index) - (xTotal / 2) + xOffset;
            return new Vector3(position.x - xShifted, position.y);
        }

        private void ReplaceImage(Army army, GameObject armyGo)
        {
            GameObject armyKind = armyManager.FindGameObjectKind(army);
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
            if (defenders.Count == 0)
            {
                Debug.Log("The garrison has fled before you!", this);
                return;
            }

            var attacker = attackers[currentAttackerIndex];
            var defender = defenders[currentDefenderIndex];
            bool didAttackerWin;
            Army losingArmy;

            if (attacker.IsDead)
            {
                didAttackerWin = false;
                losingArmy = attacker;
                currentAttackerIndex = (currentAttackerIndex + 1 == attackers.Count) ?
                    currentAttackerIndex :
                    currentAttackerIndex + 1;
            }
            else
            {
                didAttackerWin = true;
                losingArmy = defender;
                currentDefenderIndex = (currentDefenderIndex + 1 == defenders.Count) ?
                    currentDefenderIndex :
                    currentDefenderIndex + 1;
            }

            Dictionary<Army, GameObject> losingArmies = (didAttackerWin) ? defenderPanelObjects : attackerPanelObjects;

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

            // Remove killed sprite and army from panel after an interval
            var unityManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<UnityManager>();
            Destroy(killedPanelObject, unityManager.GameManager.WarTime);
            Destroy(losingArmyPanelObject, unityManager.GameManager.WarTime);
            losingArmies.Remove(losingArmy);
        }
    }
}