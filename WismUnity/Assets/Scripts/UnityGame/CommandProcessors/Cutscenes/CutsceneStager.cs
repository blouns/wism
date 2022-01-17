using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    /// <summary>
    /// Orchestrates a simple list of staged scenes
    /// </summary>
    public class CutsceneStager
    {
        internal CutsceneStager(List<CutsceneStage> stages)
        {
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));

            if (Stages.Count == 0)
            {
                throw new ArgumentException("Must include at least one CutsceneStage", nameof(stages));
            }

            Final = stages[stages.Count - 1];
        }

        public List<CutsceneStage> Stages { get; }

        public CutsceneStage Final { get; set; }

        public int SceneIndex { get; set; }
        
        public void OnAnyKeyPressed()
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
                        // Advance to final scene
                        result = SceneResult.Continue;
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
