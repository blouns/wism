using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Agent.Commands
{
    public enum ActionState
    {
        NotStarted,
        InProgress,
        Succeeded,
        Failed,        
    }

    public interface IAction
    {
        public ActionState Execute();
    }
}
