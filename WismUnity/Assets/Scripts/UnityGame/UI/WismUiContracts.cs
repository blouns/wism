using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public enum WismUiControlRole
    {
        Command,
        Toggle,
        Navigation,
        Selection,
        Status
    }

    public enum WismUiControlState
    {
        Normal,
        Selected,
        Disabled,
        Busy,
        Hidden
    }

    public enum WismUiInputModality
    {
        Mouse,
        Keyboard,
        SimulatedTouch
    }

    public sealed class WismUiSurface : MonoBehaviour
    {
        [SerializeField] private string surfaceId = string.Empty;
        [SerializeField] private WismUiControlState[] requiredStates = Array.Empty<WismUiControlState>();

        public string SurfaceId => this.surfaceId;
        public IReadOnlyList<WismUiControlState> RequiredStates => this.requiredStates;

        public static WismUiSurface Ensure(GameObject target, string id, params WismUiControlState[] states)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var surface = target.GetComponent<WismUiSurface>() ?? target.AddComponent<WismUiSurface>();
            surface.surfaceId = id ?? string.Empty;
            surface.requiredStates = states == null ? Array.Empty<WismUiControlState>() : states.Distinct().ToArray();
            return surface;
        }
    }

    public sealed class WismUiControl : MonoBehaviour
    {
        [SerializeField] private string semanticId = string.Empty;
        [SerializeField] private string actionId = string.Empty;
        [SerializeField] private WismUiControlRole role = WismUiControlRole.Command;
        [SerializeField] private WismUiControlState state = WismUiControlState.Normal;
        [SerializeField] private int overlapPriority;

        public string SemanticId => this.semanticId;
        public string ActionId => this.actionId;
        public WismUiControlRole Role => this.role;
        public WismUiControlState State => ResolveState();
        public int OverlapPriority => this.overlapPriority;
        public bool IsEnabled => this.isActiveAndEnabled && ResolveState() != WismUiControlState.Disabled;

        public static WismUiControl Ensure(
            GameObject target,
            string semanticId,
            WismUiControlRole role,
            string actionId,
            int overlapPriority = 0)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var control = target.GetComponent<WismUiControl>() ?? target.AddComponent<WismUiControl>();
            control.semanticId = semanticId ?? string.Empty;
            control.actionId = actionId ?? string.Empty;
            control.role = role;
            control.overlapPriority = overlapPriority;
            return control;
        }

        public void SetState(WismUiControlState nextState)
        {
            this.state = nextState;
        }

        private WismUiControlState ResolveState()
        {
            if (!this.gameObject.activeInHierarchy)
            {
                return WismUiControlState.Hidden;
            }

            var selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable())
            {
                return WismUiControlState.Disabled;
            }

            return this.state;
        }
    }

    public readonly struct WismUiHitCandidate
    {
        public WismUiHitCandidate(
            WismUiControl control,
            Rect visualBounds,
            Rect effectiveBounds,
            int hierarchyOrder)
        {
            Control = control;
            VisualBounds = visualBounds;
            EffectiveBounds = effectiveBounds;
            HierarchyOrder = hierarchyOrder;
        }

        public WismUiControl Control { get; }
        public Rect VisualBounds { get; }
        public Rect EffectiveBounds { get; }
        public int HierarchyOrder { get; }
    }

    public static class WismUiHitResolver
    {
        public static WismUiHitCandidate? Resolve(IEnumerable<WismUiHitCandidate> candidates, Vector2 point)
        {
            if (candidates == null)
            {
                return null;
            }

            var winner = candidates
                .Where(candidate => candidate.Control != null && candidate.EffectiveBounds.Contains(point))
                .OrderByDescending(candidate => candidate.Control.IsEnabled)
                .ThenByDescending(candidate => candidate.Control.OverlapPriority)
                .ThenBy(candidate => DistanceToRect(candidate.VisualBounds, point))
                .ThenBy(candidate => candidate.HierarchyOrder)
                .FirstOrDefault();

            return winner.Control == null ? (WismUiHitCandidate?)null : winner;
        }

        public static float DistanceToRect(Rect rect, Vector2 point)
        {
            var dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            var dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }
    }

    public static class WismUiIds
    {
        public static string FromName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Concat(value.Select((character, index) =>
                char.IsUpper(character) && index > 0 ? "-" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
        }
    }

    public static class WismPointerRoutingPolicy
    {
        public static bool CanRouteToMap(bool pointerOverUi, bool gameInputEnabled)
        {
            return gameInputEnabled && !pointerOverUi;
        }
    }
}
