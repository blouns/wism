namespace Wism.Client.Modules.Worlds
{
    public sealed class WorldKitValidationOptions
    {
        public int RequestedPlayers { get; set; }
        public string[] ActiveClans { get; set; } = new string[0];
    }
}
