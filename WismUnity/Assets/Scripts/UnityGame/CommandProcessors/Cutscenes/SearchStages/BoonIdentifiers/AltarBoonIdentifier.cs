using Assets.Scripts.UI;
using System;
using UnityEngine;
using Wism.Client.Core.Boons;

namespace Assets.Scripts.CommandProcessors
{
    public class ThroneBoonIdentifier : IBoonIdentifier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is ThroneBoon;
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
