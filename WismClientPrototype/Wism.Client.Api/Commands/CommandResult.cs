using System;
using Wism.Client.Model;

namespace Wism.Client.Api.Commands
{
    public class CommandResult
    {
        public DateTime ExecutedTime { get; set; }

        public MapObjectBaseDto Before { get; set; }

        public MapObjectBaseDto After{ get; set; }
    }
}
