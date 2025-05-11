using System;

namespace Wism.Companion.Shared.Events
{

    public class CommandExecutedEvent
    {
        public string CommandType { get; set; }
        public string Actor { get; set; }
        public string Target { get; set; }
        public string Result { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}