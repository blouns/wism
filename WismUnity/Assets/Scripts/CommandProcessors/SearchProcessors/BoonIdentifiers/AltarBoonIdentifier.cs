using System;
using UnityEngine;
using Wism.Client.Core;

namespace Assets.Scripts.CommandProcessors
{
    public class AltarBoonIdentifier : IBoonIdentfier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is AltarBoon;
        }

        public void Identify(IBoon boon)
        {
            if (!CanIdentify(boon))
            {
                throw new ArgumentException("Cannot identify " + boon);
            }

            var strengthBoon = (int)boon.Result;
            if (strengthBoon == 0)
            {
                ShowNotification($"The gods ignore you!");
            }
            else if (strengthBoon > 0)
            {
                ShowNotification($"Your strength has increased by {strengthBoon}!");
            }
            else
            {
                ShowNotification($"Your strength decreased by {strengthBoon}");
            }
        }

        private static void ShowNotification(string message)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(message);
        }
    }
}
