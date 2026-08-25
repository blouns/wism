using UnityEngine;

namespace Assets.Scripts.UI
{
    public sealed class WismUiTheme
    {
        public static readonly WismUiTheme Classic = new WismUiTheme();

        public readonly Color PanelBackground = new Color32(44, 54, 72, 255);
        public readonly Color PanelBorder = new Color32(199, 204, 211, 255);
        public readonly Color MinimapBackground = new Color32(30, 38, 48, 255);
        public readonly Color Text = new Color32(236, 238, 226, 255);
        public readonly Color MutedText = new Color32(181, 187, 184, 255);
        public readonly Color Button = new Color32(78, 96, 122, 255);
        public readonly Color ButtonDisabled = new Color32(60, 66, 76, 255);
        public readonly Color ButtonSelected = new Color32(198, 184, 68, 255);
        public readonly Color Danger = new Color32(176, 64, 48, 255);
        public readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);
        public readonly Vector2 LegacyVisualMinimum = new Vector2(44f, 34f);
        public readonly Vector2 DesktopHitTarget = new Vector2(32f, 32f);
        public readonly Vector2 TouchHitTarget = new Vector2(44f, 44f);

        // Layout keeps the established visual dimensions; WismHitArea expands only interaction bounds.
        public Vector2 MinimumHitTarget => this.LegacyVisualMinimum;
    }
}
