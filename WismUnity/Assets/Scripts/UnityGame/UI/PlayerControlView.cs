using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public sealed class PlayerControlView : MonoBehaviour
    {
        private UnityManager manager;
        private Font font;
        private Action close;
        private RectTransform content;
        private Button apply;
        private Game game;
        private readonly Dictionary<Player, Dropdown> rows = new Dictionary<Player, Dropdown>();

        public void Initialize(UnityManager owner, Font face, Action dismiss)
        {
            manager = owner; font = face; close = dismiss;
            gameObject.AddComponent<Image>().color = new Color32(128, 128, 128, 255);
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(660, 540);
            Label(transform, "ControlTitle", "Player Control", 26, 16, 12, 620, 40);
            Label(transform, "ControlTiming", "Clan                         Next turn", 18, 16, 56, 620, 32);
            var scrollObject = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            scrollObject.name = "ControlRoster";
            scrollObject.transform.SetParent(transform, false);
            Place((RectTransform)scrollObject.transform, 16, 92, 628, 372);
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            if (scroll.horizontalScrollbar != null) scroll.horizontalScrollbar.gameObject.SetActive(false);
            content = scroll.content;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            var cancel = WismUiFactory.CreateButton(transform, "CancelControls", "Cancel", "controls.cancel");
            Place((RectTransform)cancel.transform, 16, 480, 220, 44);
            cancel.onClick.AddListener(() => close());
            apply = WismUiFactory.CreateButton(transform, "ApplyControls", "Apply", "controls.apply");
            Place((RectTransform)apply.transform, 424, 480, 220, 44);
            apply.onClick.AddListener(Apply);
            WismUiSurface.Ensure(gameObject, "player-controls", WismUiControlState.Normal, WismUiControlState.Disabled);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            game = Game.Current;
            rows.Clear();
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var pending = manager.GetPendingPlayerControls().ToDictionary(choice => choice.ClanName);
            var players = game.Players.Where(player => !player.IsDead).ToArray();
            content.sizeDelta = new Vector2(0, players.Length * 48);
            content.anchoredPosition = Vector2.zero;
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                Label(content, "Clan" + i, player.Clan.DisplayName, 18, 8, i * 48, 290, 44);
                var dropdownObject = DefaultControls.CreateDropdown(new DefaultControls.Resources());
                dropdownObject.name = "Control" + player.Clan.ShortName;
                dropdownObject.transform.SetParent(content, false);
                Place((RectTransform)dropdownObject.transform, 312, i * 48, 280, 44);
                foreach (var text in dropdownObject.GetComponentsInChildren<Text>(true)) { text.font = font; text.fontSize = 18; text.color = Color.black; }
                var dropdown = dropdownObject.GetComponent<Dropdown>();
                var itemRect = (RectTransform)dropdown.itemText.transform.parent;
                itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, 44);
                dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 230);
                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string> { "Human", "Knight", "Baron", "Lord", "Warlord" });
                pending.TryGetValue(player.Clan.ShortName, out var choice);
                bool human = choice?.IsHuman ?? player.IsHuman;
                var difficulty = choice?.AiDifficulty ?? player.AiDifficulty;
                dropdown.SetValueWithoutNotify(human ? 0 : (int)difficulty + 1);
                rows.Add(player, dropdown);
            }
            gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(apply.gameObject);
        }

        private void Apply()
        {
            if (!gameObject.activeInHierarchy || Game.Current != game || game.GameState == GameState.GameOver ||
                rows.Keys.Any(player => player.IsDead || !game.Players.Contains(player))) return;
            foreach (var row in rows)
                if (row.Value.value < 0 || row.Value.value > 4) return;
            foreach (var row in rows)
                manager.QueuePlayerControl(row.Key.Clan.ShortName, row.Value.value == 0,
                    row.Value.value == 0 ? row.Key.AiDifficulty : (AiDifficultyTier)(row.Value.value - 1));
            close();
        }

        private void Label(Transform parent, string name, string value, int size, float x, float y, float width, float height)
        {
            var text = WismUiFactory.CreateText(parent, name, value, size);
            text.font = font; text.color = Color.black;
            Place(text.rectTransform, x, y, width, height);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
