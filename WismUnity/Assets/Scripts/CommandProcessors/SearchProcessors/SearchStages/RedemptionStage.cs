using System;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors
{

    public abstract class RedemptionStage
    {
        public Tile TargetTile { get; private set; }
        public Player SearchingPlayer { get; private set; }
        public Location Location { get; private set; }
        public Army Hero { get; private set; }
        public RedemptionStage NextStage { get; set; }
        public SearchRuinsCommand Command { get; }

        public RedemptionStage(SearchRuinsCommand command)
        {
            Command = command;
            TargetTile = World.Current.Map[Command.Location.X, Command.Location.Y];
            SearchingPlayer = Command.Armies[0].Player;
            Location = TargetTile.Location;
            Hero = Command.Armies.Find(a =>
                a is Hero &&
                a.Tile == TargetTile &&
                a.MovesRemaining > 0);
        }

        public abstract SearchResult Execute();

        private protected static void Notify(string message, params object[] args)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(String.Format(message, args));
        }

        private protected static bool? AskYesNo(string message, params object[] args)
        {
            var yesNoBox = GameObject.FindGameObjectWithTag("YesNoBox")
               .GetComponent<YesNoBox>();

            if (!yesNoBox.Answer.HasValue)
            {
                yesNoBox.Ask(String.Format(message, args));
            }

            return yesNoBox.Answer;
        }
    }
}
