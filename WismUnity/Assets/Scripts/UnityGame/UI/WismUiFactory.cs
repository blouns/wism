using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public static class WismUiFactory
    {
        public static Text CreateText(Transform parent, string name, string value, int fontSize = 18, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = WismFontResolver.Resolve(parent);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = WismUiTheme.Classic.Text;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            string semanticId = null,
            string actionId = null,
            WismUiControlRole role = WismUiControlRole.Command,
            int overlapPriority = 0)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);

            var image = gameObject.GetComponent<Image>();
            image.color = WismUiTheme.Classic.Button;

            var button = gameObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = WismUiTheme.Classic.Button;
            colors.disabledColor = WismUiTheme.Classic.ButtonDisabled;
            colors.selectedColor = WismUiTheme.Classic.ButtonSelected;
            button.colors = colors;

            WismHitTargetPolicy.Apply(gameObject);
            WismUiControl.Ensure(
                gameObject,
                semanticId ?? WismUiIds.FromName(name),
                role,
                actionId ?? WismUiIds.FromName(name),
                overlapPriority);

            var text = CreateText(gameObject.transform, "Label", label, 16, TextAnchor.MiddleCenter);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        public static Image CreateIconSlot(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minWidth = 32f;
            layout.minHeight = 32f;
            layout.preferredWidth = 32f;
            layout.preferredHeight = 32f;
            var image = gameObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        public static RectTransform CreateVerticalPanel(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = WismUiTheme.Classic.PanelBackground;

            var layout = gameObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return gameObject.GetComponent<RectTransform>();
        }

        public static RectTransform CreateRow(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            gameObject.transform.SetParent(parent, false);
            var layout = gameObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            return gameObject.GetComponent<RectTransform>();
        }
    }
}
