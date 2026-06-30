using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public static class WismFontResolver
    {
        public static Font Resolve(Transform context = null)
        {
            if (context != null)
            {
                var text = context.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(candidate => candidate.font != null);
                if (text != null)
                {
                    return text.font;
                }

                var parent = context.GetComponentInParent<Text>();
                if (parent != null && parent.font != null)
                {
                    return parent.font;
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
