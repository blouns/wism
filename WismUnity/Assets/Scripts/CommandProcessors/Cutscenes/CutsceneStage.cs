using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors
{

    public abstract class CutsceneStage
    {
        public Tile TargetTile { get; private set; }
        public Player Player { get; private set; }
        public Location Location { get; private set; }
        public List<Army> Armies { get; private set;  }
        public Army Hero { get; private set; }
        public Command Command { get; }

        private bool keyPressed;

        public CutsceneStage(SearchLocationCommand command)
        {
            Command = command;
            UnpackCommand(command);
        }

        public abstract SceneResult Action();

        public void OnAnyKeyPressed()
        {
            keyPressed = true;
        }

        protected SceneResult ContinueOnKeyPress()
        {
            if (KeyPressed())
            {
                return SceneResult.Continue;
            }
            else
            {
                return SceneResult.Wait;
            }
        }

        protected bool KeyPressed()
        {
            return keyPressed;
        }

        protected static void ClearNotifications()
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.ClearNotification();
        }

        protected static void Notify(string message, params object[] args)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            messageBox.Notify(String.Format(message, args));            
        }

        protected static bool? AskYesNo(string message, params object[] args)
        {
            var yesNoBox = GameObject.FindGameObjectWithTag("YesNoBox")
               .GetComponent<YesNoBox>();

            if (!yesNoBox.Answer.HasValue)
            {
                yesNoBox.Ask(String.Format(message, args));
            }

            return yesNoBox.Answer;
        }

        private void UnpackCommand(SearchLocationCommand command)
        {
            TargetTile = World.Current.Map[command.Location.X, command.Location.Y];
            Player = command.Armies[0].Player;
            Location = TargetTile.Location;
            Armies = command.Armies;
            Hero = command.Armies.Find(a =>
                a is Hero &&
                a.Tile == TargetTile &&
                a.MovesRemaining > 0);
        }
    }
}
