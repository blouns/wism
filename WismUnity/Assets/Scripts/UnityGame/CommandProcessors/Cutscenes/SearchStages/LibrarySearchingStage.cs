﻿using Wism.Client.Api.Commands;

namespace Assets.Scripts.CommandProcessors
{
    public class LibrarySearchingStage : LocationCutsceneStage
    {
        public LibrarySearchingStage(SearchLocationCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            Notify("Searching through the books, you find...");
            return ContinueOnKeyPress();
        }
    }
}