using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.Commands.Armies;
using UnityEngine;

namespace Assets.Scripts.CommandProcessors
{
    public class SelectArmyProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;
        private AudioSource noMovesAudio;

        public SelectArmyProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new System.ArgumentNullException(nameof(unityGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is SelectNextArmyCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var selectNext = (SelectNextArmyCommand)command;

            var state = selectNext.Execute();
            if (state == ActionState.Succeeded)
            {
                var armies = Game.Current.GetSelectedArmies();
                if (armies != null && armies.Count > 0)
                {
                    this.unityGame.SetCameraToSelectedBox();
                }
            }

            if (state == ActionState.Failed && selectNext is SelectNextArmyFromInputCommand &&
                this.unityGame.InputManager.CanAcceptGameplayInput)
            {
                PlayNoMovesBeep();
            }

            return state;
        }

        private void PlayNoMovesBeep()
        {
            if (!this.unityGame.InteractiveUI || !this.unityGame.SoundEnabled || AudioListener.pause || AudioListener.volume <= 0f)
                return;

            var clip = this.unityGame.WarPanel?.KilledPrefab?.GetComponent<AudioSource>()?.clip;
            if (clip == null) return;

            if (this.noMovesAudio == null)
            {
                this.noMovesAudio = this.unityGame.gameObject.AddComponent<AudioSource>();
                this.noMovesAudio.playOnAwake = false;
                this.noMovesAudio.spatialBlend = 0f;
            }
            this.noMovesAudio.PlayOneShot(clip);
        }
    }

    internal sealed class SelectNextArmyFromInputCommand : SelectNextArmyCommand
    {
        public SelectNextArmyFromInputCommand(ArmyController armyController) : base(armyController) { }
    }
}
