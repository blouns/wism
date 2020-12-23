using UnityEngine;

namespace Assets.Scripts.UI
{
    public class SelectedArmyBox : MonoBehaviour
    {
        public void SetActive(bool active)
        {
            this.gameObject.SetActive(active);
        }

        public void ShowSelectedBox(Vector3 worldVector)
        {
            this.transform.position = worldVector;
            this.SetActive(true);
        }

        public void HideSelectedBox()
        {
            this.gameObject.SetActive(false);
        }

        public bool IsSelectedBoxActive()
        {
            return this.gameObject.activeSelf;
        }
    }
}
