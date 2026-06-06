using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Wism.Client.Data.Entities;

namespace Wism.Client.Modules.Profiles
{
    public static class ModKitSelectionService
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentWismVersion = "0.1.0";

        public static ModKitCompatibilityReport VerifySelection(
            string modRoot,
            string profileId,
            IEnumerable<string> packIds,
            string worldOverride = null,
            string wismVersion = null)
        {
            var report = new ModKitCompatibilityReport();
            try
            {
                var validation = ModKitValidator.ValidateModRoot(modRoot);
                foreach (var issue in validation.Issues)
                {
                    report.Add(issue.Severity, issue.Code, issue.Message, issue.Path);
                }

                var selection = ModularGameProfileCatalog.ResolveFromModRoot(modRoot, profileId, packIds);
                report.Selection = CreateSelection(modRoot, selection, worldOverride, wismVersion ?? CurrentWismVersion);
                ApplyCompatibility(report, selection, wismVersion ?? CurrentWismVersion);

                if (!validation.IsValid)
                {
                    report.Status = ModKitCompatibilityStatus.Invalid;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.IndexOf("conflicts", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                report.Status = ModKitCompatibilityStatus.Conflict;
                report.Add(ModKitValidationSeverity.Error, "pack-conflict", ex.Message, modRoot);
            }
            catch (InvalidOperationException ex) when (ex.Message.IndexOf("requires", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                report.Status = ModKitCompatibilityStatus.MissingDependency;
                report.Add(ModKitValidationSeverity.Error, "pack-dependency-missing", ex.Message, modRoot);
            }
            catch (Exception ex)
            {
                report.Status = ModKitCompatibilityStatus.Invalid;
                report.Add(ModKitValidationSeverity.Error, "selection-invalid", ex.Message, modRoot);
            }

            return report;
        }

        public static ModKitCompatibilityReport VerifySavedSelection(
            string modRoot,
            ModKitSelectionEntity savedSelection,
            string wismVersion = null)
        {
            if (savedSelection == null)
            {
                var legacy = new ModKitCompatibilityReport
                {
                    Status = ModKitCompatibilityStatus.Legacy,
                    Selection = CreateDefaultSelection(modRoot, wismVersion ?? CurrentWismVersion)
                };
                legacy.Add(ModKitValidationSeverity.Warning, "save-mod-selection-missing", "Save has no Mod Kit selection metadata; default no-pack selection will be used.", modRoot);
                return legacy;
            }

            var report = VerifySelection(
                modRoot,
                savedSelection.ProfileId,
                savedSelection.PackIds ?? Array.Empty<string>(),
                savedSelection.World,
                wismVersion ?? CurrentWismVersion);

            if (report.Selection != null
                && !string.Equals(report.Selection.ContentFingerprint, savedSelection.ContentFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                report.Status = ModKitCompatibilityStatus.FingerprintMismatch;
                report.Add(
                    ModKitValidationSeverity.Error,
                    "save-mod-fingerprint-mismatch",
                    "Installed mod content does not match the saved game mod fingerprint.",
                    modRoot);
            }

            return report;
        }

        private static ModKitSelectionEntity CreateDefaultSelection(string modRoot, string wismVersion)
        {
            var selection = ModularGameProfileCatalog.ResolveFromModRoot(modRoot, ModularGameProfileCatalog.DefaultProfileId, Array.Empty<string>());
            return CreateSelection(modRoot, selection, null, wismVersion);
        }

        private static ModKitSelectionEntity CreateSelection(
            string modRoot,
            ModularGameProfileSelection selection,
            string worldOverride,
            string wismVersion)
        {
            var world = string.IsNullOrWhiteSpace(worldOverride)
                ? selection.BaseWorld
                : worldOverride;

            return new ModKitSelectionEntity
            {
                SchemaVersion = CurrentSchemaVersion,
                WismVersion = wismVersion,
                ProfileId = selection.Profile.Id,
                ProfileVersion = selection.Profile.Version ?? string.Empty,
                PackIds = selection.PackIds,
                PackVersions = selection.Packs.Select(pack => pack.Version ?? string.Empty).ToArray(),
                World = world,
                ContentFingerprint = ComputeFingerprint(modRoot, selection, world)
            };
        }

        private static void ApplyCompatibility(
            ModKitCompatibilityReport report,
            ModularGameProfileSelection selection,
            string wismVersion)
        {
            if (!HasGreenMetadata(selection.Profile))
            {
                report.Status = ModKitCompatibilityStatus.Legacy;
                report.Add(
                    ModKitValidationSeverity.Warning,
                    "profile-version-missing",
                    $"Profile '{selection.Profile.Id}' is loadable but is missing Green verification metadata.",
                    ProfilePath(selection.ModRoot, selection.Profile.Id));
            }

            ApplyVersionRange(report, selection.Profile.Id, selection.Profile.MinWismVersion, selection.Profile.MaxWismVersion, wismVersion, ProfilePath(selection.ModRoot, selection.Profile.Id));

            foreach (var pack in selection.Packs)
            {
                if (!HasGreenMetadata(pack))
                {
                    report.Status = ModKitCompatibilityStatus.Legacy;
                    report.Add(
                        ModKitValidationSeverity.Warning,
                        "pack-version-missing",
                        $"Feature pack '{pack.Id}' is loadable but is missing Green verification metadata.",
                        PackPath(selection.ModRoot, pack.Id));
                }

                ApplyVersionRange(report, pack.Id, pack.MinWismVersion, pack.MaxWismVersion, wismVersion, PackPath(selection.ModRoot, pack.Id));
            }
        }

        private static bool HasGreenMetadata(GameProfileManifest profile)
        {
            return profile.SchemaVersion.HasValue
                   && profile.SchemaVersion.Value == CurrentSchemaVersion
                   && !string.IsNullOrWhiteSpace(profile.Version);
        }

        private static bool HasGreenMetadata(FeaturePackManifest pack)
        {
            return pack.SchemaVersion.HasValue
                   && pack.SchemaVersion.Value == CurrentSchemaVersion
                   && !string.IsNullOrWhiteSpace(pack.Version);
        }

        private static void ApplyVersionRange(
            ModKitCompatibilityReport report,
            string id,
            string minVersion,
            string maxVersion,
            string currentVersion,
            string path)
        {
            if (!string.IsNullOrWhiteSpace(minVersion) && CompareVersions(currentVersion, minVersion) < 0)
            {
                report.Status = ModKitCompatibilityStatus.UnsupportedVersion;
                report.Add(ModKitValidationSeverity.Error, "mod-min-version-unsupported", $"'{id}' requires WISM {minVersion} or newer.", path);
            }

            if (!string.IsNullOrWhiteSpace(maxVersion) && CompareVersions(currentVersion, maxVersion) > 0)
            {
                report.Status = ModKitCompatibilityStatus.UnsupportedVersion;
                report.Add(ModKitValidationSeverity.Error, "mod-max-version-unsupported", $"'{id}' supports WISM up to {maxVersion}.", path);
            }
        }

        private static int CompareVersions(string left, string right)
        {
            Version leftVersion;
            Version rightVersion;
            if (!Version.TryParse(NormalizeVersion(left), out leftVersion)
                || !Version.TryParse(NormalizeVersion(right), out rightVersion))
            {
                return string.Compare(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return leftVersion.CompareTo(rightVersion);
        }

        private static string NormalizeVersion(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "0.0.0"
                : value.Trim().TrimStart('v', 'V');
        }

        private static string ComputeFingerprint(string modRoot, ModularGameProfileSelection selection, string world)
        {
            var files = BuildFingerprintFiles(modRoot, selection, world)
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            using (var sha = SHA256.Create())
            {
                foreach (var path in files)
                {
                    var relative = RelativePath(modRoot, path).Replace('\\', '/').ToLowerInvariant();
                    var pathBytes = Encoding.UTF8.GetBytes(relative + "\n");
                    sha.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);
                    var fileHash = SHA256.Create().ComputeHash(File.ReadAllBytes(path));
                    sha.TransformBlock(fileHash, 0, fileHash.Length, null, 0);
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToHex(sha.Hash);
            }
        }

        private static IEnumerable<string> BuildFingerprintFiles(string modRoot, ModularGameProfileSelection selection, string world)
        {
            yield return Path.Combine(modRoot, "Clan.json");
            yield return Path.Combine(modRoot, "Army.json");
            yield return Path.Combine(modRoot, "Artifact.json");
            yield return Path.Combine(modRoot, "Terrain.json");
            yield return Path.Combine(modRoot, "ClanTerrainModifier.json");
            yield return ProfilePath(modRoot, selection.Profile.Id);
            yield return Path.Combine(modRoot, "Worlds", world ?? string.Empty, "City.json");
            yield return Path.Combine(modRoot, "Worlds", world ?? string.Empty, "Location.json");
            yield return Path.Combine(modRoot, "Worlds", world ?? string.Empty, "Map.json");

            foreach (var pack in selection.Packs)
            {
                yield return PackPath(modRoot, pack.Id);
                if (!string.IsNullOrWhiteSpace(pack.Overlay))
                {
                    yield return Path.Combine(modRoot, "FeaturePacks", pack.Id, pack.Overlay);
                }

                if (!string.IsNullOrWhiteSpace(pack.PresentationCatalog))
                {
                    yield return Path.Combine(modRoot, "FeaturePacks", pack.Id, pack.PresentationCatalog);
                }
            }
        }

        private static string ProfilePath(string modRoot, string profileId)
        {
            return Path.Combine(modRoot, "Profiles", profileId, "profile.json");
        }

        private static string PackPath(string modRoot, string packId)
        {
            return Path.Combine(modRoot, "FeaturePacks", packId, "pack.json");
        }

        private static string RelativePath(string root, string path)
        {
            var rootUri = new Uri(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
