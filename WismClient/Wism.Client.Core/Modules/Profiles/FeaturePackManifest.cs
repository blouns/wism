namespace Wism.Client.Modules.Profiles
{
    public sealed class FeaturePackManifest
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public FeaturePackKind Kind { get; set; }
        public string Description { get; set; } = string.Empty;
        public string[] Dependencies { get; set; } = new string[0];
        public string[] Conflicts { get; set; } = new string[0];
        public LaunchModeSettings Launch { get; set; }
        public string PresentationCatalog { get; set; }
        public string Overlay { get; set; }
    }
}
