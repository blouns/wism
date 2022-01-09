using Assets.Scripts.CommandProcessors.Cutscenes.CityStages;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Core;

namespace Assets.Scripts.CommandProcessors.Cutscenes
{
    public class CutsceneStagerFactory
    {
        private InputManager inputManager;

        private UnityManager unityManager;

        public CutsceneStagerFactory(UnityManager unityManager)
        {
            this.unityManager = unityManager ?? throw new ArgumentNullException(nameof(unityManager));
            this.inputManager = unityManager.InputManager;
        }

        public CutsceneStager CreateRuinsStager(SearchRuinsCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var stages = new List<CutsceneStage>();

            stages.Add(new SearchStatusStage(command));
            stages.Add(new RuinsIntroStage(command));

            if (command.Location.Boon is ArtifactBoon)
            {
                stages.Add(new RuinsEncounteredMonsterStage(command));
                stages.Add(new RuinsFightMonsterStage(command));
            }
            else if (command.Location.Boon is ThroneBoon)
            {
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

        public CutsceneStager CreateLibraryStager(SearchLibraryCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var stages = new List<CutsceneStage>();

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

        public CutsceneStager CreateSageStager(SearchSageCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var stages = new List<CutsceneStage>();

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

        public CutsceneStager CreateTempleStager(SearchTempleCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var stages = new List<CutsceneStage>();

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


        public CutsceneStager CreateBuildCityStager(BuildCityCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var stages = new List<CutsceneStage>();

            stages.Add(new VerifyBuildStage(command));
            stages.Add(new AskBuildStage(command));
            stages.Add(new BuildCityStage(command));

            var stager = new CutsceneStager(stages)
            {
                Final = new DefaultFinalStage(command),
            };

            // Set the input callback
            inputManager.KeyPressed += stager.OnAnyKeyPressed;

            return stager;
        }

        public CutsceneStager CreateRazeCityStager(RazeCityCommand command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var stages = new List<CutsceneStage>();

            stages.Add(new AskRazeStage(command));
            stages.Add(new RavageCityStage(command));
            stages.Add(new RazeCityStage(command));
            stages.Add(new CityInRuinsStage(command));

            var stager = new CutsceneStager(stages)
            {
                Final = new DefaultFinalStage(command),
            };

            // Set the input callback
            inputManager.KeyPressed += stager.OnAnyKeyPressed;

            return stager;
        }
    }
}

