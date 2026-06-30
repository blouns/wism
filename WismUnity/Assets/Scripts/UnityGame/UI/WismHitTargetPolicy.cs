using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public static class WismHitTargetPolicy
    {
        public static void Apply(GameObject control, WismUiTheme theme = null)
        {
            if (control == null)
            {
                return;
            }

            theme ??= WismUiTheme.Classic;
            var layout = control.GetComponent<LayoutElement>() ?? control.AddComponent<LayoutElement>();
            layout.minWidth = Mathf.Max(layout.minWidth, theme.MinimumHitTarget.x);
            layout.minHeight = Mathf.Max(layout.minHeight, theme.MinimumHitTarget.y);
        }
    }
}
