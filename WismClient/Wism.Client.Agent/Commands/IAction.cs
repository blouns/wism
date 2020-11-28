using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Agent.Commands
{
    public interface IAction
    {
        public bool Execute();
    }
}
