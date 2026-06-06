namespace Wism.Client.Modules.Profiles
{
    public sealed class ModOverlayManifest
    {
        public NamedDisplayOverride[] Clans { get; set; } = new NamedDisplayOverride[0];
        public NamedDisplayOverride[] Armies { get; set; } = new NamedDisplayOverride[0];
        public NamedDisplayOverride[] Artifacts { get; set; } = new NamedDisplayOverride[0];
    }

    public sealed class NamedDisplayOverride
    {
        public string ShortName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
