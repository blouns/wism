using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    [CreateAssetMenu(menuName = "WISM/UI/Motion Profile", fileName = "WismMotionProfile")]
    public sealed class WismMotionProfile : ScriptableObject
    {
        [SerializeField, Range(0f, 0.18f)] private float feedbackSeconds = 0.08f;
        [SerializeField, Range(0f, 0.18f)] private float transitionSeconds = 0.16f;
        [SerializeField] private bool reducedMotion;

        public float FeedbackSeconds => this.reducedMotion ? 0f : this.feedbackSeconds;
        public float TransitionSeconds => this.reducedMotion ? 0f : this.transitionSeconds;
        public bool ReducedMotion => this.reducedMotion;
    }

    [CreateAssetMenu(menuName = "WISM/UI/Typography Profile", fileName = "WismTypographyProfile")]
    public sealed class WismTypographyProfile : ScriptableObject
    {
        [SerializeField] private Font approvedFont;
        [SerializeField, Range(10, 32)] private int bodySize = 16;
        [SerializeField, Range(10, 36)] private int headingSize = 18;

        public Font ApprovedFont => this.approvedFont;
        public int BodySize => this.bodySize;
        public int HeadingSize => this.headingSize;

        public void Apply(Text text, bool heading = false)
        {
            if (text == null)
            {
                return;
            }

            text.font = this.approvedFont != null ? this.approvedFont : WismFontResolver.Resolve(text.transform);
            text.fontSize = heading ? this.headingSize : this.bodySize;
        }
    }
}
