using System;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors
{
    public class ArtifactBoonIdentifier : IBoonIdentfier
    {
        public bool CanIdentify(IBoon boon)
        {
            return boon is ArtifactBoon;
        }

        public void Identify(IBoon boon)
        {
            if (!CanIdentify(boon))
            {
                throw new ArgumentException("Cannot identify " + boon);
            }

            var artifact = (Artifact)boon.Result;
            ShowNotification($"You have found the {artifact.DisplayName}!");
        }

        private static void ShowNotification(string message)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(message);
        }
    }
}
