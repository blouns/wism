using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchSageProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly UnityManager unityGame;
        private Librarian librarian = new Librarian();

        public SearchSageProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is SearchSageCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchSageCommand)command;            

            var result = searchCommand.Execute();
            if (searchCommand.Gold > 0)
            {
                ShowNotification("You are greeted warmly...");
                ShowNotification("...the Seer gives you a gem...");
                ShowNotification($"...worth {searchCommand.Gold} gp!");
            }

            if (result == ActionState.Succeeded)
            {
                ShowNotification("A sign says, \"Go away\"");
            }
            else
            {
                ShowNotification("You have found nothing!");
            }

            return result;
        }

        private static void ShowNotification(string message)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(message);
        }
    }
}
