using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.Managers
{
    public class SearchTempleProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly UnityManager unityGame;

        public SearchTempleProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new ArgumentNullException(nameof(unityGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is SearchTempleCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var templeCommand = (SearchTempleCommand)command;

            ShowNotification($"You have found a temple...");

            var result = templeCommand.Execute();

            if (result == ActionState.Succeeded)
            {
                if (templeCommand.NumberOfArmiesBlessed == 1)
                {
                    ShowNotification("You have been blessed! Seek more blessings in far temples!");
                }
                else
                {
                    ShowNotification("{0} Armies have been blessed! Seek more blessings in far temples!",
                        templeCommand.NumberOfArmiesBlessed);
                }
            }
            else
            {
                ShowNotification("You have already received our blessing! Try another temple!");
            }


            return result;
        }

        private static void ShowNotification(string message, params object[] args)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(String.Format(message, args));
        }
    }
}
