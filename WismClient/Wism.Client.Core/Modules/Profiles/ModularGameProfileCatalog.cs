using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Wism.Client.Modules.Profiles
{
    public static class ModularGameProfileCatalog
    {
        public const string DefaultProfileId = "classic-warlords";

        public static ModularGameProfileSelection Resolve(
            string repositoryRoot,
            string profileId = null,
            IEnumerable<string> enabledPacks = null)
        {
            var modRoot = ResolveModRoot(repositoryRoot);
            return ResolveFromModRoot(modRoot, profileId, enabledPacks);
        }

        public static ModularGameProfileSelection ResolveFromModRoot(
            string modRoot,
            string profileId = null,
            IEnumerable<string> enabledPacks = null)
        {
            var profile = LoadProfile(modRoot, profileId ?? DefaultProfileId);
            var requested = enabledPacks?.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray()
                ?? profile.EnabledPacks;
            var packs = requested.Select(id => LoadPack(modRoot, id)).ToArray();
            ValidatePackSet(packs);

            var launch = MergeLaunch(profile.Launch, packs.Select(pack => pack.Launch));
            return new ModularGameProfileSelection(profile, packs, modRoot, launch);
        }

        public static IReadOnlyList<FeaturePackManifest> DiscoverPacks(string repositoryRoot)
        {
            var modRoot = ResolveModRoot(repositoryRoot);
            return DiscoverPacksFromModRoot(modRoot);
        }

        public static IReadOnlyList<FeaturePackManifest> DiscoverPacksFromModRoot(string modRoot)
        {
            var packsRoot = Path.Combine(modRoot, "FeaturePacks");
            if (!Directory.Exists(packsRoot))
            {
                return Array.Empty<FeaturePackManifest>();
            }

            return Directory.GetDirectories(packsRoot)
                .Select(path => Path.Combine(path, "pack.json"))
                .Where(File.Exists)
                .Select(LoadJson<FeaturePackManifest>)
                .OrderBy(pack => pack.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string ResolveModRoot(string repositoryRoot)
        {
            var candidates = new[]
            {
                Path.Combine(repositoryRoot, "WismClient", "Wism.Client.Core", "mod"),
                Path.Combine(repositoryRoot, "WismUnity", "Assets", "Plugins", "WismClient", "Mods"),
                Path.Combine(repositoryRoot, "Assets", "Plugins", "WismClient", "Mods"),
                Path.Combine(repositoryRoot, "Assets", "Mod"),
                Path.Combine(repositoryRoot, "Wism.Client.Core", "mod"),
                Path.Combine(repositoryRoot, "mod")
            };

            var found = candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "Clan.json")));
            return found ?? candidates[0];
        }

        private static GameProfileManifest LoadProfile(string modRoot, string profileId)
        {
            var path = Path.Combine(modRoot, "Profiles", profileId, "profile.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Game profile '{profileId}' was not found.", path);
            }

            return LoadJson<GameProfileManifest>(path);
        }

        private static FeaturePackManifest LoadPack(string modRoot, string packId)
        {
            var path = Path.Combine(modRoot, "FeaturePacks", packId, "pack.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Feature pack '{packId}' was not found.", path);
            }

            return LoadJson<FeaturePackManifest>(path);
        }

        private static void ValidatePackSet(IReadOnlyList<FeaturePackManifest> packs)
        {
            var ids = new HashSet<string>(packs.Select(pack => pack.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var pack in packs)
            {
                foreach (var dependency in pack.Dependencies ?? Array.Empty<string>())
                {
                    if (!ids.Contains(dependency))
                    {
                        throw new InvalidOperationException($"Feature pack '{pack.Id}' requires '{dependency}'.");
                    }
                }

                foreach (var conflict in pack.Conflicts ?? Array.Empty<string>())
                {
                    if (ids.Contains(conflict))
                    {
                        throw new InvalidOperationException($"Feature pack '{pack.Id}' conflicts with '{conflict}'.");
                    }
                }
            }
        }

        private static LaunchModeSettings MergeLaunch(LaunchModeSettings profileLaunch, IEnumerable<LaunchModeSettings> packLaunches)
        {
            var merged = new LaunchModeSettings
            {
                World = profileLaunch.World,
                Seed = profileLaunch.Seed,
                Clans = profileLaunch.Clans,
                MaxTurns = profileLaunch.MaxTurns,
                Scenario = profileLaunch.Scenario
            };

            foreach (var launch in packLaunches.Where(launch => launch != null))
            {
                merged.World = launch.World ?? merged.World;
                merged.Seed = launch.Seed ?? merged.Seed;
                merged.Clans = launch.Clans ?? merged.Clans;
                merged.MaxTurns = launch.MaxTurns ?? merged.MaxTurns;
                merged.Scenario = launch.Scenario ?? merged.Scenario;
            }

            return merged;
        }

        private static T LoadJson<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path))
                ?? throw new InvalidDataException($"Could not load JSON file {path}.");
        }
    }
}
