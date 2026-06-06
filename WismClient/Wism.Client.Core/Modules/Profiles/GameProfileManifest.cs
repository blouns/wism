namespace Wism.Client.Modules.Profiles
{
    public sealed class GameProfileManifest
    {
        public string Id { get; set; } = "classic-warlords";
        public string DisplayName { get; set; } = "Classic Warlords";
        public string BaseWorld { get; set; } = "TestWorld";
        public string ModeId { get; set; } = "classic";
        public string[] EnabledPacks { get; set; } = new string[0];
        public string ModRoot { get; set; } = "mod";
        public string UnityScene { get; set; }
        public LaunchModeSettings Launch { get; set; } = new LaunchModeSettings();
    }
}
