using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchLibraryProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public SearchLibraryProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new System.ArgumentNullException(nameof(unityGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is SearchLibraryCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchLibraryCommand)command;

            ShowNotification("You enter a great Library...");
            ShowNotification("Searching through the books, you find...");

            var result = searchCommand.Execute();

            string knowledge = "Nothing!";
            if (result == ActionState.Succeeded)
            {
                knowledge = searchCommand.Knowledge;
            }

            ShowNotification(knowledge);

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
