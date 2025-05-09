﻿using Assets.Scripts.UI;
using System;
using UnityEngine;
using Wism.Client.Core.Boons;

namespace Assets.Scripts.CommandProcessors
{
    public class GoldBoonIdentifier : IBoonIdentifier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is GoldBoon;
        }

        public void Identify(IBoon boon)
        {
            if (!CanIdentify(boon))
            {
                throw new ArgumentException("Cannot identify " + boon);
            }

            var gold = (int)boon.Result;
            ShowNotification($"You have found {gold} gp!");
        }

        private static void ShowNotification(string message)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(message);
        }
    }
}
