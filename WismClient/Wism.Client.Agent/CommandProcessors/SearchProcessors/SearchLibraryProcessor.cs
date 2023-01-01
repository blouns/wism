using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors;

public class SearchLibraryProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private ILogger logger;

    public SearchLibraryProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.logger = loggerFactory.CreateLogger();
        this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
    }

    public bool CanExecute(ICommandAction command)
    {
        return command is SearchLibraryCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        var searchCommand = (SearchLibraryCommand)command;

        Notify.DisplayAndWait("You enter a great Library...");
        Notify.DisplayAndWait("Searching through the books, you find...");

        var result = searchCommand.Execute();

        var knowledge = "Nothing!";
        if (result == ActionState.Succeeded)
        {
            knowledge = searchCommand.Knowledge;
        }

        Notify.DisplayAndWait(knowledge);

        return result;
    }
}