using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors;

public class SearchSageProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private readonly Librarian librarian = new();
    private ILogger logger;

    public SearchSageProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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
        return command is SearchSageCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        var searchCommand = (SearchSageCommand)command;

        var result = searchCommand.Execute();
        if (searchCommand.Gold > 0)
        {
            Notify.DisplayAndWait("You are greeted warmly...");
            Notify.DisplayAndWait("...the Seer gives you a gem...");
            Notify.DisplayAndWait($"...worth {searchCommand.Gold} gp!");
        }

        if (result == ActionState.Succeeded)
        {
            this.DoSagesAdvice();
        }
        else
        {
            Notify.DisplayAndWait("You have found nothing!");
        }

        return result;
    }

    private void DoSagesAdvice()
    {
        Notify.Information("A sign says:");
        Notify.Information("\t\"The Great Sage Master of Wisdom,");
        Notify.Information("\tInformation on Magical Items and Locations.\"");
        Notify.Information("The sage welcomes you");
        Notify.Information("What do you wish to know?");
        Console.WriteLine("[I]tems");
        Console.WriteLine("[L]ocations");
        Console.WriteLine("[C]ancel");
        Console.Write("Enter a command: ");
        var key = Console.ReadKey();
        Console.WriteLine();
        switch (key.Key)
        {
            case ConsoleKey.I:
                this.DoItemAdvice();
                break;
            case ConsoleKey.L:
                this.DoLocationAdvice();
                break;
            default:
                // TODO: Cancel should not debit moves for searching
                return;
        }
    }

    private void DoItemAdvice()
    {
        var names = this.librarian.GetAllArtifactNames();
        for (var i = 0; i < names.Length; i++)
        {
            Notify.Information("({0}) {1}", i, names[i]);
            if (i % 11 == 10)
            {
                Notify.DisplayAndWait("Press any key to continue...");
            }
        }

        var valid = false;
        while (!valid)
        {
            Console.Write("Select which [#]: ");
            var response = Console.ReadLine();
            Console.WriteLine();
            if ((!int.TryParse(response, out var index) &&
                 index < 0) || index >= names.Length)
            {
                Notify.Alert("Must enter a valid number.");
            }
            else
            {
                valid = true;
                var artifact = this.librarian.GetArtifact(index);
                var knowledge = this.librarian.GetSageArtifactKnowledge(artifact, false);
                foreach (var message in knowledge)
                {
                    Notify.DisplayAndWait(message);
                }
            }
        }
    }

    private void DoLocationAdvice()
    {
        var names = this.librarian.GetAllLocationNames();
        for (var i = 0; i < names.Length; i++)
        {
            Notify.Information("({0}) {1}", i, names[i]);
            if (i % 11 == 10)
            {
                Notify.DisplayAndWait("Press any key to continue...");
            }
        }

        var valid = false;
        while (!valid)
        {
            Console.WriteLine("Select which [#]: ");
            var response = Console.ReadLine();
            Console.WriteLine();
            if ((!int.TryParse(response, out var index) &&
                 index < 0) || index >= names.Length)
            {
                Notify.Alert("Must enter a valid number.");
            }
            else
            {
                valid = true;
                var location = this.librarian.GetLocation(index);
                var knowledge = this.librarian.GetSageLocationKnowledge(location);
                foreach (var message in knowledge)
                {
                    Notify.DisplayAndWait(message);
                }
            }
        }
    }
}