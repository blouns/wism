using System;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class SelectableButton : MonoBehaviour
    {
        private ItemPicker itemPicker;

        public void Awake()
        {
            this.itemPicker = UnityUtilities.GameObjectHardFind("ItemPickerPanel")
                .GetComponent<ItemPicker>();
        }

        public void OnClick()
        {
            string name = gameObject.name;
            if (name == "ButtonTemplate")
            {
                return;
            }

            int index = Int32.Parse(name.Replace("Button", ""));
            itemPicker.SetCurrentItem(index - 1);
        }
    }
}
