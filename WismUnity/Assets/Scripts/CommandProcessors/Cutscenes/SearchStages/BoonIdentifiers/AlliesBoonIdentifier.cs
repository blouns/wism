using Assets.Scripts.UI;
using System;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors
{
    public class AlliesBoonIdentifier : IBoonIdentfier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is AlliesBoon;
        }

        public void Identify(IBoon boon)
        {
            if (!CanIdentify(boon))
            {
                throw new ArgumentException("Cannot identify " + boon);
            }

            var armies = (Army[])boon.Result;
            if (armies.Length == 1)
            {
                ShowNotification($"A {armies[0].DisplayName} has offered to join your party!");
            }
            else if (armies.Length > 0)
            {
                ShowNotification($"{armies.Length} {armies[0].DisplayName} have offered to join your party!");
            }
            else
            {
                throw new ArgumentException("No armies found in the AlliesBoon");
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
