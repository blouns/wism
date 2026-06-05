using System;
using Wism.Client.Api.Telemetry;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.SearchProcessors;

public class SearchLibraryProcessor : InstrumentedProcessor
{
    private IWismLogger logger;

    public SearchLibraryProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
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
        return command is SearchLibraryCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
    {
        var searchCommand = (SearchLibraryCommand)command;

        if (IsHuman)
        {
            Notify.DisplayAndWait("You enter a great Library...");
            Notify.DisplayAndWait("Searching through the books, you find...");
        }

        var result = searchCommand.Execute();

        var knowledge = "Nothing!";
        if (result == ActionState.Succeeded)
        {
            knowledge = searchCommand.Knowledge;
        }

        if (IsHuman)
        {
            Notify.DisplayAndWait(knowledge);
        }
        else
        {
            // TODO: AI players should 'learn' from the library
        }

        return result;
    }
}