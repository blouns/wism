using System;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Ai.SearchProcessors;

public class SearchTempleAiProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private IWismLogger logger;

    public SearchTempleAiProcessor(IWismLoggerFactory loggerFactory, AsciiGame asciiGame)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        logger = loggerFactory.CreateLogger();
        this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
    }

    public bool CanExecute(ICommandAction command)
    {
        return command is SearchTempleCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        var templeCommand = (SearchTempleCommand)command;

        Notify.DisplayAndWait("You have found a temple...");

        var result = templeCommand.Execute();

        if (result == ActionState.Succeeded)
        {
            if (templeCommand.BlessedArmyCount == 1)
            {
                Notify.DisplayAndWait("You have been blessed! Seek more blessings in far temples!");
            }
            else
            {
                Notify.DisplayAndWait("{0} Armies have been blessed! Seek more blessings in far temples!",
                    templeCommand.BlessedArmyCount);
            }
        }
        else
        {
            Notify.DisplayAndWait("You have already received our blessing! Try another temple!");
        }


        return result;
    }
}