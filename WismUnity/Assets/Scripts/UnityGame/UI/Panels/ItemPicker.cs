using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public class ItemPicker : MonoBehaviour
    {
        private List<MapObject> items;
        private int selectedIndex;
        private bool isInitialized;
        private const int MaxItems = 1000;

        public OkCancel OkCancelResult { get; private set; }

        public void Initialize(UnityManager unityGame, List<MapObject> items)
        {
            if (unityGame is null)
            {
                throw new ArgumentNullException(nameof(unityGame));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            Clear();
            this.gameObject.SetActive(true);
            this.items = items;
            RenderItemRows(items);
            this.OkCancelResult = OkCancel.Picking;

            this.isInitialized = true;
        }

        public bool IsInitialized()
        {
            return this.isInitialized;
        }

        public void SetCurrentItem(int index)
        {
            this.selectedIndex = index;
        }

        public void Ok()
        {
            if (this.selectedIndex < 0)
            {
                // Nothing to select
                Cancel();
                return;
            }

            this.OkCancelResult = OkCancel.Ok;
            Close();
        }

        public void Cancel()
        {
            this.selectedIndex = -1;
            this.OkCancelResult = OkCancel.Cancel;
            Close();
        }

        private void Close()
        {
            this.isInitialized = false;
            this.gameObject.SetActive(false);
        }

        public MapObject GetSelectedItem()
        {
            MapObject item = null;
            if (this.selectedIndex >= 0 && this.selectedIndex < this.items.Count)
            {
                item = this.items[this.selectedIndex];
            }

            return item;
        }

        private void RenderItemRows(List<MapObject> items)
        {
            if (items is null || items.Count == 0)
            {
                return;
            }

            // Get the button template
            var content = UnityUtilities.GameObjectHardFind("Content").transform;
            var buttonTemplateGO = content.Find("ButtonTemplate").gameObject;
            var rt = buttonTemplateGO.GetComponent<RectTransform>();
            float height = rt.sizeDelta.y;
            float totalHeight = height;

            // Create buttons for each item            
            int itemCount = Math.Min(MaxItems, items.Count);
            for (int i = 1; i <= itemCount; i++)
            {
                var newButton = Instantiate(buttonTemplateGO, content.transform);
                newButton.name = "Button" + i;
                newButton.GetComponentInChildren<Text>().text = items[i - 1].DisplayName;
                var selectedButtonScript = newButton.GetComponent<SelectableButton>();
                newButton.GetComponent<Button>().onClick.AddListener(selectedButtonScript.OnClick);
                newButton.SetActive(true);
            }
        }

        /// <summary>
        /// Clears the items
        /// </summary>
        public void Clear(Transform content = null)
        {
            if (content == null)
            {
                content = UnityUtilities.GameObjectHardFind("Content").transform;
            }

            this.selectedIndex = -1;
            if (this.items != null)
            {
                this.items.Clear();
            }

            // Skip template button
            int count = content.childCount;
            if (count > 1)
            {
                for (int i = 1; i <= count; i++)
                {
                    var button = content.Find("Button" + i);
                    if (button)
                    {
                        DestroyImmediate(button.gameObject);
                    }
                }
            }

            this.OkCancelResult = OkCancel.None;
        }
    }
}