using System.Collections.Generic;

namespace Wism.Client.Modules.Profiles
{
    public sealed class ModularGameProfileSelection
    {
        public ModularGameProfileSelection(
            GameProfileManifest profile,
            IReadOnlyList<FeaturePackManifest> packs,
            string modRoot,
            LaunchModeSettings launch)
        {
            Profile = profile;
            Packs = packs;
            ModRoot = modRoot;
            Launch = launch;
        }

        public GameProfileManifest Profile { get; }
        public IReadOnlyList<FeaturePackManifest> Packs { get; }
        public string ModRoot { get; }
        public LaunchModeSettings Launch { get; }
        public string BaseWorld => Launch.World ?? Profile.BaseWorld;
        public string[] PackIds
        {
            get
            {
                var ids = new string[Packs.Count];
                for (var i = 0; i < Packs.Count; i++)
                {
                    ids[i] = Packs[i].Id;
                }

                return ids;
            }
        }
    }
}
