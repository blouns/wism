using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class NotificationBox : MonoBehaviour
    {
        public const float DefaultInterval = 3f;

        private Text notificationText;
        private CanvasGroup infoPanelGroup;
        private float timer;
        private float waitTime = DefaultInterval;

        public void Start()
        {
            this.notificationText = UnityUtilities.GameObjectHardFind("NotificationBox")
                .GetComponent<Text>();

            this.infoPanelGroup = UnityUtilities.GameObjectHardFind("InformationPanel")
                .GetComponent<CanvasGroup>();
        }

        public void Update()
        {
            this.timer += Time.deltaTime;
            if (this.timer > this.waitTime)
            {
                ClearNotification();
            }
            else
            {
                ShowNotifications();
            }
        }

        private void ShowNotifications()
        {
            this.infoPanelGroup.alpha = 0f;
        }

        public void ClearNotification()
        {
            this.notificationText.text = "";
            this.infoPanelGroup.alpha = 1f;
        }

        public void Notify(string message, double interval = DefaultInterval)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            this.infoPanelGroup.alpha = 0f;
            this.notificationText.text = message;
            this.timer = 0f;
            ShowNotifications();
        }
    }
}