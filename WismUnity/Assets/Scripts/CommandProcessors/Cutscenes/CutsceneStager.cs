using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class CutsceneStager
    {
        public CutsceneStager(List<CutsceneStage> stages)
        {
            Stages = stages;
        }

        public List<CutsceneStage> Stages { get; }

        public CutsceneStage Final { get; set; }

        public int SceneIndex { get; set; }

        // TODO: Create cutscenes from JSON
        public static CutsceneStager CreateDefault(SearchRuinsCommand command, InputManager inputManager)
        {
            List<CutsceneStage> stages = new List<CutsceneStage>();

            stages.Add(new SearchStatusStage(command));
            stages.Add(new RuinsIntroStage(command));

            if (command.Location.Boon is ArtifactBoon)
            {
                stages.Add(new RuinsEncounteredMonsterStage(command));
                stages.Add(new RuinsFightMonsterStage(command));
            }
            else if (command.Location.Boon is AltarBoon)
            {
                stages.Add(new RuinsFoundThroneStage(command));
                stages.Add(new RuinsSitAtThroneStage(command));
            }
            
            stages.Add(new SearchLocationStage(command));
            stages.Add(new RuinsRevealBoonStage(command));

            var stager = new CutsceneStager(stages)
            {
                Final = new RuinsFinalStage(command),
            };

            // Set the input callback
            inputManager.KeyPressed += stager.OnAnyKeyPressed;

            return stager;
        }

        public static CutsceneStager CreateDefault(SearchLibraryCommand command, InputManager inputManager)
        {
            List<CutsceneStage> stages = new List<CutsceneStage>();

            stages.Add(new LibraryEnterStage(command));
            stages.Add(new LibrarySearchingStage(command));
            stages.Add(new SearchLocationStage(command));            

            var stager = new CutsceneStager(stages)
            {
                Final = new LibraryFinalStage(command),
            };

            // Set the input callback
            inputManager.KeyPressed += stager.OnAnyKeyPressed;

            return stager;
        }

        public static CutsceneStager CreateDefault(SearchSageCommand command, InputManager inputManager)
        {
            List<CutsceneStage> stages = new List<CutsceneStage>();

            stages.Add(new SageEnterStage(command));
            stages.Add(new SageGemStage(command));            
            stages.Add(new SearchLocationStage(command));
            stages.Add(new SageGoldStage(command));

            var stager = new CutsceneStager(stages)
            {
                Final = new SageFinalStage(command),
            };

            // Set the input callback
            inputManager.KeyPressed += stager.OnAnyKeyPressed;

            return stager;
        }

        public static CutsceneStager CreateDefault(SearchTempleCommand command, InputManager inputManager)
        {
            List<CutsceneStage> stages = new List<CutsceneStage>();

            stages.Add(new TempleEnterStage(command));            
            stages.Add(new SearchLocationStage(command));

            var stager = new CutsceneStager(stages)
            {
                Final = new TempleFinalStage(command),
            };

            // Set the input callback
            inputManager.KeyPressed += stager.OnAnyKeyPressed;

            return stager;
        }

        private void OnAnyKeyPressed()
        {
            if (SceneIndex < Stages.Count)
            {
                Stages[SceneIndex].OnAnyKeyPressed();
            }
        }

        public ActionState Action()
        {            
            SceneResult result;
            if (SceneIndex < Stages.Count)
            {
                result = Stages[SceneIndex].Action();
                switch (result)
                {
                    case SceneResult.Continue:
                        SceneIndex++;
                        break;
                    case SceneResult.Wait:
                        // Wait for user input
                        break;
                    case SceneResult.Success:
                    case SceneResult.Failure:
                    default:
                        SceneIndex = Stages.Count;
                        break;
                }
            }
            else
            {
                result = Close();
            }

            return MapToActionState(result);
        }



        private ActionState MapToActionState(SceneResult result)
        {
            ActionState state;
            switch (result)
            {
                case SceneResult.Continue:
                case SceneResult.Wait:
                    state = ActionState.InProgress;
                    break;
                case SceneResult.Success:
                    state = ActionState.Succeeded;
                    break;
                case SceneResult.Failure:
                default:
                    state = ActionState.Failed;
                    break;
            }

            return state;
        }

        public SceneResult Close()
        {
            return Final.Action();
        }
    }
}
