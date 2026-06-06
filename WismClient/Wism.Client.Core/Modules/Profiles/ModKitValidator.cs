using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Modules.Profiles
{
    public static class ModKitValidator
    {
        public static ModKitValidationReport Validate(string repositoryRoot)
        {
            var modRoot = ModularGameProfileCatalog.ResolveModRoot(repositoryRoot);
            return ValidateModRoot(modRoot);
        }

        public static ModKitValidationReport ValidateModRoot(string modRoot)
        {
            var report = new ModKitValidationReport();
            if (!Directory.Exists(modRoot))
            {
                report.Add(ModKitValidationSeverity.Error, "mod-root-missing", "Mod root was not found.", modRoot);
                return report;
            }

            var clans = LoadStableIds<ClanInfo>(report, modRoot, "Clan.json", info => info.ShortName);
            var armies = LoadStableIds<ArmyInfo>(report, modRoot, "Army.json", info => info.ShortName);
            var artifacts = LoadStableIds<ArtifactInfo>(report, modRoot, "Artifact.json", info => info.ShortName);

            ValidateProfiles(report, modRoot);
            ValidateFeaturePacks(report, modRoot, clans, armies, artifacts);

            return report;
        }

        private static void ValidateProfiles(ModKitValidationReport report, string modRoot)
        {
            var profilesRoot = Path.Combine(modRoot, "Profiles");
            if (!Directory.Exists(profilesRoot))
            {
                report.Add(ModKitValidationSeverity.Error, "profiles-missing", "Profiles directory was not found.", profilesRoot);
                return;
            }

            foreach (var profileDirectory in Directory.GetDirectories(profilesRoot))
            {
                var profilePath = Path.Combine(profileDirectory, "profile.json");
                var profile = LoadManifest<GameProfileManifest>(report, profilePath, "profile-json-invalid");
                if (profile == null)
                {
                    continue;
                }

                var folderId = Path.GetFileName(profileDirectory);
                ValidateId(report, "profile-id-mismatch", profile.Id, folderId, profilePath);
                ValidateWorldExists(report, modRoot, profile.BaseWorld, profilePath, "profile-world-missing");

                foreach (var packId in profile.EnabledPacks ?? Array.Empty<string>())
                {
                    ValidatePackExists(report, modRoot, packId, profilePath, "profile-pack-missing");
                }

                if (profile.Launch != null && !string.IsNullOrWhiteSpace(profile.Launch.World))
                {
                    ValidateWorldExists(report, modRoot, profile.Launch.World, profilePath, "profile-launch-world-missing");
                }
            }
        }

        private static void ValidateFeaturePacks(
            ModKitValidationReport report,
            string modRoot,
            ISet<string> clans,
            ISet<string> armies,
            ISet<string> artifacts)
        {
            var packsRoot = Path.Combine(modRoot, "FeaturePacks");
            if (!Directory.Exists(packsRoot))
            {
                report.Add(ModKitValidationSeverity.Warning, "packs-missing", "FeaturePacks directory was not found.", packsRoot);
                return;
            }

            var knownPacks = new HashSet<string>(
                Directory.GetDirectories(packsRoot)
                    .Select(Path.GetFileName)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var packDirectory in Directory.GetDirectories(packsRoot))
            {
                var packPath = Path.Combine(packDirectory, "pack.json");
                var raw = LoadObject(report, packPath, "pack-json-invalid");
                if (raw == null)
                {
                    continue;
                }

                var pack = raw.ToObject<FeaturePackManifest>();
                if (pack == null)
                {
                    report.Add(ModKitValidationSeverity.Error, "pack-json-invalid", "Pack manifest could not be loaded.", packPath);
                    continue;
                }

                var folderId = Path.GetFileName(packDirectory);
                ValidateRequired(report, raw, "id", "pack-id-missing", packPath);
                ValidateRequired(report, raw, "displayName", "pack-display-name-missing", packPath);
                ValidateRequired(report, raw, "kind", "pack-kind-missing", packPath);
                ValidateId(report, "pack-id-mismatch", pack.Id, folderId, packPath);
                ValidatePackReferences(report, modRoot, knownPacks, pack, packPath);

                if (pack.Kind == FeaturePackKind.Visual)
                {
                    ValidatePackFile(report, modRoot, pack.Id, pack.PresentationCatalog, packPath, "visual-catalog-missing");
                }
                else if (pack.Kind == FeaturePackKind.Flavor)
                {
                    ValidateFlavorOverlay(report, modRoot, pack, packPath, clans, armies, artifacts);
                }
                else if (pack.Kind == FeaturePackKind.Mode)
                {
                    if (pack.Launch == null)
                    {
                        report.Add(ModKitValidationSeverity.Error, "mode-launch-missing", "Mode packs must define launch settings.", packPath);
                    }
                    else
                    {
                        ValidateWorldExists(report, modRoot, pack.Launch.World, packPath, "mode-launch-world-missing");
                    }
                }
            }
        }

        private static void ValidatePackReferences(
            ModKitValidationReport report,
            string modRoot,
            ISet<string> knownPacks,
            FeaturePackManifest pack,
            string packPath)
        {
            foreach (var dependency in pack.Dependencies ?? Array.Empty<string>())
            {
                if (string.Equals(pack.Id, dependency, StringComparison.OrdinalIgnoreCase))
                {
                    report.Add(ModKitValidationSeverity.Error, "pack-self-dependency", "Feature packs cannot depend on themselves.", packPath);
                }

                if (!knownPacks.Contains(dependency))
                {
                    ValidatePackExists(report, modRoot, dependency, packPath, "pack-dependency-missing");
                }
            }

            foreach (var conflict in pack.Conflicts ?? Array.Empty<string>())
            {
                if (string.Equals(pack.Id, conflict, StringComparison.OrdinalIgnoreCase))
                {
                    report.Add(ModKitValidationSeverity.Error, "pack-self-conflict", "Feature packs cannot conflict with themselves.", packPath);
                }

                if (!knownPacks.Contains(conflict))
                {
                    report.Add(ModKitValidationSeverity.Warning, "pack-conflict-unknown", $"Conflict target '{conflict}' is not installed locally.", packPath);
                }
            }
        }

        private static void ValidateFlavorOverlay(
            ModKitValidationReport report,
            string modRoot,
            FeaturePackManifest pack,
            string packPath,
            ISet<string> clans,
            ISet<string> armies,
            ISet<string> artifacts)
        {
            var overlayPath = ResolvePackFile(modRoot, pack.Id, pack.Overlay);
            if (string.IsNullOrWhiteSpace(pack.Overlay) || !File.Exists(overlayPath))
            {
                report.Add(ModKitValidationSeverity.Error, "flavor-overlay-missing", "Flavor packs must reference an overlay JSON file.", packPath);
                return;
            }

            var overlay = LoadManifest<ModOverlayManifest>(report, overlayPath, "flavor-overlay-invalid");
            if (overlay == null)
            {
                return;
            }

            ValidateDisplayOverrides(report, overlay.Clans, clans, overlayPath, "flavor-clan-unknown");
            ValidateDisplayOverrides(report, overlay.Armies, armies, overlayPath, "flavor-army-unknown");
            ValidateDisplayOverrides(report, overlay.Artifacts, artifacts, overlayPath, "flavor-artifact-unknown");
        }

        private static void ValidateDisplayOverrides(
            ModKitValidationReport report,
            IEnumerable<NamedDisplayOverride> overrides,
            ISet<string> knownIds,
            string path,
            string code)
        {
            foreach (var item in overrides ?? Array.Empty<NamedDisplayOverride>())
            {
                if (string.IsNullOrWhiteSpace(item.ShortName))
                {
                    report.Add(ModKitValidationSeverity.Error, "flavor-short-name-missing", "Flavor overrides must include a stable shortName.", path);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    report.Add(ModKitValidationSeverity.Error, "flavor-display-name-missing", $"Flavor override '{item.ShortName}' must include a displayName.", path);
                }

                if (!knownIds.Contains(item.ShortName))
                {
                    report.Add(ModKitValidationSeverity.Error, code, $"Flavor override references unknown stable id '{item.ShortName}'.", path);
                }
            }
        }

        private static void ValidatePackFile(
            ModKitValidationReport report,
            string modRoot,
            string packId,
            string relativePath,
            string manifestPath,
            string code)
        {
            var path = ResolvePackFile(modRoot, packId, relativePath);
            if (string.IsNullOrWhiteSpace(relativePath) || !File.Exists(path))
            {
                report.Add(ModKitValidationSeverity.Error, code, $"Referenced pack file '{relativePath}' was not found.", manifestPath);
                return;
            }

            LoadObject(report, path, "pack-file-json-invalid");
        }

        private static void ValidateWorldExists(ModKitValidationReport report, string modRoot, string worldId, string sourcePath, string code)
        {
            if (string.IsNullOrWhiteSpace(worldId))
            {
                report.Add(ModKitValidationSeverity.Error, code, "A world id is required.", sourcePath);
                return;
            }

            var worldRoot = Path.Combine(modRoot, "Worlds", worldId);
            if (!File.Exists(Path.Combine(worldRoot, "City.json")) || !File.Exists(Path.Combine(worldRoot, "Location.json")))
            {
                report.Add(ModKitValidationSeverity.Error, code, $"World '{worldId}' was not found or is missing City.json/Location.json.", sourcePath);
            }
        }

        private static void ValidatePackExists(ModKitValidationReport report, string modRoot, string packId, string sourcePath, string code)
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                report.Add(ModKitValidationSeverity.Error, code, "A feature pack id is required.", sourcePath);
                return;
            }

            var packPath = Path.Combine(modRoot, "FeaturePacks", packId, "pack.json");
            if (!File.Exists(packPath))
            {
                report.Add(ModKitValidationSeverity.Error, code, $"Feature pack '{packId}' was not found.", sourcePath);
            }
        }

        private static void ValidateRequired(ModKitValidationReport report, JObject raw, string property, string code, string path)
        {
            var token = raw[property];
            if (token == null || string.IsNullOrWhiteSpace(token.ToString()))
            {
                report.Add(ModKitValidationSeverity.Error, code, $"Required property '{property}' is missing.", path);
            }
        }

        private static void ValidateId(ModKitValidationReport report, string code, string manifestId, string folderId, string path)
        {
            if (!string.IsNullOrWhiteSpace(manifestId)
                && !string.IsNullOrWhiteSpace(folderId)
                && !string.Equals(manifestId, folderId, StringComparison.OrdinalIgnoreCase))
            {
                report.Add(ModKitValidationSeverity.Error, code, $"Manifest id '{manifestId}' does not match folder id '{folderId}'.", path);
            }
        }

        private static string ResolvePackFile(string modRoot, string packId, string relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath)
                ? string.Empty
                : Path.Combine(modRoot, "FeaturePacks", packId, relativePath);
        }

        private static ISet<string> LoadStableIds<T>(
            ModKitValidationReport report,
            string modRoot,
            string fileName,
            Func<T, string> idSelector)
        {
            var path = Path.Combine(modRoot, fileName);
            var items = LoadManifest<List<T>>(report, path, "base-json-invalid") ?? new List<T>();
            return new HashSet<string>(
                items.Select(idSelector)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static T LoadManifest<T>(ModKitValidationReport report, string path, string code) where T : class
        {
            try
            {
                if (!File.Exists(path))
                {
                    report.Add(ModKitValidationSeverity.Error, code, "JSON file was not found.", path);
                    return null;
                }

                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (JsonException ex)
            {
                report.Add(ModKitValidationSeverity.Error, code, ex.Message, path);
                return null;
            }
        }

        private static JObject LoadObject(ModKitValidationReport report, string path, string code)
        {
            try
            {
                if (!File.Exists(path))
                {
                    report.Add(ModKitValidationSeverity.Error, code, "JSON file was not found.", path);
                    return null;
                }

                return JObject.Parse(File.ReadAllText(path));
            }
            catch (JsonException ex)
            {
                report.Add(ModKitValidationSeverity.Error, code, ex.Message, path);
                return null;
            }
        }
    }
}
