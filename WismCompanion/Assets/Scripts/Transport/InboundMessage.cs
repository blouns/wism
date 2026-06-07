using Wism.Companion.Shared.Events;

namespace WismCompanion.Transport
{
    /// <summary>A decoded inbound telemetry message, ready to apply on the Unity main thread.</summary>
    public readonly struct InboundMessage
    {
        public enum MessageKind
        {
            MapSnapshot,
            Command
        }

        private InboundMessage(MessageKind kind, MapSnapshot map, CommandExecutedEvent command)
        {
            Kind = kind;
            Map = map;
            Command = command;
        }

        public MessageKind Kind { get; }

        public MapSnapshot Map { get; }

        public CommandExecutedEvent Command { get; }

        public static InboundMessage ForMap(MapSnapshot map) => new(MessageKind.MapSnapshot, map, null);

        public static InboundMessage ForCommand(CommandExecutedEvent command) => new(MessageKind.Command, null, command);
    }
}
