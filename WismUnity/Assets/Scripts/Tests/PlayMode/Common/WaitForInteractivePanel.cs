using UnityEngine;

namespace Assets.Tests.PlayMode
{
    public class WaitForInteractivePanel : CustomYieldInstruction
    {
        /// <summary>
        /// Wait for a panel to become interactive (or uninteractive).
        /// </summary>
        /// <param name="panel">Panel to wait for</param>
        /// <param name="waitForActive">Wait for panel to be active if True, otherwise wait for panel to be inactive</param>
        public WaitForInteractivePanel(GameObject panel, bool waitForActive = true)
        {
            this.Panel = panel;
            this.WaitForActive = waitForActive;
        }

        public override bool keepWaiting
        {
            get
            {
                bool isActive = this.Panel.activeSelf;

                // Some panels use canvas groups to show/hide
                var canvasGroup = this.Panel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    isActive &= canvasGroup.interactable;
                }

                // Keep waiting until active (or until inactive)
                return (this.WaitForActive) ? !isActive : isActive;
            }
        }

        public GameObject Panel { get; }
        public bool WaitForActive { get; }
    }
}
