using System;
using System.Collections.Generic;
using Wism.Client.Model;

namespace Wism.Client.Agent.Commands
{
    public interface IAction
    {
        public abstract void Execute();

        public abstract void Undo();
    }
}
