using System;
using System.Collections.Generic;
using Wism.Client.Model;

namespace Wism.Client.Api.Commands
{
    public abstract class Command
    {
        public abstract CommandResult Execute();

        public abstract void Undo();
    }
}
