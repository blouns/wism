using System;
using Wism.Client.Model;

namespace Wism.Client.Api.Commands
{
    public class CommandResult
    {
        public DateTime ExecutedTime { get; set; }

        public MapObjectBase Before { get; set; }

        public MapObjectBase After{ get; set; }
    }
}
