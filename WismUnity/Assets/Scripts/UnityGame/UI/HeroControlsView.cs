using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.UI
{
    public sealed class HeroControlsView : MonoBehaviour
    {
        private UnityManager manager;
        private Font font;
        private Text heading;
        private Text details;
        private Text empty;
        private RectTransform heroContent;
        private RectTransform itemContent;
        private ScrollRect heroScroll;
        private ScrollRect itemScroll;
        private Button go;
        private Button closeButton;
        private Action close;
        private Game game;
        private Player player;
        private bool finding;
        private readonly List<Hero> heroes = new List<Hero>();
        private readonly List<Button> heroButtons = new List<Button>();
        public IReadOnlyList<Hero> Heroes => heroes.AsReadOnly();
        public Hero SelectedHero { get; private set; }

        public void Initialize(UnityManager owner, Font face, Action dismiss)
        {
            manager = owner;
            font = face;
            close = dismiss;
            gameObject.AddComponent<Image>().color = new Color32(128, 128, 128, 255);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(.06f, .10f);
            rect.anchorMax = new Vector2(.94f, .90f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            heading = Label(transform, "HeroHeading", "Heroes", 26);
            Bounds(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -58), new Vector2(-148, -12));
            closeButton = MakeButton(transform, "CloseHeroes", "Close", close);
            Bounds((RectTransform)closeButton.transform, Vector2.one, Vector2.one,
                new Vector2(-128, -58), new Vector2(-16, -14));
            details = Label(transform, "HeroDetails", "", 18);
            Bounds(details.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -102), new Vector2(-20, -62));
            heroScroll = List(transform, "HeroRoster", new Vector2(0, 0), new Vector2(.40f, 1),
                new Vector2(20, 84), new Vector2(-12, -110), out heroContent);
            itemScroll = List(transform, "HeroInventory", new Vector2(.40f, 0), Vector2.one,
                new Vector2(8, 84), new Vector2(-20, -110), out itemContent);
            empty = Label(heroScroll.viewport, "NoHeroes", "No living heroes", 20);
            Bounds(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            empty.alignment = TextAnchor.MiddleCenter;
            go = MakeButton(transform, "GoToHero", "Go to Hero", Navigate);
            Bounds((RectTransform)go.transform, Vector2.zero, Vector2.zero, new Vector2(20, 20), new Vector2(224, 64));
            WismUiSurface.Ensure(gameObject, "hero-controls", WismUiControlState.Normal,
                WismUiControlState.Selected, WismUiControlState.Disabled);
            gameObject.SetActive(false);
        }

        public void Open(bool find)
        {
            game = Game.Current;
            player = game.GetCurrentPlayer();
            finding = find;
            SelectedHero = null;
            heroes.Clear();
            Clear(heroContent);
            heroButtons.Clear();
            var choices = find ? player.GetArmies() : game.GetSelectedArmies();
            heroes.AddRange((choices ?? new List<Army>()).OfType<Hero>()
                .Where(IsCurrentHero).OrderBy(hero => hero.DisplayName, StringComparer.Ordinal).ThenBy(hero => hero.Id));
            heading.text = find ? "Heroes" : "Inventory";
            go.gameObject.SetActive(find);
            empty.text = find ? "No living heroes" : "No selected heroes";
            empty.gameObject.SetActive(heroes.Count == 0);
            for (int i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                var button = MakeButton(heroContent, "HeroRow" + i,
                    $"{hero.DisplayName}\n({hero.Tile.X}, {hero.Tile.Y})", () => Show(hero));
                Row((RectTransform)button.transform, i, 64);
                var trigger = button.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
                entry.callback.AddListener(_ => { Show(hero); Reveal(heroScroll, heroContent, heroes.IndexOf(hero), 64); });
                trigger.triggers.Add(entry);
                heroButtons.Add(button);
            }
            heroContent.sizeDelta = new Vector2(0, heroes.Count * 64);
            heroScroll.verticalNormalizedPosition = 1;
            gameObject.SetActive(true);
            Show(heroes.FirstOrDefault());
            EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
        }

        private bool IsCurrentHero(Hero hero) => hero != null && !hero.IsDead && hero.Player == player &&
            hero.Tile != null && player.GetArmies().Contains(hero) &&
            hero.Tile.X >= 0 && hero.Tile.Y >= 0 && hero.Tile.X < World.Current.Map.GetLength(0) &&
            hero.Tile.Y < World.Current.Map.GetLength(1) && World.Current.Map[hero.Tile.X, hero.Tile.Y] == hero.Tile;

        private void Show(Hero hero)
        {
            SelectedHero = hero;
            for (int i = 0; i < heroButtons.Count; i++)
                heroButtons[i].image.color = heroes[i] == hero ? new Color32(210, 198, 126, 255) : new Color32(158, 158, 158, 255);
            Clear(itemContent);
            details.text = hero == null ? player.Clan.DisplayName : $"{hero.DisplayName}  ({hero.Tile.X}, {hero.Tile.Y})";
            var items = hero?.Items;
            int count = items?.Count ?? 0;
            for (int i = 0; i < Math.Max(1, count); i++)
            {
                var item = count == 0 ? null : items[i];
                string text = item == null ? "No artifacts" :
                    $"{item.DisplayName}\nCombat {item.CombatBonus:+0;-0;0}   Command {item.CommandBonus:+0;-0;0}";
                var label = Label(itemContent, "InventoryItem" + i, text, 20);
                Row(label.rectTransform, i, 76);
            }
            itemContent.sizeDelta = new Vector2(0, Math.Max(1, count) * 76);
            itemScroll.verticalNormalizedPosition = 1;
            go.interactable = finding && hero != null && IsCurrentHero(hero);
        }

        private void Navigate()
        {
            if (!finding || SelectedHero == null || game != Game.Current || player != game.GetCurrentPlayer() ||
                !IsCurrentHero(SelectedHero)) return;
            var hero = SelectedHero;
            close();
            manager.NavigateToHero(hero);
        }

        private void Update()
        {
            if (!Game.IsInitialized() || game != Game.Current || player != Game.Current.GetCurrentPlayer())
            { close(); return; }
            if (heroes.Any(hero => !IsCurrentHero(hero))) { close(); return; }
        }

        private Text Label(Transform parent, string name, string text, int size)
        {
            var label = WismUiFactory.CreateText(parent, name, text, size, TextAnchor.MiddleLeft);
            label.font = font;
            label.color = Color.black;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = size;
            label.raycastTarget = false;
            return label;
        }

        private Button MakeButton(Transform parent, string name, string text, Action action)
        {
            var button = WismUiFactory.CreateButton(parent, name, text, "heroes." + name, "heroes." + name, overlapPriority: 100);
            button.image.color = new Color32(158, 158, 158, 255);
            var label = button.GetComponentInChildren<Text>();
            label.font = font;
            label.color = Color.black;
            label.fontSize = 20;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 20;
            button.onClick.AddListener(() => action());
            return button;
        }

        private ScrollRect List(Transform parent, string name, Vector2 min, Vector2 max,
            Vector2 insetMin, Vector2 insetMax, out RectTransform content)
        {
            var viewport = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);
            var rect = (RectTransform)viewport.transform;
            Bounds(rect, min, max, insetMin, insetMax);
            viewport.GetComponent<Image>().color = new Color32(117, 117, 117, 255);
            content = new GameObject(name + "Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(rect, false);
            content.anchorMin = Vector2.up;
            content.anchorMax = Vector2.one;
            content.pivot = Vector2.up;
            content.offsetMin = content.offsetMax = Vector2.zero;
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = rect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48;
            var track = new GameObject(name + "Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            track.transform.SetParent(rect, false);
            Bounds((RectTransform)track.transform, Vector2.right, Vector2.one, new Vector2(-18, 4), new Vector2(-4, -4));
            track.GetComponent<Image>().color = new Color32(65, 65, 65, 255);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(track.transform, false);
            handle.GetComponent<Image>().color = new Color32(200, 200, 200, 255);
            var bar = track.GetComponent<Scrollbar>();
            bar.handleRect = (RectTransform)handle.transform;
            bar.targetGraphic = handle.GetComponent<Image>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            content.offsetMax = new Vector2(-24, 0);
            return scroll;
        }

        private static void Reveal(ScrollRect scroll, RectTransform content, int index, float height)
        {
            float top = content.anchoredPosition.y;
            if (index * height < top) top = index * height;
            else if ((index + 1) * height > top + scroll.viewport.rect.height)
                top = (index + 1) * height - scroll.viewport.rect.height;
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(top, 0, Mathf.Max(0, content.rect.height - scroll.viewport.rect.height)));
        }

        private static void Row(RectTransform rect, int index, float height)
        {
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.up;
            rect.sizeDelta = new Vector2(0, height - 4);
            rect.anchoredPosition = new Vector2(0, -index * height);
        }

        private static void Clear(Transform parent)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        private static void Bounds(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }
    }
}
