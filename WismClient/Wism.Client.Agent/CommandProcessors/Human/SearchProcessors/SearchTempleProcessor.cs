using System;
using Wism.Client.Api.Telemetry;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Human.SearchProcessors;

public class SearchTempleProcessor : InstrumentedProcessor
{
    private IWismLogger logger;

    public SearchTempleProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
        : base(publisher)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        logger = loggerFactory.CreateLogger();      
    }

    public override bool CanExecute(ICommandAction command)
    {
        return command is SearchTempleCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
    {
        var templeCommand = (SearchTempleCommand)command;

        if (IsHuman)
        {
            Notify.DisplayAndWait("You have found a temple...");
        }

        var result = templeCommand.Execute();

        if (IsHuman)
        {
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
        }

        return result;
    }
}