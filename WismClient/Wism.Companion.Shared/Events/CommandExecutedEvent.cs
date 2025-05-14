using System;
using System.Collections.Generic;
using Wism.Companion.Shared.Models;

namespace Wism.Companion.Shared.Events
{
    public class CommandExecutedEvent
    {
        public string CommandType { get; set; }

        // Replay-critical
        public string? ActorId { get; set; }
        public string? TargetId { get; set; }
        public PositionDto? TargetPosition { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();

        // Optional but useful
        public string? Result { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
