using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchRuinsProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly UnityManager unityGame;
        private readonly List<IBoonIdentfier> boonIdentifiers;

        public SearchRuinsProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new ArgumentNullException(nameof(unityGame));
            this.boonIdentifiers = new List<IBoonIdentfier>()
            {
                new AlliesBoonIdentifier(),
                new AltarBoonIdentifier(),
                new ArtifactBoonIdentifier(),
                new GoldBoonIdentifier(),
            };
        }

        public bool CanExecute(ICommandAction command)
        {
            // Ruins and tombs are interchangable
            return command is SearchRuinsCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var ruinsCommand = command as SearchRuinsCommand;

            var targetTile = World.Current.Map[ruinsCommand.Location.X, ruinsCommand.Location.Y];
            var searchingPlayer = ruinsCommand.Armies[0].Player;
            var searchingArmies = new List<Army>(ruinsCommand.Armies);
            var location = targetTile.Location;

            if (location == null)
            {
                throw new InvalidOperationException("No location found on this tile: " + targetTile);
            }

            var hero = searchingArmies.Find(a => 
                a is Hero && 
                a.Tile == targetTile &&
                a.MovesRemaining > 0);

            if (hero == null)
            {
                ShowNotification("You have found nothing!");
                return ActionState.Failed;
            }

            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();
            worldTilemap.ShowSearchIcon(hero.X, hero.Y);            

            if (location.Boon is AltarBoon)
            {
                ShowNotification("An altar stands before you. Do you wish to approach?");
                //var key = Console.ReadKey();
                //if (key.Key != ConsoleKey.Y)
                //{
                //    return ActionState.Failed;
                //}
                //Console.WriteLine();
            }

            var monster = location.Monster;
            if (monster != null)
            {
                ShowNotification($"{hero.DisplayName} encounters a {monster}...");
            }            

            // Search the ruins
            var result = ruinsCommand.Execute();
            if (result == ActionState.Succeeded)
            {
                if (monster != null)
                {
                    ShowNotification("...and is victorious!");
                }

                DisplayBoon(ruinsCommand.Boon);
            }
            else if (result == ActionState.Failed &&
                     hero.IsDead)
            {
                ShowNotification("...and is slain!");                
            }
            else
            {
                ShowNotification("You have found nothing!");                
            }

            return result;
        }

        private void DisplayBoon(IBoon boon)
        {
            foreach (var identifier in boonIdentifiers)
            {
                if (identifier.CanIdentify(boon))
                {
                    identifier.Identify(boon);
                    return;
                }
            }

            throw new ArgumentException("Cannot identify boon: " + boon);
        }

        private static void ShowNotification(string message)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(message);
        }
    }
}
