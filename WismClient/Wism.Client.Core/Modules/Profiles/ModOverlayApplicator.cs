using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Modules.Profiles
{
    internal static class ModOverlayApplicator
    {
        public static void ApplyClanOverlays(IList<ClanInfo> infos, string modRoot, IEnumerable<string> packIds)
        {
            foreach (var overlay in LoadOverlays(modRoot, packIds))
            {
                Apply(infos, overlay.Clans);
            }
        }

        public static void ApplyArmyOverlays(IList<ArmyInfo> infos, string modRoot, IEnumerable<string> packIds)
        {
            foreach (var overlay in LoadOverlays(modRoot, packIds))
            {
                Apply(infos, overlay.Armies);
            }
        }

        public static void ApplyArtifactOverlays(IList<ArtifactInfo> infos, string modRoot, IEnumerable<string> packIds)
        {
            foreach (var overlay in LoadOverlays(modRoot, packIds))
            {
                Apply(infos, overlay.Artifacts);
            }
        }

        private static IEnumerable<ModOverlayManifest> LoadOverlays(string modRoot, IEnumerable<string> packIds)
        {
            foreach (var packId in packIds ?? Array.Empty<string>())
            {
                var manifestPath = Path.Combine(modRoot, "FeaturePacks", packId, "pack.json");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                var manifest = JsonConvert.DeserializeObject<FeaturePackManifest>(File.ReadAllText(manifestPath));
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.Overlay))
                {
                    continue;
                }

                var overlayPath = Path.Combine(modRoot, "FeaturePacks", packId, manifest.Overlay);
                if (!File.Exists(overlayPath))
                {
                    continue;
                }

                yield return JsonConvert.DeserializeObject<ModOverlayManifest>(File.ReadAllText(overlayPath))
                    ?? new ModOverlayManifest();
            }
        }

        private static void Apply(IList<ClanInfo> infos, IEnumerable<NamedDisplayOverride> overrides)
        {
            foreach (var item in overrides ?? Array.Empty<NamedDisplayOverride>())
            {
                var info = infos.FirstOrDefault(candidate => string.Equals(candidate.ShortName, item.ShortName, StringComparison.OrdinalIgnoreCase));
                if (info != null && !string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    info.DisplayName = item.DisplayName;
                }
            }
        }

        private static void Apply(IList<ArmyInfo> infos, IEnumerable<NamedDisplayOverride> overrides)
        {
            foreach (var item in overrides ?? Array.Empty<NamedDisplayOverride>())
            {
                var info = infos.FirstOrDefault(candidate => string.Equals(candidate.ShortName, item.ShortName, StringComparison.OrdinalIgnoreCase));
                if (info != null && !string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    info.DisplayName = item.DisplayName;
                }
            }
        }

        private static void Apply(IList<ArtifactInfo> infos, IEnumerable<NamedDisplayOverride> overrides)
        {
            foreach (var item in overrides ?? Array.Empty<NamedDisplayOverride>())
            {
                var info = infos.FirstOrDefault(candidate => string.Equals(candidate.ShortName, item.ShortName, StringComparison.OrdinalIgnoreCase));
                if (info != null && !string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    info.DisplayName = item.DisplayName;
                }
            }
        }
    }
}
