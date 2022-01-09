using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using UnityEngine;
using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{

    public abstract class CutsceneStage
    {
        public Command Command { get; private set;  }

        public InputManager InputManager { get; set; }

        public bool IsFirstRun { get; set; }

        private bool keyPressed;

        public CutsceneStage(Command command)
        {
            Command = command;
            this.InputManager = GameObject.FindGameObjectWithTag("UnityManager")
                    .GetComponent<UnityManager>()
                    .InputManager;

            this.IsFirstRun = true;
        }

        public SceneResult Action()
        {
            if (this.IsFirstRun)
            {
                // Clear any static state such as YesNoBox results
                // TODO: Consider instantiating new YesNo instead of static
                Reset();
                this.IsFirstRun = false;
            }            

            return ActionInternal();
        }

        protected abstract SceneResult ActionInternal();


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
            else
            {
                // REVIEW: Should this instead be 'previous' input mode?
                InputManager.SetInputMode(InputMode.WaitForKey);
            }

            return yesNoBox.Answer;
        }

        protected void Reset()
        {
            this.keyPressed = false;
            GameObject.FindGameObjectWithTag("YesNoBox")
               .GetComponent<YesNoBox>()
               .Clear();
        }
    }
}
