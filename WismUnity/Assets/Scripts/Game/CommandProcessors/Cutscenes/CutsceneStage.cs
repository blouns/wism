using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{

    public abstract class CutsceneStage
    {
        public Command Command { get; }

        public List<CutsceneStage> Children { get; set; }

        public CutsceneStage Next { get; set; }

        public InputManager InputManager { get; set; }

        private bool keyPressed;

        public CutsceneStage(Command command)
        {
            Command = command;
            this.InputManager = GameObject.FindGameObjectWithTag("UnityManager")
                    .GetComponent<UnityManager>()
                    .InputManager;
        }

        public abstract SceneResult Action();

        internal void OnAnyKeyPressed()
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

        protected void Notify(string message, params object[] args)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                .GetComponent<NotificationBox>();
            InputManager.SetInputMode(InputMode.WaitForKey);
            messageBox.Notify(String.Format(message, args));            
        }

        protected bool? AskAcceptReject(string message, params object[] args)
        {
            return AskYesNo("AcceptRejectPanel", message, args);
        }

        protected bool? AskYesNo(string message, params object[] args)
        {
            return AskYesNo("YesNoBox", message, args);
        }

        protected bool? AskYesNo(string panelName, string message, params object[] args)
        {
            var yesNoBox = GameObject.FindGameObjectWithTag(panelName)
               .GetComponent<YesNoBox>();

            if (!yesNoBox.Answer.HasValue)
            {
                InputManager.SetInputMode(InputMode.UI);
                yesNoBox.Ask(String.Format(message, args));
            }

            return yesNoBox.Answer;
        }
    }
}
