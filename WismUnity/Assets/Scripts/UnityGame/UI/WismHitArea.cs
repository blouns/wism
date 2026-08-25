using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class WismHitArea : MonoBehaviour
    {
        private const string RaycastTargetName = "WismExpandedHitArea";
        private static readonly List<WismHitArea> ActiveAreas = new List<WismHitArea>();

        [SerializeField] private Vector2 desktopMinimum = new Vector2(32f, 32f);
        [SerializeField] private Vector2 touchMinimum = new Vector2(44f, 44f);
        private RectTransform raycastRect;

        public Vector2 DesktopMinimum => this.desktopMinimum;
        public Vector2 TouchMinimum => this.touchMinimum;

        public void Configure(Vector2 desktop, Vector2 touch)
        {
            this.desktopMinimum = desktop;
            this.touchMinimum = touch;
            EnsureRaycastTarget();
            RefreshGeometry();
        }

        public Rect GetVisualScreenBounds()
        {
            return ScreenBounds(GetComponent<RectTransform>());
        }

        public Rect GetEffectiveScreenBounds(WismUiInputModality modality)
        {
            var visual = GetVisualScreenBounds();
            var minimum = modality == WismUiInputModality.SimulatedTouch ? this.touchMinimum : this.desktopMinimum;
            var width = Mathf.Max(visual.width, minimum.x);
            var height = Mathf.Max(visual.height, minimum.y);
            return new Rect(visual.center - new Vector2(width, height) * 0.5f, new Vector2(width, height));
        }

        public void RefreshGeometry()
        {
            EnsureRaycastTarget();
            var visualRect = GetComponent<RectTransform>();
            if (visualRect == null || this.raycastRect == null)
            {
                return;
            }

            var width = Mathf.Max(visualRect.rect.width, this.touchMinimum.x);
            var height = Mathf.Max(visualRect.rect.height, this.touchMinimum.y);
            this.raycastRect.anchorMin = new Vector2(0.5f, 0.5f);
            this.raycastRect.anchorMax = new Vector2(0.5f, 0.5f);
            this.raycastRect.pivot = new Vector2(0.5f, 0.5f);
            this.raycastRect.anchoredPosition = Vector2.zero;
            this.raycastRect.sizeDelta = new Vector2(width, height);
            this.raycastRect.SetAsLastSibling();
        }

        internal static WismHitArea ResolveAt(Vector2 point)
        {
            var candidates = ActiveAreas
                .Where(area => area != null && area.isActiveAndEnabled)
                .Select(area => area.ToCandidate())
                .ToArray();
            var winner = WismUiHitResolver.Resolve(candidates, point);
            return winner.HasValue ? winner.Value.Control.GetComponent<WismHitArea>() : null;
        }

        internal void Activate()
        {
            var button = GetComponent<Button>();
            if (button != null && button.IsInteractable())
            {
                button.onClick.Invoke();
            }
        }

        private void OnEnable()
        {
            if (!ActiveAreas.Contains(this))
            {
                ActiveAreas.Add(this);
            }

            EnsureRaycastTarget();
            RefreshGeometry();
        }

        private void OnDisable()
        {
            ActiveAreas.Remove(this);
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshGeometry();
        }

        private void EnsureRaycastTarget()
        {
            if (this.raycastRect != null)
            {
                return;
            }

            var existing = transform.Find(RaycastTargetName);
            var target = existing == null
                ? new GameObject(RaycastTargetName, typeof(RectTransform), typeof(Image), typeof(WismHitAreaRaycastTarget))
                : existing.gameObject;
            target.transform.SetParent(transform, false);
            var image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            var router = target.GetComponent<WismHitAreaRaycastTarget>() ?? target.AddComponent<WismHitAreaRaycastTarget>();
            router.Owner = this;
            this.raycastRect = target.GetComponent<RectTransform>();
        }

        private WismUiHitCandidate ToCandidate()
        {
            var control = GetComponent<WismUiControl>() ?? WismUiControl.Ensure(
                gameObject,
                WismUiIds.FromName(gameObject.name),
                WismUiControlRole.Command,
                WismUiIds.FromName(gameObject.name));
            return new WismUiHitCandidate(
                control,
                GetVisualScreenBounds(),
                GetEffectiveScreenBounds(WismUiInputModality.SimulatedTouch),
                HierarchyOrder(transform));
        }

        private static int HierarchyOrder(Transform target)
        {
            var order = 0;
            var multiplier = 1;
            while (target != null)
            {
                order += target.GetSiblingIndex() * multiplier;
                multiplier *= 1024;
                target = target.parent;
            }

            return order;
        }

        private static Rect ScreenBounds(RectTransform rect)
        {
            if (rect == null)
            {
                return default;
            }

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var minimum = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var maximum = minimum;
            for (var i = 1; i < corners.Length; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }

            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }
    }

    public sealed class WismHitAreaRaycastTarget : MonoBehaviour, IPointerClickHandler
    {
        public WismHitArea Owner { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            var winner = WismHitArea.ResolveAt(eventData.position);
            if (winner != null)
            {
                winner.Activate();
            }
        }
    }
}
